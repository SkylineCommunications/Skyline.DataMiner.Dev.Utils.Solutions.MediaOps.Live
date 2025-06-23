namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation;
	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;
	using Skyline.DataMiner.Utils.DOM.Extensions;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	public class TakeHelper
	{
		private readonly MediaOpsLiveApi _api;

		public TakeHelper(MediaOpsLiveApi api)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
		}

		public void Take(IEngine engine, ICollection<ConnectionRequest> connectionRequests, PerformanceCollector performanceCollector)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

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
				TakeInternal(engine, connectionRequests, performanceTracker);
			}
		}

		public void Take(IEngine engine, ICollection<VsgConnectionRequest> vsgConnectionRequests, PerformanceCollector performanceCollector)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

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

				TakeInternal(engine, connectionRequests, performanceTracker);
			}
		}

		private void TakeInternal(IEngine engine, ICollection<ConnectionRequest> connectionRequests, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var takeContexts = connectionRequests
					.Select(x => new ConnectionOperationContext(x))
					.ToList();

				var notifyPendingConnectionsTask = StartNotifyPendingConnectionsTask(takeContexts, out var lockAcquired, performanceTracker);

				using (var connectionWatcher = SubscribeDomConnections(performanceTracker))
				{
					try
					{
						// Ensure the lock is acquired before proceeding
						// this prevents the connection handler script to already update the connections before we have set the pending source
						lockAcquired.Task.Wait();

						FindConnectionHandlerScripts(engine, takeContexts, performanceTracker);
						ExecuteConnectionHandlerScripts(engine, ScriptAction.Connect, takeContexts, performanceTracker);

						// Notify pending connections task runs in parallel with setting up the connections
						notifyPendingConnectionsTask.Wait();
						WaitUntilAllConnected(engine, connectionWatcher, takeContexts, performanceTracker);
					}
					finally
					{
						notifyPendingConnectionsTask.Wait();
						HandleFailedConnections(takeContexts, performanceTracker);
					}
				}
			}
		}

		public void Disconnect(IEngine engine, ICollection<DisconnectRequest> disconnectRequests, PerformanceCollector performanceCollector)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

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
				DisconnectInternal(engine, disconnectRequests, performanceTracker);
			}
		}

		public void Disconnect(IEngine engine, ICollection<VsgDisconnectRequest> vsgDisconnectRequests, PerformanceCollector performanceCollector)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

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

				DisconnectInternal(engine, disconnectRequests, performanceTracker);
			}
		}

		private void DisconnectInternal(IEngine engine, ICollection<DisconnectRequest> disconnectRequests, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var takeContexts = disconnectRequests
					.Select(x => new ConnectionOperationContext(x))
					.ToList();

				FindConnectionHandlerScripts(engine, takeContexts, performanceTracker);
				ExecuteConnectionHandlerScripts(engine, ScriptAction.Disconnect, takeContexts, performanceTracker);
			}
		}

		private Task StartNotifyPendingConnectionsTask(ICollection<ConnectionOperationContext> takeContexts, out TaskCompletionSource<bool> lockAcquired, PerformanceTracker performanceTracker)
		{
			var localLockAcquired = new TaskCompletionSource<bool>();
			lockAcquired = localLockAcquired; // assign to the out parameter

			return Task.Factory.StartNew(
				() =>
				{
					var destinationEndpointIds = takeContexts.Select(x => x.Destination.ID).ToList();

					using (CreateConnectionUpdateLock(destinationEndpointIds, performanceTracker))
					{
						localLockAcquired.SetResult(true); // Signal that lock is acquired
						GetOrCreateDomConnections(takeContexts, performanceTracker);
						NotifyPendingConnections(takeContexts, performanceTracker);
					}
				},
				TaskCreationOptions.LongRunning);
		}

		private void HandleFailedConnections(ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			var failedConnections = takeContexts.Where(x => !x.HasSucceeded).ToList();
			if (failedConnections.Count == 0)
			{
				return;
			}

			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var destinationEndpointIds = failedConnections.Select(x => x.Destination.ID).ToList();

				using (CreateConnectionUpdateLock(destinationEndpointIds, performanceTracker))
				{
					// refresh DOM connections
					GetOrCreateDomConnections(failedConnections, performanceTracker);

					ClearPendingSourceOnFailedConnections(failedConnections, performanceTracker);
				}
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

		private void GetOrCreateDomConnections(ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var connections = _api.SlcConnectivityManagementHelper.GetConnectionsForDestinations(takeContexts.Select(x => x.Destination.ID));

				foreach (var takeContext in takeContexts)
				{
					if (!connections.TryGetValue(takeContext.Destination.ID, out var connection))
					{
						connection = new ConnectionInstance
						{
							ConnectionInfo = new ConnectionInfoSection
							{
								Destination = takeContext.Destination.ID,
								IsConnected = false,
							},
						};
					}

					takeContext.DomConnection = connection;
				}
			}
		}

		private void WaitUntilAllConnected(IEngine engine, ConnectionWatcher connectionWatcher, ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var tasks = new List<Task>();

				foreach (var takeContext in takeContexts)
				{
					var task = Task.Factory.StartNew(
						() =>
						{
							takeContext.HasSucceeded = IsConnectionEstablished(engine, connectionWatcher, takeContext, performanceTracker);
						},
						TaskCreationOptions.LongRunning);

					tasks.Add(task);
				}

				Task.WaitAll(tasks.ToArray());
			}
		}

		private bool IsConnectionEstablished(IEngine engine, ConnectionWatcher connectionWatcher, ConnectionOperationContext takeContext, PerformanceTracker performanceTracker)
		{
			if (takeContext.DomConnection?.ConnectionInfo?.ConnectedSource == takeContext.Source?.ID)
			{
				// no need to wait, they were already connected
				return true;
			}

			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				performanceTracker.AddMetadata("Source", takeContext.Source?.Name);
				performanceTracker.AddMetadata("Destination", takeContext.Destination?.Name);

				return ConnectionAwaiter.Wait(engine, connectionWatcher, takeContext.Source, takeContext.Destination, TimeSpan.FromSeconds(5));
			}
		}

		private void FindConnectionHandlerScripts(IEngine engine, ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var dms = engine.GetDms();

				foreach (var group in takeContexts.GroupBy(x => x.Destination.Element))
				{
					var elementKey = group.Key;
					var element = dms.GetElementReference(new DmsElementId(elementKey));

					var script = ConnectionHandlerScript.FindScriptForElement(engine, element);

					if (String.IsNullOrWhiteSpace(script))
					{
						throw new InvalidOperationException($"Couldn't determine connection handler script for element {element}");
					}

					foreach (var connection in group)
					{
						connection.ConnectionHandlerScript = script;
					}
				}
			}
		}

		private void ExecuteConnectionHandlerScripts(IEngine engine, ScriptAction action, ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
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
								Connections = group.Select(x => ConnectionInfo.Create(x.Source, x.Destination)).ToArray(),
							};
							break;
						case ScriptAction.Disconnect:
							request = new DisconnectDestinationsRequest
							{
								Destinations = group.Select(x => EndpointInfo.Create(x.Destination)).ToArray(),
							};
							break;
						default:
							throw new InvalidOperationException($"Invalid action: {action}");
					}

					ConnectionHandlerScript.Execute(engine, script, request, performanceTracker);
				}
			}
		}

		private void NotifyPendingConnections(ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var updatedConnections = new List<ConnectionInstance>();

				foreach (var takeContext in takeContexts)
				{
					var connection = takeContext.DomConnection;
					var sourceId = takeContext.Source?.ID;

					if (connection.ConnectionInfo.ConnectedSource != sourceId &&
						connection.ConnectionInfo.PendingConnectedSource != sourceId)
					{
						connection.ConnectionInfo.PendingConnectedSource = sourceId;
						updatedConnections.Add(connection);
					}
				}

				performanceTracker.AddMetadata("Number of Connections", Convert.ToString(updatedConnections.Count));

				if (updatedConnections.Count > 0)
				{
					_api.SlcConnectivityManagementHelper.DomHelper.DomInstances.CreateOrUpdateInBatches(updatedConnections.Select(x => x.ToInstance())).ThrowOnFailure();
				}
			}
		}

		private void ClearPendingSourceOnFailedConnections(ICollection<ConnectionOperationContext> failedConnections, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				// update connections
				var updatedConnections = new List<ConnectionInstance>();

				foreach (var takeContext in failedConnections)
				{
					var connection = takeContext.DomConnection;
					var sourceId = takeContext.Source?.ID;

					if (connection.ConnectionInfo.PendingConnectedSource != null)
					{
						if (connection.ConnectionInfo.PendingConnectedSource != sourceId)
						{
							// this is already a pending source from another connect, skip updating this connection
							continue;
						}

						connection.ConnectionInfo.PendingConnectedSource = null;
						updatedConnections.Add(connection);
					}
				}

				performanceTracker.AddMetadata("Number of Connections", Convert.ToString(updatedConnections.Count));

				if (updatedConnections.Count > 0)
				{
					_api.SlcConnectivityManagementHelper.DomHelper.DomInstances.CreateOrUpdateInBatches(updatedConnections.Select(x => x.ToInstance())).ThrowOnFailure();
				}
			}
		}

		private ConnectionWatcher SubscribeDomConnections(PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				return new ConnectionWatcher();
			}
		}

		private MultiConnectionUpdateLock CreateConnectionUpdateLock(ICollection<Guid> destinationEndpointIds, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				return new MultiConnectionUpdateLock(destinationEndpointIds);
			}
		}
	}
}