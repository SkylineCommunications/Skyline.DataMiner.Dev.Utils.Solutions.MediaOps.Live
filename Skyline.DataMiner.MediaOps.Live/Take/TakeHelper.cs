namespace Skyline.DataMiner.Solutions.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.ConnectionHandlers;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.ConnectionHandlers.Data;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.InterApp.Messages;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Tools;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	public class TakeHelper
	{
		internal TakeHelper(MediaOpsLiveApi api)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
		}

		internal MediaOpsLiveApi Api { get; }

		public ICollection<EndpointConnectionResult> Take(ICollection<EndpointConnectionRequest> connectionRequests, PerformanceTracker performanceTracker, TakeOptions options = null)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				return TakeAsync(connectionRequests, performanceTracker, options).GetAwaiter().GetResult();
			}
		}

		public ICollection<VsgConnectionResult> Take(ICollection<VsgConnectionRequest> connectionRequests, PerformanceTracker performanceTracker, TakeOptions options = null)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				return TakeAsync(connectionRequests, performanceTracker, options).GetAwaiter().GetResult();
			}
		}

		public async Task<ICollection<EndpointConnectionResult>> TakeAsync(ICollection<EndpointConnectionRequest> connectionRequests, PerformanceTracker performanceTracker, TakeOptions options = null)
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

				Api.Logger?.Information($"Start connecting with {connectionRequests.Count} requests:\n" +
					$"{FormatConnectionRequests(connectionRequests)}");

				using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(TakeHelper), nameof(TakeAsync)))
				{
					var takeContexts = connectionRequests.Select(x => new EndpointConnectOperationContext(x)).ToList();

					Validate(takeContexts, performanceTracker);
					PrepareData(ConnectionHandlerScriptAction.Connect, takeContexts, performanceTracker);
					NotifyPendingConnectionActions(ConnectionHandlerScriptAction.Connect, takeContexts, performanceTracker);
					ExecuteConnectionHandlerScripts(ConnectionHandlerScriptAction.Connect, takeContexts, performanceTracker);
					await WaitUntilAllConnectedAsync(takeContexts, performanceTracker, options);

					Api.Logger?.Information("Take finished successfully.");

					results = GenerateResults(takeContexts);
				}

				return results;
			}
			catch (Exception ex)
			{
				Api.Logger?.Error("Take failed", ex);
				throw;
			}
		}

		public async Task<ICollection<VsgConnectionResult>> TakeAsync(ICollection<VsgConnectionRequest> connectionRequests, PerformanceTracker performanceTracker, TakeOptions options = null)
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

				Api.Logger?.Information($"Start connecting with {connectionRequests.Count} requests:\n" +
					$"{FormatConnectionRequests(connectionRequests)}");

				using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(TakeHelper), nameof(TakeAsync)))
				{
					var destinationVsgs = connectionRequests.Select(x => x.Destination).Distinct().ToList();
					CheckDestinationLocks(destinationVsgs, options, performanceTracker);

					var takeContexts = ConvertConnectionRequests(connectionRequests, performanceTracker);

					Validate(takeContexts, performanceTracker);
					PrepareData(ConnectionHandlerScriptAction.Connect, takeContexts, performanceTracker);
					NotifyPendingConnectionActions(ConnectionHandlerScriptAction.Connect, takeContexts, performanceTracker);
					ExecuteConnectionHandlerScripts(ConnectionHandlerScriptAction.Connect, takeContexts, performanceTracker);
					await WaitUntilAllConnectedAsync(takeContexts, performanceTracker, options);

					Api.Logger?.Information("Take finished successfully.");

					results = GenerateResults(takeContexts);
				}

				return results;
			}
			catch (Exception ex)
			{
				Api.Logger?.Error("Take failed", ex);
				throw;
			}
		}

		public ICollection<EndpointDisconnectResult> Disconnect(ICollection<EndpointDisconnectRequest> disconnectRequests, PerformanceTracker performanceTracker, DisconnectOptions options = null)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				return DisconnectAsync(disconnectRequests, performanceTracker, options).GetAwaiter().GetResult();
			}
		}

		public ICollection<VsgDisconnectResult> Disconnect(ICollection<VsgDisconnectRequest> disconnectRequests, PerformanceTracker performanceTracker, DisconnectOptions options = null)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				return DisconnectAsync(disconnectRequests, performanceTracker, options).GetAwaiter().GetResult();
			}
		}

		public async Task<ICollection<EndpointDisconnectResult>> DisconnectAsync(ICollection<EndpointDisconnectRequest> disconnectRequests, PerformanceTracker performanceTracker, DisconnectOptions options = null)
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

				Api.Logger?.Information($"Start disconnecting with {disconnectRequests.Count} requests:\n" +
					$"{FormatDisconnectRequests(disconnectRequests)}");

				using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(TakeHelper), nameof(DisconnectAsync)))
				{
					var takeContexts = disconnectRequests.Select(x => new EndpointDisconnectOperationContext(x)).ToList();

					PrepareData(ConnectionHandlerScriptAction.Disconnect, takeContexts, performanceTracker);
					NotifyPendingConnectionActions(ConnectionHandlerScriptAction.Disconnect, takeContexts, performanceTracker);
					ExecuteConnectionHandlerScripts(ConnectionHandlerScriptAction.Disconnect, takeContexts, performanceTracker);
					await WaitUntilAllDisconnectedAsync(takeContexts, performanceTracker, options);

					results = GenerateResults(takeContexts);
				}

				Api.Logger?.Information("Disconnecting finished successfully.");
				return results;
			}
			catch (Exception ex)
			{
				Api.Logger?.Error("Disconnecting failed", ex);
				throw;
			}
		}

		public async Task<ICollection<VsgDisconnectResult>> DisconnectAsync(ICollection<VsgDisconnectRequest> disconnectRequests, PerformanceTracker performanceTracker, DisconnectOptions options = null)
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

				Api.Logger?.Information($"Start disconnecting with {disconnectRequests.Count} requests:\n" +
					$"{FormatDisconnectRequests(disconnectRequests)}");

				using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(TakeHelper), nameof(DisconnectAsync)))
				{
					var destinationVsgs = disconnectRequests.Select(x => x.Destination).Distinct().ToList();
					CheckDestinationLocks(destinationVsgs, options, performanceTracker);

					var takeContexts = ConvertDisconnectRequests(disconnectRequests, performanceTracker);
					PrepareData(ConnectionHandlerScriptAction.Disconnect, takeContexts, performanceTracker);
					NotifyPendingConnectionActions(ConnectionHandlerScriptAction.Disconnect, takeContexts, performanceTracker);
					ExecuteConnectionHandlerScripts(ConnectionHandlerScriptAction.Disconnect, takeContexts, performanceTracker);
					await WaitUntilAllDisconnectedAsync(takeContexts, performanceTracker, options);

					results = GenerateResults(takeContexts);
				}

				Api.Logger?.Information("Disconnecting finished successfully.");
				return results;
			}
			catch (Exception ex)
			{
				Api.Logger?.Error("Disconnecting failed", ex);
				throw;
			}
		}

		private ICollection<VsgEndpointConnectOperationContext> ConvertConnectionRequests(ICollection<VsgConnectionRequest> vsgConnectionRequests, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				// create connection requests between endpoints
				var connectionRequests = new List<VsgEndpointConnectOperationContext>();

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

						var request = new VsgEndpointConnectOperationContext(vsgConnectionRequest, sourceEndpoint, destinationEndpoint);

						connectionRequests.Add(request);
					}
				}

				return connectionRequests;
			}
		}

		private ICollection<VsgEndpointDisconnectOperationContext> ConvertDisconnectRequests(ICollection<VsgDisconnectRequest> vsgDisconnectRequests, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				// create disconnect requests
				var disconnectRequests = new List<VsgEndpointDisconnectOperationContext>();

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

						var request = new VsgEndpointDisconnectOperationContext(vsgDisconnectRequest, destinationEndpoint);

						disconnectRequests.Add(request);
					}
				}

				return disconnectRequests;
			}
		}

		private void CheckDestinationLocks(ICollection<VirtualSignalGroup> destinationVsgs, OptionsBase options, PerformanceTracker performanceTracker)
		{
			if (options != null && options.BypassLockValidation)
			{
				return;
			}

			using (new PerformanceTracker(performanceTracker))
			{
				var dict = destinationVsgs.SafeToDictionary(x => x.Reference);

				var states = Api.VirtualSignalGroupStates.GetByVirtualSignalGroups(destinationVsgs)
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

		private void Validate(IEnumerable<ConnectOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var transportTypesCache = Api.GetCache().TransportTypesCache;

				foreach (var context in takeContexts)
				{
					if (context.Source.TransportType != context.Destination.TransportType)
					{
						transportTypesCache.TryGetTransportType(context.Source.TransportType, out var sourceTransportType);
						transportTypesCache.TryGetTransportType(context.Destination.TransportType, out var destinationTransportType);

						throw new InvalidOperationException($"Cannot connect endpoints with different transport types: " +
							$"source '{context.Source.Name}' has transport type '{sourceTransportType?.Name}', " +
							$"destination '{context.Destination.Name}' has transport type '{destinationTransportType?.Name}'.");
					}

					// Additional validations can be added here
				}
			}
		}

		private void PrepareData(ConnectionHandlerScriptAction action, IEnumerable<TakeOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var dms = Api.Connection.GetDms();

				GetDestinationElements(dms, takeContexts, performanceTracker);
				GetMediationElements(takeContexts, performanceTracker);
				FindConnectionHandlerScripts(takeContexts, performanceTracker);
				ApplyConnectionHandlerScriptConfigurations(action, takeContexts, performanceTracker);
			}
		}

		private IDictionary<Guid, Endpoint> LoadEndpoints(IEnumerable<Guid> endpointIds, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var result = Api.Endpoints.Read(endpointIds);

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
					foreach (var context in group)
					{
						context.DestinationElement = element;
					}
				}
			}
		}

		private void GetMediationElements(IEnumerable<TakeOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var allMediationElements = Api.MediationElements.GetAllElementsCached();

				foreach (var group in takeContexts
					.Where(x => x.DestinationElement != null)
					.GroupBy(x => x.DestinationElement.Host))
				{
					var hostingAgentId = group.Key.Id;

					var mediationElement = allMediationElements
						.FirstOrDefault(e => e.DmsElement.Host.Id == hostingAgentId);

					if (mediationElement == null)
					{
						throw new InvalidOperationException($"Couldn't find MediaOps mediation element on hosting agent {hostingAgentId}");
					}

					foreach (var context in group)
					{
						context.MediationElement = mediationElement;
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

					foreach (var context in group)
					{
						context.ConnectionHandlerScript = script;
					}
				}
			}
		}

		private void ApplyConnectionHandlerScriptConfigurations(ConnectionHandlerScriptAction action, IEnumerable<TakeOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				foreach (var group in takeContexts.GroupBy(x => x.ConnectionHandlerScript))
				{
					ConnectionHandlerConfiguration config;

					try
					{
						config = LoadConnectionHandlerConfiguration(group.Key, performanceTracker);
					}
					catch (Exception)
					{
						// Couldn't get configuration, apply defaults
						config = ConnectionHandlerConfiguration.Default;
					}

					foreach (var context in group)
					{
						if (!context.Timeout.HasValue)
						{
							context.Timeout = action switch
							{
								ConnectionHandlerScriptAction.Connect => config.ConnectTimeout,
								ConnectionHandlerScriptAction.Disconnect => config.DisconnectTimeout,
								_ => throw new InvalidOperationException($"Invalid action: {action}"),
							};
						}
					}
				}
			}
		}

		private ConnectionHandlerConfiguration LoadConnectionHandlerConfiguration(string scriptName, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var config = ExecuteConnectionHandlerScript<ConnectionHandlerConfiguration>(
					scriptName,
					ConnectionHandlerScriptAction.GetConfiguration,
					null,
					performanceTracker);

				return config ?? ConnectionHandlerConfiguration.Default;
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

					foreach (var context in group)
					{
						var request = new PendingConnectionAction
						{
							Time = now,
							Timeout = context.Timeout ?? ConnectionHandlerConfiguration.DefaultTimeout,
							ConnectionHandlerScript = context.ConnectionHandlerScript,
							Destination = new Mediation.InterApp.Messages.EndpointInfo(context.Destination),
						};

						switch (action)
						{
							case ConnectionHandlerScriptAction.Connect:
								request.Action = ConnectionAction.Connect;
								if (context is ConnectOperationContext connectContext)
								{
									request.PendingSource = new Mediation.InterApp.Messages.EndpointInfo(connectContext.Source);
								}

								break;

							case ConnectionHandlerScriptAction.Disconnect:
								request.Action = ConnectionAction.Disconnect;
								break;

							default:
								throw new InvalidOperationException($"Invalid action: {action}");
						}

						requests.Add(request);
					}

					Api.Logger?.Information($"Notifying {requests.Count} pending connection actions to mediation element '{mediationElement.DmsElementId}'");

					var commands = InterAppCallFactory.CreateNew();

					var message = new NotifyPendingConnectionActionMessage
					{
						Actions = requests,
					};
					commands.Messages.Add(message);

					commands.Send(
						Api.Connection,
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

					Api.Logger?.Information($"Executing connection handler script '{script}' ({action}) for {group.Count()} connections");

					ExecuteConnectionHandlerScript(script, action, inputData, performanceTracker);
				}
			}
		}

		protected virtual void ExecuteConnectionHandlerScript(string script, ConnectionHandlerScriptAction action, IConnectionHandlerInputData inputData, PerformanceTracker performanceTracker)
		{
			ExecuteConnectionHandlerScriptCore(script, action, inputData, performanceTracker);
		}

		protected virtual T ExecuteConnectionHandlerScript<T>(string script, ConnectionHandlerScriptAction action, IConnectionHandlerInputData inputData, PerformanceTracker performanceTracker)
		{
			var result = ExecuteConnectionHandlerScriptCore(script, action, inputData, performanceTracker);

			if (!result.ScriptOutput.TryGetValue("output", out var outputValue))
			{
				throw new InvalidOperationException("Couldn't find script output 'output'");
			}

			return JsonConvert.DeserializeObject<T>(outputValue);
		}

		private Net.Messages.ExecuteScriptResponseMessage ExecuteConnectionHandlerScriptCore(string script, ConnectionHandlerScriptAction action, IConnectionHandlerInputData inputData, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var inputDataSerialized = inputData != null ? JsonConvert.SerializeObject(inputData) : "{}";

				performanceTracker.AddMetadata("Script", script);
				performanceTracker.AddMetadata("Input Data", inputDataSerialized);

				var parameters = new Dictionary<string, string>
				{
					{ "Action", Convert.ToString(action) },
					{ "Input Data", inputDataSerialized },
				};

				try
				{
					return AutomationHelper.ExecuteAutomationScript(Api.Connection, script, parameters);
				}
				catch (ScriptExecutionFailedException ex) when (
					ex.ScriptOutput.TryGetValue("Exception.Message", out var exceptionMessage) &&
					!String.IsNullOrWhiteSpace(exceptionMessage))
				{
					throw new ConnectionHandlerScriptExecutionFailedException(exceptionMessage, ex);
				}
				catch (Exception ex)
				{
					throw new ConnectionHandlerScriptExecutionFailedException($"Connection handler script execution failed: {ex.Message}", ex);
				}
			}
		}

		private async Task WaitUntilAllConnectedAsync(IEnumerable<ConnectOperationContext> takeContexts, PerformanceTracker performanceTracker, TakeOptions options)
		{
			Api.Logger?.Information($"Waiting for all connections to be established...");

			using (new PerformanceTracker(performanceTracker, nameof(TakeHelper), nameof(WaitUntilAllConnectedAsync)))
			{
				var connectionMonitor = options?.ConnectionMonitor ?? Api.GetCache().ConnectionMonitor;

				var tasks = takeContexts
					.Select(takeContext => WaitUntilConnectedAsync(takeContext, connectionMonitor))
					.ToArray();

				if (options?.WaitForCompletion == true)
				{
					await Task.WhenAll(tasks);

					var failedRequests = takeContexts.Where(x => !x.IsSuccessful).Select(x => x.ConnectionRequest).ToList();

					if (failedRequests.Count > 0)
					{
						throw new ConnectFailedException(
							$"Failed to connect {failedRequests.Count} connections before the timeout.",
							failedRequests);
					}
				}
			}
		}

		private async Task<bool> WaitUntilConnectedAsync(ConnectOperationContext takeContext, ConnectionMonitor connectionMonitor)
		{
			bool result;

			try
			{
				using var cts = new CancellationTokenSource(takeContext.Timeout ?? ConnectionHandlerConfiguration.DefaultTimeout);

				result = await connectionMonitor.WaitUntilConnectedAsync(takeContext.Source, takeContext.Destination, cts.Token)
					.ConfigureAwait(false);
			}
			catch (Exception)
			{
				result = false;
			}

			takeContext.IsSuccessful = result;
			takeContext.SetCompleted(result);

			return result;
		}

		private async Task WaitUntilAllDisconnectedAsync(IEnumerable<DisconnectOperationContext> takeContexts, PerformanceTracker performanceTracker, DisconnectOptions options)
		{
			Api.Logger?.Information($"Waiting for all disconnections to complete...");

			using (new PerformanceTracker(performanceTracker, nameof(TakeHelper), nameof(WaitUntilAllDisconnectedAsync)))
			{
				var connectionMonitor = options?.ConnectionMonitor ?? Api.GetCache().ConnectionMonitor;

				var tasks = takeContexts
					.Select(takeContext => WaitUntilDisconnectedAsync(takeContext, connectionMonitor))
					.ToArray();

				if (options?.WaitForCompletion == true)
				{
					await Task.WhenAll(tasks);

					var failedRequests = takeContexts.Where(x => !x.IsSuccessful).Select(x => x.DisconnectRequest).ToList();

					if (failedRequests.Count > 0)
					{
						throw new DisconnectFailedException(
							$"Failed to disconnect {failedRequests.Count} connections before the timeout.",
							failedRequests);
					}
				}
			}
		}

		private async Task<bool> WaitUntilDisconnectedAsync(DisconnectOperationContext takeContext, ConnectionMonitor connectionMonitor)
		{
			bool result;

			try
			{
				using var cts = new CancellationTokenSource(takeContext.Timeout ?? ConnectionHandlerConfiguration.DefaultTimeout);

				result = await connectionMonitor.WaitUntilDisconnectedAsync(takeContext.Destination, cts.Token)
					.ConfigureAwait(false);
			}
			catch (Exception)
			{
				result = false;
			}

			takeContext.IsSuccessful = result;
			takeContext.SetCompleted(result);

			return result;
		}

		private ICollection<EndpointConnectionResult> GenerateResults(ICollection<EndpointConnectOperationContext> takeContexts)
		{
			var results = new List<EndpointConnectionResult>();

			foreach (var context in takeContexts)
			{
				var result = new EndpointConnectionResult(
					context.EndpointConnectionRequest,
					context.IsSuccessful,
					context.CompletionTask);

				results.Add(result);
			}

			return results;
		}

		private ICollection<VsgConnectionResult> GenerateResults(ICollection<VsgEndpointConnectOperationContext> takeContexts)
		{
			var results = new List<VsgConnectionResult>();

			foreach (var group in takeContexts.GroupBy(x => x.VsgConnectionRequest))
			{
				var isSuccessful = group.All(x => x.IsSuccessful);

				var completionTask = Task.WhenAll(group.Select(x => x.CompletionTask))
					.ContinueWith(
						t => t.Status == TaskStatus.RanToCompletion && t.Result.All(r => r),
						TaskContinuationOptions.ExecuteSynchronously);

				var result = new VsgConnectionResult(group.Key, isSuccessful, completionTask);

				results.Add(result);
			}

			return results;
		}

		private ICollection<EndpointDisconnectResult> GenerateResults(ICollection<EndpointDisconnectOperationContext> takeContexts)
		{
			var results = new List<EndpointDisconnectResult>();

			foreach (var context in takeContexts)
			{
				var result = new EndpointDisconnectResult(
					context.EndpointDisconnectRequest,
					context.IsSuccessful,
					context.CompletionTask);

				results.Add(result);
			}

			return results;
		}

		private ICollection<VsgDisconnectResult> GenerateResults(ICollection<VsgEndpointDisconnectOperationContext> takeContexts)
		{
			var results = new List<VsgDisconnectResult>();

			foreach (var group in takeContexts.GroupBy(x => x.VsgDisconnectRequest))
			{
				var isSuccessful = group.All(x => x.IsSuccessful);

				var completionTask = Task.WhenAll(group.Select(x => x.CompletionTask))
					.ContinueWith(
						t => t.Status == TaskStatus.RanToCompletion && t.Result.All(r => r),
						TaskContinuationOptions.ExecuteSynchronously);

				var result = new VsgDisconnectResult(group.Key, isSuccessful, completionTask);
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