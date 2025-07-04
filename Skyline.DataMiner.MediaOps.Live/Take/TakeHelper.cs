namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	public class TakeHelper
	{
		private readonly MediaOpsLiveApi _api;

		private bool _waitForCompletion = false;
		private TimeSpan _timeout;

		public TakeHelper(MediaOpsLiveApi api)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
		}

		public void ConfigureWaitForCompletion(bool waitForCompletion, TimeSpan timeout)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw new ArgumentException("Timeout cannot be negative.", nameof(timeout));
			}

			_waitForCompletion = waitForCompletion;
			_timeout = timeout;
		}

		public void Take(ICollection<ConnectionRequest> connectionRequests, PerformanceCollector performanceCollector)
		{
			if (connectionRequests == null)
			{
				throw new ArgumentNullException(nameof(connectionRequests));
			}

			if (performanceCollector == null)
			{
				throw new ArgumentNullException(nameof(performanceCollector));
			}

			using (var performanceTracker = new PerformanceTracker(performanceCollector))
			{
				TakeInternal(connectionRequests, performanceTracker);
			}
		}

		public void Take(ICollection<VsgConnectionRequest> vsgConnectionRequests, PerformanceCollector performanceCollector)
		{
			if (vsgConnectionRequests == null)
			{
				throw new ArgumentNullException(nameof(vsgConnectionRequests));
			}

			if (performanceCollector == null)
			{
				throw new ArgumentNullException(nameof(performanceCollector));
			}

			using (var performanceTracker = new PerformanceTracker(performanceCollector))
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

		public void Disconnect(ICollection<DisconnectRequest> disconnectRequests, PerformanceCollector performanceCollector)
		{
			if (disconnectRequests == null)
			{
				throw new ArgumentNullException(nameof(disconnectRequests));
			}

			if (performanceCollector == null)
			{
				throw new ArgumentNullException(nameof(performanceCollector));
			}

			using (var performanceTracker = new PerformanceTracker(performanceCollector))
			{
				DisconnectInternal(disconnectRequests, performanceTracker);
			}
		}

		public void Disconnect(ICollection<VsgDisconnectRequest> vsgDisconnectRequests, PerformanceCollector performanceCollector)
		{
			if (vsgDisconnectRequests == null)
			{
				throw new ArgumentNullException(nameof(vsgDisconnectRequests));
			}

			if (performanceCollector == null)
			{
				throw new ArgumentNullException(nameof(performanceCollector));
			}

			using (var performanceTracker = new PerformanceTracker(performanceCollector))
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

		private void GetDestinationElements(IDms dms, ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				foreach (var group in takeContexts.GroupBy(x => x.Destination.Element))
				{
					var elementKey = group.Key;
					var element = dms.GetElementReference(new DmsElementId(elementKey));

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
				var allMediationElements = MediationElement.GetAllMediationElements(_api).ToList();

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
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			using (var connectivityInfoProvider = new ConnectivityInfoProvider(_api, subscribe: true))
			{
				var connectionsMonitor = new ConnectionMonitor(connectivityInfoProvider);

				var tasks = new List<Task<bool>>();

				foreach (var takeContext in takeContexts)
				{
					var task = Task.Run(
						() => WaitUntilConnected(takeContext, connectionsMonitor, performanceTracker));

					tasks.Add(task);
				}

				var results = Task.WhenAll(tasks).Result;
				var failedCount = results.Count(x => !x);

				if (failedCount > 0)
				{
					throw new TimeoutException($"Failed to connect {failedCount} connections within the specified timeout of {_timeout.TotalSeconds} seconds.");
				}
			}
		}

		private bool WaitUntilConnected(ConnectionOperationContext takeContext, ConnectionMonitor connectionMonitor, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			using (var connectivityInfoProvider = new ConnectivityInfoProvider(_api, subscribe: true))
			{
				performanceTracker.AddMetadata("Source", takeContext.Source.Name);
				performanceTracker.AddMetadata("Destination", takeContext.Destination.Name);

				return connectionMonitor.WaitUntilConnected(
					takeContext.Source,
					takeContext.Destination,
					_timeout);
			}
		}

		private void WaitUntilAllDisconnected(ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			using (var connectivityInfoProvider = new ConnectivityInfoProvider(_api, subscribe: true))
			{
				var connectionMonitor = new ConnectionMonitor(connectivityInfoProvider);

				var tasks = new List<Task<bool>>();

				foreach (var takeContext in takeContexts)
				{
					var task = Task.Run(
						() => WaitUntilDisconnected(takeContext, connectionMonitor, performanceTracker));

					tasks.Add(task);
				}

				var results = Task.WhenAll(tasks).Result;
				var failedCount = results.Count(x => !x);

				if (failedCount > 0)
				{
					throw new TimeoutException($"Failed to disconnect {failedCount} connections within the specified timeout of {_timeout.TotalSeconds} seconds.");
				}
			}
		}

		private bool WaitUntilDisconnected(ConnectionOperationContext takeContext, ConnectionMonitor connectionMonitor, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			using (var connectivityInfoProvider = new ConnectivityInfoProvider(_api, subscribe: true))
			{
				performanceTracker.AddMetadata("Destination", takeContext.Destination.Name);

				return connectionMonitor.WaitUntilDisconnected(
					takeContext.Destination,
					_timeout);
			}
		}
	}
}