namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	public class TakeHelper
	{
		private readonly MediaOpsLiveApi _api;

		private bool _waitForCompletion = false;
		private TimeSpan _timeout;
		private ConnectionMonitor _connectionMonitor;

		public TakeHelper(MediaOpsLiveApi api)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
		}

		public void EnableWaitForCompletion(TimeSpan timeout, ConnectionMonitor connectionMonitor = null)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw new ArgumentException("Timeout cannot be negative.", nameof(timeout));
			}

			_waitForCompletion = true;
			_timeout = timeout;

			if (connectionMonitor == null)
			{
				var staticCache = StaticMediaOpsLiveCache.GetOrCreate(_api.Connection);
				connectionMonitor = staticCache.ConnectionMonitor;
			}

			_connectionMonitor = connectionMonitor;
		}

		public void DisableWaitForCompletion()
		{
			_waitForCompletion = false;
			_connectionMonitor = null;
		}

		public void Take(ICollection<ConnectionRequest> connectionRequests, PerformanceTracker performanceTracker)
		{
			if (connectionRequests == null)
			{
				throw new ArgumentNullException(nameof(connectionRequests));
			}

			if (performanceTracker == null)
			{
				throw new ArgumentNullException(nameof(performanceTracker));
			}

			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				TakeInternal(connectionRequests, performanceTracker);
			}
		}

		public void Take(ICollection<VsgConnectionRequest> vsgConnectionRequests, PerformanceTracker performanceTracker)
		{
			if (vsgConnectionRequests == null)
			{
				throw new ArgumentNullException(nameof(vsgConnectionRequests));
			}

			if (performanceTracker == null)
			{
				throw new ArgumentNullException(nameof(performanceTracker));
			}

			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				// load all endpoints
				var endpointIds = vsgConnectionRequests
					.SelectMany(x => x.Source.GetLevelEndpoints().Concat(x.Destination.GetLevelEndpoints()))
					.Select(x => x.Endpoint.ID)
					.Distinct();

				var endpoints = LoadEndpoints(endpointIds, performanceTracker);

				// create connection requests between endpoints
				var connectionRequests = new List<ConnectionRequest>();

				foreach (var vsgConnectionRequest in vsgConnectionRequests)
				{
					var source = vsgConnectionRequest.Source;
					var destination = vsgConnectionRequest.Destination;

					var levelMappings = vsgConnectionRequest.LevelMappings;
					if (levelMappings == null || levelMappings.Count == 0)
					{
						// create default level mappings
						// connect same levels between source and destination
						levelMappings = source.GetLevelEndpoints().Select(x => x.Level)
							.Intersect(destination.GetLevelEndpoints().Select(x => x.Level))
							.Select(x => new LevelMapping(x))
							.ToList();
					}

					foreach (var levelMapping in levelMappings)
					{
						var (sourceLevel, destinationLevel) = levelMapping;

						if (!source.TryGetEndpointForLevel(sourceLevel, out var sourceEndpointRef) ||
							!endpoints.TryGetValue(sourceEndpointRef, out var sourceEndpoint))
						{
							throw new InvalidOperationException($"Couldn't find source endpoint for level with ID '{sourceLevel.ID}' in virtual signal group '{source.Name}'");
						}

						if (!destination.TryGetEndpointForLevel(destinationLevel, out var destinationEndpointRef) ||
							!endpoints.TryGetValue(destinationEndpointRef, out var destinationEndpoint))
						{
							throw new InvalidOperationException($"Couldn't find destination endpoint for level with ID '{destinationLevel.ID}' in virtual signal group '{destination.Name}'");
						}

						var request = new ConnectionRequest(sourceEndpoint, destinationEndpoint);
						connectionRequests.Add(request);
					}
				}

				TakeInternal(connectionRequests, performanceTracker);
			}
		}

		private void TakeInternal(ICollection<ConnectionRequest> connectionRequests, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var takeContexts = connectionRequests
					.Select(x => new ConnectionOperationContext(x))
					.ToList();

				PrepareData(takeContexts, performanceTracker);

				NotifyPendingConnectionActions(ScriptAction.Connect, takeContexts, performanceTracker);
				ExecuteConnectionHandlerScripts(ScriptAction.Connect, takeContexts, performanceTracker);

				if (_waitForCompletion)
				{
					WaitUntilAllConnected(takeContexts, performanceTracker);
				}
			}
		}

		public void Disconnect(ICollection<DisconnectRequest> disconnectRequests, PerformanceTracker performanceTracker)
		{
			if (disconnectRequests == null)
			{
				throw new ArgumentNullException(nameof(disconnectRequests));
			}

			if (performanceTracker == null)
			{
				throw new ArgumentNullException(nameof(performanceTracker));
			}

			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				DisconnectInternal(disconnectRequests, performanceTracker);
			}
		}

		public void Disconnect(ICollection<VsgDisconnectRequest> vsgDisconnectRequests, PerformanceTracker performanceTracker)
		{
			if (vsgDisconnectRequests == null)
			{
				throw new ArgumentNullException(nameof(vsgDisconnectRequests));
			}

			if (performanceTracker == null)
			{
				throw new ArgumentNullException(nameof(performanceTracker));
			}

			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				// load all endpoints
				var endpointIds = vsgDisconnectRequests
					.SelectMany(x => x.Destination.GetLevelEndpoints())
					.Select(x => x.Endpoint.ID)
					.Distinct();

				var endpoints = LoadEndpoints(endpointIds, performanceTracker);

				// create disconnect requests
				var disconnectRequests = new List<DisconnectRequest>();

				foreach (var vsgDisconnectRequest in vsgDisconnectRequests)
				{
					var destination = vsgDisconnectRequest.Destination;

					foreach (var levelEndpoint in destination.GetLevelEndpoints())
					{
						if (vsgDisconnectRequest.Levels != null &&
							!vsgDisconnectRequest.Levels.Contains(levelEndpoint.Level))
						{
							continue; // skip this level if it is not in the disconnect request
						}

						if (!endpoints.TryGetValue(levelEndpoint.Endpoint, out var destinationEndpoint))
						{
							throw new InvalidOperationException($"Couldn't find endpoint with ID '{levelEndpoint.Endpoint.ID}'");
						}

						var request = new DisconnectRequest(destinationEndpoint);
						disconnectRequests.Add(request);
					}
				}

				DisconnectInternal(disconnectRequests, performanceTracker);
			}
		}

		private void DisconnectInternal(ICollection<DisconnectRequest> disconnectRequests, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var takeContexts = disconnectRequests
					.Select(x => new ConnectionOperationContext(x))
					.ToList();

				PrepareData(takeContexts, performanceTracker);

				NotifyPendingConnectionActions(ScriptAction.Disconnect, takeContexts, performanceTracker);
				ExecuteConnectionHandlerScripts(ScriptAction.Disconnect, takeContexts, performanceTracker);

				if (_waitForCompletion)
				{
					WaitUntilAllDisconnected(takeContexts, performanceTracker);
				}
			}
		}

		private void PrepareData(ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var dms = _api.Connection.GetDms();

				GetDestinationElements(dms, takeContexts, performanceTracker);
				GetMediationElements(takeContexts, performanceTracker);
				FindConnectionHandlerScripts(takeContexts, performanceTracker);
			}
		}

		private IDictionary<Guid, Endpoint> LoadEndpoints(IEnumerable<Guid> endpointIds, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var result = _api.Endpoints.Read(endpointIds);

				performanceTracker.AddMetadata("Number of Endpoints", Convert.ToString(result.Count));

				return result;
			}
		}

		private void GetDestinationElements(IDms dms, ICollection<ConnectionOperationContext> connectionContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				// Group connections by their destination element
				foreach (var group in connectionContexts.GroupBy(ctx => ctx.Destination.Element))
				{
					var elementId = group.Key;

					if (elementId == null)
					{
						// Skip connections without a valid destination element
						continue;
					}

					// Get the element once for this group
					var element = dms.GetElement(elementId.Value);

					// Assign the fetched element to each connection in the group
					foreach (var connection in group)
					{
						connection.DestinationElement = element;
					}
				}
			}
		}

		private void GetMediationElements(ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var allMediationElements = _api.MediationElements.GetAllElementsCached();

				foreach (var group in takeContexts.GroupBy(x => x.DestinationElement.Host))
				{
					var hostingAgentId = group.Key.Id;

					var mediationElement = allMediationElements
						.FirstOrDefault(e => e.DmsElement.Host.Id == hostingAgentId);

					if (mediationElement == null)
					{
						throw new InvalidOperationException($"Couldn't find MediaOps mediation element on hosting agent {hostingAgentId}");
					}

					foreach (var connection in group)
					{
						connection.MediationElement = mediationElement;
					}
				}
			}
		}

		private void FindConnectionHandlerScripts(ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				foreach (var group in takeContexts.GroupBy(x => new { x.MediationElement, x.DestinationElement }))
				{
					var mediationElement = group.Key.MediationElement;
					var destinationElement = group.Key.DestinationElement;

					var script = mediationElement.GetConnectionHandlerScriptName(destinationElement);

					foreach (var connection in group)
					{
						connection.ConnectionHandlerScript = script;
					}
				}
			}
		}

		private void NotifyPendingConnectionActions(ScriptAction action, ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var now = DateTimeOffset.Now;

				foreach (var group in takeContexts.GroupBy(x => x.MediationElement))
				{
					var mediationElement = group.Key;

					var requests = new List<Mediation.InterApp.Messages.PendingConnectionAction>();

					foreach (var connection in group)
					{
						var request = new Mediation.InterApp.Messages.PendingConnectionAction
						{
							Time = now,
							Destination = new Mediation.InterApp.Messages.EndpointInfo(connection.Destination),
						};

						switch (action)
						{
							case ScriptAction.Connect:
								request.Action = Mediation.InterApp.Messages.ConnectionAction.Connect;
								request.PendingSource = new Mediation.InterApp.Messages.EndpointInfo(connection.Source);
								break;
							case ScriptAction.Disconnect:
								request.Action = Mediation.InterApp.Messages.ConnectionAction.Disconnect;
								break;
							default:
								throw new InvalidOperationException($"Invalid action: {action}");
						}

						requests.Add(request);
					}

					var commands = InterAppCallFactory.CreateNew();

					var message = new Mediation.InterApp.Messages.NotifyPendingConnectionActionMessage { Actions = requests };
					commands.Messages.Add(message);

					commands.Send(
						_api.Connection,
						mediationElement.DmaId,
						mediationElement.ElementId,
						9000000,
						[typeof(Mediation.InterApp.Messages.NotifyPendingConnectionActionMessage)]);
				}
			}
		}

		private void ExecuteConnectionHandlerScripts(ScriptAction action, ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				foreach (var group in takeContexts.GroupBy(x => x.ConnectionHandlerScript))
				{
					var script = group.Key;

					IConnectionHandlerRequest request;

					switch (action)
					{
						case ScriptAction.Connect:
							request = new CreateConnectionsRequest
							{
								Connections = group
									.Select(x => new Mediation.Data.ConnectionInfo(x.Source, x.Destination))
									.ToArray(),
							};
							break;
						case ScriptAction.Disconnect:
							request = new DisconnectDestinationsRequest
							{
								Destinations = group
									.Select(x => new Mediation.Data.EndpointInfo(x.Destination))
									.ToArray(),
							};
							break;
						default:
							throw new InvalidOperationException($"Invalid action: {action}");
					}

					if (_api.HasEngine)
					{
						ConnectionHandlerScript.Execute(_api.Engine, script, request, performanceTracker);
					}
					else
					{
						ConnectionHandlerScript.Execute(_api.Connection, script, request, performanceTracker);
					}
				}
			}
		}

		private void WaitUntilAllConnected(ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			using (var cts = new CancellationTokenSource(_timeout))
			using (var semaphore = new SemaphoreSlim(100))
			{
				var tasks = takeContexts
					.Select(async takeContext =>
					{
						try
						{
							await semaphore.WaitAsync(cts.Token);
							return await WaitUntilConnectedAsync(takeContext, cts.Token);
						}
						catch (Exception)
						{
							return false;
						}
						finally
						{
							semaphore.Release();
						}
					});

				var result = Task.WhenAll(tasks).GetAwaiter().GetResult();
				var failedCount = result.Count(x => x == false);

				if (failedCount > 0)
				{
					throw new TimeoutException($"Failed to connect {failedCount} connections within the specified timeout of {_timeout.TotalSeconds} seconds.");
				}
			}
		}

		private async Task<bool> WaitUntilConnectedAsync(ConnectionOperationContext takeContext, CancellationToken cancellationToken)
		{
			try
			{
				return await _connectionMonitor.WaitUntilConnectedAsync(
					takeContext.Source,
					takeContext.Destination,
					cancellationToken);
			}
			catch (OperationCanceledException)
			{
				return false;
			}
		}

		private void WaitUntilAllDisconnected(ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			using (var cts = new CancellationTokenSource(_timeout))
			using (var semaphore = new SemaphoreSlim(100))
			{
				var tasks = takeContexts
					.Select(async takeContext =>
					{
						try
						{
							await semaphore.WaitAsync(cts.Token);
							return await WaitUntilDisconnectedAsync(takeContext, cts.Token);
						}
						catch (Exception)
						{
							return false;
						}
						finally
						{
							semaphore.Release();
						}
					});

				var result = Task.WhenAll(tasks).GetAwaiter().GetResult();
				var failedCount = result.Count(x => x == false);

				if (failedCount > 0)
				{
					throw new TimeoutException($"Failed to disconnect {failedCount} connections within the specified timeout of {_timeout.TotalSeconds} seconds.");
				}
			}
		}

		private async Task<bool> WaitUntilDisconnectedAsync(ConnectionOperationContext takeContext, CancellationToken cancellationToken)
		{
			try
			{
				return await _connectionMonitor.WaitUntilDisconnectedAsync(
					takeContext.Destination,
					cancellationToken);
			}
			catch (OperationCanceledException)
			{
				return false;
			}
		}
	}
}