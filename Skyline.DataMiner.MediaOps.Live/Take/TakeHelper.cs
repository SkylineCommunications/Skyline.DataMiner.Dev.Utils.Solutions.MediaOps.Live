namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.API.Exceptions;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;
	using Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages;
	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	public class TakeHelper
	{
		private readonly MediaOpsLiveApi _api;

		protected internal TakeHelper(MediaOpsLiveApi api)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
		}

		public ICollection<EndpointConnectionResult> Take(ICollection<EndpointConnectionRequest> connectionRequests, PerformanceTracker performanceTracker, TakeOptions options = null)
		{
			if (connectionRequests == null)
			{
				throw new ArgumentNullException(nameof(connectionRequests));
			}

			if (performanceTracker == null)
			{
				throw new ArgumentNullException(nameof(performanceTracker));
			}

			try
			{
				ICollection<EndpointConnectionResult> results;

				_api.Logger?.Information($"Start connecting with {connectionRequests.Count} requests:\n" +
					$"{FormatConnectionRequests(connectionRequests)}");

				using (performanceTracker = new PerformanceTracker(performanceTracker))
				{
					var takeContexts = connectionRequests.Select(x => new EndpointConnectOperationContext(x)).ToList();

					PrepareData(takeContexts, performanceTracker);
					NotifyPendingConnectionActions(ConnectionHandlerScriptAction.Connect, takeContexts, performanceTracker);
					ExecuteConnectionHandlerScripts(ConnectionHandlerScriptAction.Connect, takeContexts, performanceTracker);
					WaitUntilAllConnected(takeContexts, performanceTracker, options);

					_api.Logger?.Information("Take finished successfully.");

					results = GenerateResults(takeContexts);
				}

				return results;
			}
			catch (Exception ex)
			{
				_api.Logger?.Error("Take failed", ex);
				throw;
			}
		}

		public ICollection<VsgConnectionResult> Take(ICollection<VsgConnectionRequest> connectionRequests, PerformanceTracker performanceTracker, TakeOptions options = null)
		{
			if (connectionRequests == null)
			{
				throw new ArgumentNullException(nameof(connectionRequests));
			}

			if (performanceTracker == null)
			{
				throw new ArgumentNullException(nameof(performanceTracker));
			}

			try
			{
				ICollection<VsgConnectionResult> results;

				_api.Logger?.Information($"Start connecting with {connectionRequests.Count} requests:\n" +
					$"{FormatConnectionRequests(connectionRequests)}");

				using (performanceTracker = new PerformanceTracker(performanceTracker))
				{
					var destinationVsgs = connectionRequests.Select(x => x.Destination).Distinct().ToList();
					CheckDestinationLocks(destinationVsgs, options, performanceTracker);

					var takeContexts = ConvertConnectionRequests(connectionRequests, performanceTracker);

					PrepareData(takeContexts, performanceTracker);
					NotifyPendingConnectionActions(ConnectionHandlerScriptAction.Connect, takeContexts, performanceTracker);
					ExecuteConnectionHandlerScripts(ConnectionHandlerScriptAction.Connect, takeContexts, performanceTracker);
					WaitUntilAllConnected(takeContexts, performanceTracker, options);

					_api.Logger?.Information("Take finished successfully.");

					results = GenerateResults(takeContexts);
				}

				return results;
			}
			catch (Exception ex)
			{
				_api.Logger?.Error("Take failed", ex);
				throw;
			}
		}

		public ICollection<EndpointDisconnectResult> Disconnect(ICollection<EndpointDisconnectRequest> disconnectRequests, PerformanceTracker performanceTracker, TakeOptions options = null)
		{
			if (disconnectRequests == null)
			{
				throw new ArgumentNullException(nameof(disconnectRequests));
			}

			if (performanceTracker == null)
			{
				throw new ArgumentNullException(nameof(performanceTracker));
			}

			try
			{
				ICollection<EndpointDisconnectResult> results;

				_api.Logger?.Information($"Start disconnecting with {disconnectRequests.Count} requests:\n" +
					$"{FormatDisconnectRequests(disconnectRequests)}");

				using (performanceTracker = new PerformanceTracker(performanceTracker))
				{
					var takeContexts = disconnectRequests.Select(x => new EndpointDisconnectOperationContext(x)).ToList();

					PrepareData(takeContexts, performanceTracker);
					NotifyPendingConnectionActions(ConnectionHandlerScriptAction.Disconnect, takeContexts, performanceTracker);
					ExecuteConnectionHandlerScripts(ConnectionHandlerScriptAction.Disconnect, takeContexts, performanceTracker);
					WaitUntilAllDisconnected(takeContexts, performanceTracker, options);

					results = GenerateResults(takeContexts);
				}

				_api.Logger?.Information("Disconnecting finished successfully.");
				return results;
			}
			catch (Exception ex)
			{
				_api.Logger?.Error("Disconnecting failed", ex);
				throw;
			}
		}

		public ICollection<VsgDisconnectResult> Disconnect(ICollection<VsgDisconnectRequest> disconnectRequests, PerformanceTracker performanceTracker, TakeOptions options = null)
		{
			if (disconnectRequests == null)
			{
				throw new ArgumentNullException(nameof(disconnectRequests));
			}

			if (performanceTracker == null)
			{
				throw new ArgumentNullException(nameof(performanceTracker));
			}

			try
			{
				ICollection<VsgDisconnectResult> results;

				_api.Logger?.Information($"Start disconnecting with {disconnectRequests.Count} requests:\n" +
					$"{FormatDisconnectRequests(disconnectRequests)}");

				using (performanceTracker = new PerformanceTracker(performanceTracker))
				{
					var destinationVsgs = disconnectRequests.Select(x => x.Destination).Distinct().ToList();
					CheckDestinationLocks(destinationVsgs, options, performanceTracker);

					var takeContexts = ConvertDisconnectRequests(disconnectRequests, performanceTracker);
					PrepareData(takeContexts, performanceTracker);
					NotifyPendingConnectionActions(ConnectionHandlerScriptAction.Disconnect, takeContexts, performanceTracker);
					ExecuteConnectionHandlerScripts(ConnectionHandlerScriptAction.Disconnect, takeContexts, performanceTracker);
					WaitUntilAllDisconnected(takeContexts, performanceTracker, options);

					results = GenerateResults(takeContexts);
				}

				_api.Logger?.Information("Disconnecting finished successfully.");
				return results;
			}
			catch (Exception ex)
			{
				_api.Logger?.Error("Disconnecting failed", ex);
				throw;
			}
		}

		private ICollection<VsgConnectOperationContext> ConvertConnectionRequests(ICollection<VsgConnectionRequest> vsgConnectionRequests, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				// create connection requests between endpoints
				var connectionRequests = new List<VsgConnectOperationContext>();

				// load all endpoints
				var endpointIds = vsgConnectionRequests
					.SelectMany(x => x.Source.GetLevelEndpoints().Concat(x.Destination.GetLevelEndpoints()))
					.Select(x => x.Endpoint.ID)
					.Distinct();

				var endpoints = LoadEndpoints(endpointIds, performanceTracker);

				// do the conversion
				foreach (var vsgConnectionRequest in vsgConnectionRequests)
				{
					var source = vsgConnectionRequest.Source;
					var destination = vsgConnectionRequest.Destination;

					ICollection<LevelMapping> levelMappings;

					if (vsgConnectionRequest.IsConnectAllLevels)
					{
						// create default level mappings
						// connect same levels between source and destination
						levelMappings = source.GetLevelEndpoints().Select(x => x.Level)
							.Intersect(destination.GetLevelEndpoints().Select(x => x.Level))
							.Select(x => new LevelMapping(x))
							.ToList();
					}
					else
					{
						levelMappings = vsgConnectionRequest.LevelMappings;
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

						var request = new VsgConnectOperationContext(vsgConnectionRequest, sourceEndpoint, destinationEndpoint);

						connectionRequests.Add(request);
					}
				}

				return connectionRequests;
			}
		}

		private ICollection<VsgDisconnectOperationContext> ConvertDisconnectRequests(ICollection<VsgDisconnectRequest> vsgDisconnectRequests, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				// create disconnect requests
				var disconnectRequests = new List<VsgDisconnectOperationContext>();

				// load all endpoints
				var endpointIds = vsgDisconnectRequests
					.SelectMany(x => x.Destination.GetLevelEndpoints())
					.Select(x => x.Endpoint.ID)
					.Distinct();

				var endpoints = LoadEndpoints(endpointIds, performanceTracker);

				// do the conversion
				foreach (var vsgDisconnectRequest in vsgDisconnectRequests)
				{
					var destination = vsgDisconnectRequest.Destination;

					foreach (var levelEndpoint in destination.GetLevelEndpoints())
					{
						if (!vsgDisconnectRequest.IsDisconnectAllLevels &&
							!vsgDisconnectRequest.Levels.Contains(levelEndpoint.Level))
						{
							continue; // skip this level if it is not in the disconnect request
						}

						if (!endpoints.TryGetValue(levelEndpoint.Endpoint, out var destinationEndpoint))
						{
							throw new InvalidOperationException($"Couldn't find endpoint with ID '{levelEndpoint.Endpoint.ID}'");
						}

						var request = new VsgDisconnectOperationContext(vsgDisconnectRequest, destinationEndpoint);

						disconnectRequests.Add(request);
					}
				}

				return disconnectRequests;
			}
		}

		private void CheckDestinationLocks(ICollection<VirtualSignalGroup> destinationVsgs, TakeOptions options, PerformanceTracker performanceTracker)
		{
			if (options != null && options.BypassLockValidation)
			{
				return;
			}

			using (new PerformanceTracker(performanceTracker))
			{
				var dict = destinationVsgs.SafeToDictionary(x => x.Reference);

				var states = _api.VirtualSignalGroupStates.GetByVirtualSignalGroups(destinationVsgs)
					.SafeToDictionary(x => dict[x.VirtualSignalGroupReference], x => x);

				foreach (var kvp in states)
				{
					var vsg = kvp.Key;
					var state = kvp.Value;

					if (state.IsLocked)
					{
						throw new DestinationLockedException(vsg, state.LockTime, state.LockedBy, state.LockReason);
					}
					else if (state.IsProtected)
					{
						throw new DestinationProtectedException(vsg, state.LockTime, state.LockedBy, state.LockReason);
					}
				}
			}
		}

		private void PrepareData(IEnumerable<TakeOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var dms = _api.GetDms();

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

		private void GetDestinationElements(IDms dms, IEnumerable<TakeOperationContext> connectionContexts, PerformanceTracker performanceTracker)
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

		private void GetMediationElements(IEnumerable<TakeOperationContext> takeContexts, PerformanceTracker performanceTracker)
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

		private void FindConnectionHandlerScripts(IEnumerable<TakeOperationContext> takeContexts, PerformanceTracker performanceTracker)
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

		private void NotifyPendingConnectionActions(ConnectionHandlerScriptAction action, IEnumerable<TakeOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var now = DateTimeOffset.UtcNow;

				foreach (var group in takeContexts.GroupBy(x => x.MediationElement))
				{
					var mediationElement = group.Key;

					var requests = new List<PendingConnectionAction>();

					if (action == ConnectionHandlerScriptAction.Connect)
					{
						foreach (var connection in group.OfType<ConnectOperationContext>())
						{
							var request = new PendingConnectionAction
							{
								Time = now,
								Destination = new Mediation.InterApp.Messages.EndpointInfo(connection.Destination),
							};

							switch (action)
							{
								case ConnectionHandlerScriptAction.Connect:
									request.Action = ConnectionAction.Connect;
									request.PendingSource = new Mediation.InterApp.Messages.EndpointInfo(connection.Source);
									break;
								case ConnectionHandlerScriptAction.Disconnect:
									request.Action = ConnectionAction.Disconnect;
									break;
								default:
									throw new InvalidOperationException($"Invalid action: {action}");
							}

							requests.Add(request);
						}
					}
					else if (action == ConnectionHandlerScriptAction.Disconnect)
					{
						foreach (var connection in group.OfType<DisconnectOperationContext>())
						{
							var request = new PendingConnectionAction
							{
								Action = ConnectionAction.Disconnect,
								Time = now,
								Destination = new Mediation.InterApp.Messages.EndpointInfo(connection.Destination),
							};

							requests.Add(request);
						}
					}
					else
					{
						throw new InvalidOperationException($"Invalid action: {action}");
					}

					_api.Logger?.Information($"Notifying {requests.Count} pending connection actions to mediation element '{mediationElement.DmsElementId}'");

					var commands = InterAppCallFactory.CreateNew();

					var message = new NotifyPendingConnectionActionMessage { Actions = requests };
					commands.Messages.Add(message);

					commands.Send(
						_api.Connection,
						mediationElement.DmaId,
						mediationElement.ElementId,
						9000000,
						[typeof(NotifyPendingConnectionActionMessage)]);
				}
			}
		}

		private void ExecuteConnectionHandlerScripts(ConnectionHandlerScriptAction action, IEnumerable<TakeOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				foreach (var group in takeContexts.GroupBy(x => x.ConnectionHandlerScript))
				{
					var script = group.Key;

					IConnectionHandlerInputData inputData = action switch
					{
						ConnectionHandlerScriptAction.Connect => new CreateConnectionsInputData
						{
							Connections = group
								.OfType<ConnectOperationContext>()
								.Select(x => new Mediation.ConnectionHandlers.Data.ConnectionInfo(x.Source, x.Destination))
								.ToArray(),
						},
						ConnectionHandlerScriptAction.Disconnect => new DisconnectDestinationsInputData
						{
							Destinations = group
								.OfType<DisconnectOperationContext>()
								.Select(x => new Mediation.ConnectionHandlers.Data.EndpointInfo(x.Destination))
								.ToArray(),
						},
						_ => throw new InvalidOperationException($"Invalid action: {action}"),
					};

					_api.Logger?.Information($"Executing connection handler script '{script}' ({action}) for {group.Count()} connections");

					ExecuteConnectionHandlerScript(script, action, inputData, performanceTracker);
				}
			}
		}

		protected virtual void ExecuteConnectionHandlerScript(string script, ConnectionHandlerScriptAction action, IConnectionHandlerInputData inputData, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var inputDataSerialized = JsonConvert.SerializeObject(inputData);

				performanceTracker.AddMetadata("Script", script);
				performanceTracker.AddMetadata("Input Data", inputDataSerialized);

				var parameters = new Dictionary<string, string>
				{
					{ "Action", Convert.ToString(action) },
					{ "Input Data", inputDataSerialized },
				};

				AutomationHelper.ExecuteAutomationScript(_api.Connection, script, parameters);
			}
		}

		private void WaitUntilAllConnected(IEnumerable<ConnectOperationContext> takeContexts, PerformanceTracker performanceTracker, TakeOptions options)
		{
			var timeout = options?.Timeout ?? TimeSpan.FromSeconds(30);
			_api.Logger?.Information($"Waiting for all connections to be established... ({timeout.TotalSeconds} seconds)");

			using (new PerformanceTracker(performanceTracker))
			using (var cts = new CancellationTokenSource(timeout))
			{
				var connectionMonitor = options?.ConnectionMonitor ??
					StaticMediaOpsLiveCache.GetOrCreate(_api.Connection).ConnectionMonitor;

				var tasks = takeContexts
					.Select(takeContext => WaitUntilConnectedAsync(takeContext, connectionMonitor, cts.Token))
					.ToArray();

				if (options?.WaitForCompletion == true)
				{
					Task.WaitAll(tasks);

					var failedRequests = takeContexts.Where(x => !x.IsSuccessful).Select(x => x.ConnectionRequest).ToList();

					if (failedRequests.Count > 0)
					{
						throw new ConnectFailedException(
							$"Failed to connect {failedRequests.Count} connections within the specified timeout of {timeout.TotalSeconds} seconds.",
							failedRequests);
					}
				}
			}
		}

		private async Task<bool> WaitUntilConnectedAsync(ConnectOperationContext takeContext, ConnectionMonitor connectionMonitor, CancellationToken cancellationToken)
		{
			bool result;

			try
			{
				result = await connectionMonitor.WaitUntilConnectedAsync(takeContext.Source, takeContext.Destination, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (Exception)
			{
				result = false;
			}

			takeContext.IsSuccessful = result;
			takeContext.SetCompleted();

			return result;
		}

		private void WaitUntilAllDisconnected(IEnumerable<DisconnectOperationContext> takeContexts, PerformanceTracker performanceTracker, TakeOptions options)
		{
			var timeout = options?.Timeout ?? TimeSpan.FromSeconds(30);
			_api.Logger?.Information($"Waiting for all disconnections to complete... ({timeout.TotalSeconds} seconds)");

			using (new PerformanceTracker(performanceTracker))
			using (var cts = new CancellationTokenSource(timeout))
			{
				var connectionMonitor = options?.ConnectionMonitor ??
					StaticMediaOpsLiveCache.GetOrCreate(_api.Connection).ConnectionMonitor;

				var tasks = takeContexts
					.Select(takeContext => WaitUntilDisconnectedAsync(takeContext, connectionMonitor, cts.Token))
					.ToArray();

				if (options?.WaitForCompletion == true)
				{
					Task.WaitAll(tasks);

					var failedRequests = takeContexts.Where(x => !x.IsSuccessful).Select(x => x.DisconnectRequest).ToList();

					if (failedRequests.Count > 0)
					{
						throw new DisconnectFailedException(
							$"Failed to disconnect {failedRequests.Count} connections within the specified timeout of {timeout.TotalSeconds} seconds.",
							failedRequests);
					}
				}
			}
		}

		private async Task<bool> WaitUntilDisconnectedAsync(DisconnectOperationContext takeContext, ConnectionMonitor connectionMonitor, CancellationToken cancellationToken)
		{
			bool result;

			try
			{
				result = await connectionMonitor.WaitUntilDisconnectedAsync(takeContext.Destination, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (Exception)
			{
				result = false;
			}

			takeContext.IsSuccessful = result;
			takeContext.SetCompleted();

			return result;
		}

		private ICollection<EndpointConnectionResult> GenerateResults(ICollection<EndpointConnectOperationContext> takeContexts)
		{
			var results = new List<EndpointConnectionResult>();

			foreach (var context in takeContexts)
			{
				var result = new EndpointConnectionResult(context.EndpointConnectionRequest)
				{
					IsSuccessful = context.IsSuccessful,
					CompletionTask = context.CompletionTask,
				};

				results.Add(result);
			}

			return results;
		}

		private ICollection<VsgConnectionResult> GenerateResults(ICollection<VsgConnectOperationContext> takeContexts)
		{
			var results = new List<VsgConnectionResult>();

			foreach (var group in takeContexts.GroupBy(x => x.VsgConnectionRequest))
			{
				var result = new VsgConnectionResult(group.Key)
				{
					IsSuccessful = group.All(x => x.IsSuccessful),
					CompletionTask = Task.WhenAll(group.Select(x => x.CompletionTask)),
				};

				results.Add(result);
			}

			return results;
		}

		private ICollection<EndpointDisconnectResult> GenerateResults(ICollection<EndpointDisconnectOperationContext> takeContexts)
		{
			var results = new List<EndpointDisconnectResult>();

			foreach (var context in takeContexts)
			{
				var result = new EndpointDisconnectResult(context.EndpointDisconnectRequest)
				{
					IsSuccessful = context.IsSuccessful,
					CompletionTask = context.CompletionTask,
				};

				results.Add(result);
			}

			return results;
		}

		private ICollection<VsgDisconnectResult> GenerateResults(ICollection<VsgDisconnectOperationContext> takeContexts)
		{
			var results = new List<VsgDisconnectResult>();

			foreach (var group in takeContexts.GroupBy(x => x.VsgDisconnectRequest))
			{
				var result = new VsgDisconnectResult(group.Key)
				{
					IsSuccessful = group.All(x => x.IsSuccessful),
					CompletionTask = Task.WhenAll(group.Select(x => x.CompletionTask)),
				};

				results.Add(result);
			}

			return results;
		}

		private static string FormatConnectionRequests(ICollection<EndpointConnectionRequest> requests)
		{
			return String.Join(
				Environment.NewLine,
				requests
					.Select(r => $" - {r.Source.Name} [{r.Source.ID}] -> {r.Destination.Name} [{r.Destination.ID}]"));
		}

		private static string FormatConnectionRequests(ICollection<VsgConnectionRequest> requests)
		{
			return String.Join(
				Environment.NewLine,
				requests
					.Select(r =>
					{
						var levels = r.LevelMappings == null || r.LevelMappings.Count == 0
							? $"all levels"
							: $"{r.LevelMappings.Count} level(s)";

						return $" - {r.Source.Name} [{r.Source.ID}] -> {r.Destination.Name} [{r.Destination.ID}] ({levels})";
					}));
		}

		private static string FormatDisconnectRequests(ICollection<EndpointDisconnectRequest> requests)
		{
			return String.Join(
				Environment.NewLine,
				requests
					.Select(r => $" - {r.Destination.Name} [{r.Destination.ID}]"));
		}

		private static string FormatDisconnectRequests(ICollection<VsgDisconnectRequest> requests)
		{
			return String.Join(
				Environment.NewLine,
				requests
					.Select(r =>
					{
						var levels = r.Levels == null || r.Levels.Count == 0
							? $"all levels"
							: $"{r.Levels.Count} level(s)";

						return $" - {r.Destination.Name} [{r.Destination.ID}] ({levels})";
					}));
		}
	}
}