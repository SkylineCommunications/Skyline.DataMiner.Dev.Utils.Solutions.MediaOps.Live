namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation;
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
				var endpoints = LoadEndpoints(vsgConnectionRequests, performanceTracker);

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
						levelMappings = source.Levels.Select(x => x.Level)
							.Intersect(destination.Levels.Select(x => x.Level))
							.Select(x => new LevelMapping(x.Value))
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
				var connectionContexts = connectionRequests
					.Select(x => new CreateConnectionContext(x))
					.ToList();

				var notifyPendingConnectionsTask = StartNotifyPendingConnectionsTask(connectionContexts, out var lockAcquired, performanceTracker);

				using (var connectionWatcher = SubscribeDomConnections(performanceTracker))
				{
					try
					{
						// Ensure the lock is acquired before proceeding
						// this prevents the connection handler script to already update the connections before we have set the pending source
						lockAcquired.Task.Wait();

						FindConnectionHandlerScripts(engine, connectionContexts, performanceTracker);
						ExecuteConnectionHandlerScripts(engine, connectionContexts, performanceTracker);

						// Notify pending connections task runs in parallel with setting up the connections
						notifyPendingConnectionsTask.Wait();
						WaitUntilAllConnected(engine, connectionWatcher, connectionContexts, performanceTracker);
					}
					finally
					{
						notifyPendingConnectionsTask.Wait();
						HandleFailedConnections(connectionContexts, performanceTracker);
					}
				}
			}
		}

		private Task StartNotifyPendingConnectionsTask(ICollection<CreateConnectionContext> connectionContexts, out TaskCompletionSource<bool> lockAcquired, PerformanceTracker performanceTracker)
		{
			var localLockAcquired = new TaskCompletionSource<bool>();
			lockAcquired = localLockAcquired; // assign to the out parameter

			return Task.Factory.StartNew(
				() =>
				{
					var destinationEndpointIds = connectionContexts.Select(x => x.Destination.ID).ToList();

					using (CreateConnectionUpdateLock(destinationEndpointIds, performanceTracker))
					{
						localLockAcquired.SetResult(true); // Signal that lock is acquired
						GetOrCreateDomConnections(connectionContexts, performanceTracker);
						NotifyPendingConnections(connectionContexts, performanceTracker);
					}
				},
				TaskCreationOptions.LongRunning);
		}

		private void HandleFailedConnections(ICollection<CreateConnectionContext> connectionContexts, PerformanceTracker performanceTracker)
		{
			var failedConnections = connectionContexts.Where(x => !x.HasSucceeded).ToList();
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

		private IDictionary<Guid, Endpoint> LoadEndpoints(ICollection<VsgConnectionRequest> vsgConnectionRequests, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var endpointIds = vsgConnectionRequests.SelectMany(x => x.Source.GetEndpoints())
						.Concat(vsgConnectionRequests.SelectMany(x => x.Destination.GetEndpoints()))
						.Select(x => x.ID)
						.Distinct();

				var result = _api.Endpoints.Read(endpointIds);

				performanceTracker.AddMetadata("Number of Endpoints", Convert.ToString(result.Count));

				return result;
			}
		}

		private void GetOrCreateDomConnections(ICollection<CreateConnectionContext> connectionContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var connections = _api.SlcConnectivityManagementHelper.GetConnectionsForDestinations(connectionContexts.Select(x => x.Destination.ID));

				foreach (var connectionToCreate in connectionContexts)
				{
					if (!connections.TryGetValue(connectionToCreate.Destination.ID, out var connection))
					{
						connection = new ConnectionInstance
						{
							ConnectionInfo = new ConnectionInfoSection
							{
								Destination = connectionToCreate.Destination.ID,
								IsConnected = false,
							},
						};
					}

					connectionToCreate.DomConnection = connection;
				}
			}
		}

		private void WaitUntilAllConnected(IEngine engine, ConnectionWatcher connectionWatcher, ICollection<CreateConnectionContext> connectionContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var tasks = new List<Task>();

				foreach (var connectionToCreate in connectionContexts)
				{
					var task = Task.Factory.StartNew(
						() =>
						{
							connectionToCreate.HasSucceeded = IsConnectionEstablished(engine, connectionWatcher, connectionToCreate, performanceTracker);
						},
						TaskCreationOptions.LongRunning);

					tasks.Add(task);
				}

				Task.WaitAll(tasks.ToArray());
			}
		}

		private bool IsConnectionEstablished(IEngine engine, ConnectionWatcher connectionWatcher, CreateConnectionContext connectionContexts, PerformanceTracker performanceTracker)
		{
			if (connectionContexts.DomConnection?.ConnectionInfo?.ConnectedSource == connectionContexts.Source?.ID)
			{
				// no need to wait, they were already connected
				return true;
			}

			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				performanceTracker.AddMetadata("Source", connectionContexts.Source?.Name);
				performanceTracker.AddMetadata("Destination", connectionContexts.Destination?.Name);

				return ConnectionAwaiter.Wait(engine, connectionWatcher, connectionContexts.Source, connectionContexts.Destination, TimeSpan.FromSeconds(5));
			}
		}

		private string FindConnectionHandlerScript(IEngine engine, IDmsElement element)
		{
			var hostingAgentId = element.Host.Id;

			var mediationElement = engine.FindElementsByProtocol(Constants.MediationProtocolName)
				.FirstOrDefault(e => e.RawInfo.HostingAgentID == hostingAgentId);

			if (mediationElement == null)
			{
				throw new InvalidOperationException($"Couldn't find MediaOps mediation element on hosting agent {hostingAgentId}");
			}

			var elementKey = element.DmsElementId.Value;
			var script = Convert.ToString(mediationElement.GetParameterByPrimaryKey(1003, elementKey));

			if (String.IsNullOrEmpty(script))
			{
				throw new InvalidOperationException($"No connection handler script found for element '{elementKey}'.");
			}

			return script;
		}

		private void FindConnectionHandlerScripts(IEngine engine, ICollection<CreateConnectionContext> connectionContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var dms = engine.GetDms();

				foreach (var group in connectionContexts.GroupBy(x => x.Destination.Element))
				{
					var elementKey = group.Key;
					var element = dms.GetElement(new DmsElementId(elementKey));

					var script = FindConnectionHandlerScript(engine, element);

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

		private void ExecuteConnectionHandlerScripts(IEngine engine, ICollection<CreateConnectionContext> connectionContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				foreach (var group in connectionContexts.GroupBy(x => x.ConnectionHandlerScript))
				{
					var script = group.Key;

					ExecuteConnectionHandlerScript(engine, script, group, performanceTracker);
				}
			}
		}

		private void ExecuteConnectionHandlerScript(IEngine engine, string scriptName, IEnumerable<CreateConnectionContext> connectionContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var createConnectionsRequest = new CreateConnectionsRequest
				{
					Connections = connectionContexts.Select(x => ConnectionInfo.Create(x.Source, x.Destination)).ToArray(),
				};

				performanceTracker.AddMetadata("Script", scriptName);

				var subScript = engine.PrepareSubScript(scriptName);
				subScript.Synchronous = true;
				subScript.ExtendedErrorInfo = true;

				subScript.SelectScriptParam("Action", "Connect");
				subScript.SelectScriptParam("Input Data", JsonConvert.SerializeObject(createConnectionsRequest));

				subScript.StartScript();

				if (subScript.HadError)
				{
					throw new InvalidOperationException(String.Join(@"\r\n", subScript.GetErrorMessages()));
				}
			}
		}

		private void NotifyPendingConnections(ICollection<CreateConnectionContext> connectionContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var updatedConnections = new List<ConnectionInstance>();

				foreach (var connectionToCreate in connectionContexts)
				{
					var connection = connectionToCreate.DomConnection;
					var sourceId = connectionToCreate.Source?.ID;

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

		private void ClearPendingSourceOnFailedConnections(ICollection<CreateConnectionContext> failedConnections, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				// update connections
				var updatedConnections = new List<ConnectionInstance>();

				foreach (var connectionToCreate in failedConnections)
				{
					var connection = connectionToCreate.DomConnection;
					var sourceId = connectionToCreate.Source?.ID;

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