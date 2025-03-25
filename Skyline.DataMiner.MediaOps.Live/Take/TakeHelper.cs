namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
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

			using (new PerformanceTracker(performanceCollector))
			{
				var connectionContexts = connectionRequests
					.Select(x => new CreateConnectionContext(x))
					.ToList();

				using (var connectionWatcher = new ConnectionWatcher())
				{
					try
					{
						Task.WaitAll(
							Task.Run(
								() =>
								{
									GetOrCreateDomConnections(connectionContexts, performanceCollector);
									NotifyPendingConnections(connectionContexts, performanceCollector);
								}),
							Task.Run(
								() =>
								{
									ExecuteConnectionHandlerScripts(engine, connectionContexts, performanceCollector);
									WaitUntilAllConnected(engine, connectionWatcher, connectionContexts, performanceCollector);
								}));
					}
					finally
					{
						ClearPendingSourceOnFailedConnections(connectionContexts, performanceCollector);
					}
				}
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

			// load all endpoints
			var endpoints = LoadEndpoints(vsgConnectionRequests, performanceCollector);

			// create connection requests between endpoints
			var connectionRequests = new List<ConnectionRequest>();

			foreach (var vsgConnectionRequest in vsgConnectionRequests)
			{
				var source = vsgConnectionRequest.Source;
				var destination = vsgConnectionRequest.Destination;

				var join = source.Levels.Join(
					destination.Levels,
					left => left.Level,
					right => right.Level,
					(left, right) => new { Level = left.Level, SourceLevel = left, DistinationLevel = right });

				foreach (var item in join)
				{
					var filterLevels = vsgConnectionRequest.Levels != null;

					if (filterLevels && !vsgConnectionRequest.Levels.Any(l => l.ID == item.Level))
					{
						continue;
					}

					endpoints.TryGetValue((Guid)item.SourceLevel.Endpoint, out var sourceEndpoint);
					endpoints.TryGetValue((Guid)item.DistinationLevel.Endpoint, out var destinationEndpoint);

					var request = new ConnectionRequest(sourceEndpoint, destinationEndpoint);
					connectionRequests.Add(request);
				}
			}

			Take(engine, connectionRequests, performanceCollector);
		}

		private IDictionary<Guid, Endpoint> LoadEndpoints(ICollection<VsgConnectionRequest> vsgConnectionRequests, PerformanceCollector performanceCollector)
		{
			using (new PerformanceTracker(performanceCollector))
			{
				var endpointIds = vsgConnectionRequests.SelectMany(x => x.Source.GetEndpoints())
						.Concat(vsgConnectionRequests.SelectMany(x => x.Destination.GetEndpoints()))
						.Select(x => x.ID)
						.Distinct();

				return _api.Endpoints.Read(endpointIds);
			}
		}

		private void GetOrCreateDomConnections(ICollection<CreateConnectionContext> connectionContexts, PerformanceCollector performanceCollector)
		{
			using (new PerformanceTracker(performanceCollector))
			{
				var connections = _api.Helper.GetConnectionsForDestinations(connectionContexts.Select(x => x.Destination.ID));

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

		private void WaitUntilAllConnected(IEngine engine, ConnectionWatcher connectionWatcher, ICollection<CreateConnectionContext> connectionContexts, PerformanceCollector performanceCollector)
		{
			using (new PerformanceTracker(performanceCollector))
			{
				Parallel.ForEach(connectionContexts, ctc =>
				{
					ctc.HasSucceeded = IsConnectionEstablished(engine, connectionWatcher, ctc, performanceCollector);
				});
			}
		}

		private bool IsConnectionEstablished(IEngine engine, ConnectionWatcher connectionWatcher, CreateConnectionContext connectionContexts, PerformanceCollector performanceCollector)
		{
			if (connectionContexts.DomConnection?.ConnectionInfo?.ConnectedSource == connectionContexts.Source?.ID)
			{
				// no need to wait, they were already connected
				return true;
			}

			using (var tracker = new PerformanceTracker(performanceCollector))
			{
				tracker.AddMetadata("Source", connectionContexts.Source?.Name);
				tracker.AddMetadata("Destination", connectionContexts.Destination?.Name);

				return ConnectionAwaiter.Wait(engine, connectionWatcher, connectionContexts.Source, connectionContexts.Destination, TimeSpan.FromSeconds(5));
			}
		}

		private string FindConnectionHandlerScript(IEngine engine, string elementKey)
		{
			var mediationElement = engine.FindElement("MediaOps Mediation PoC");

			var script = Convert.ToString(mediationElement.GetParameterByPrimaryKey(1003, elementKey));

			if (String.IsNullOrEmpty(script))
			{
				throw new InvalidOperationException($"No connection handler script found for element '{elementKey}'.");
			}

			return script;
		}

		private void ExecuteConnectionHandlerScripts(IEngine engine, ICollection<CreateConnectionContext> connectionContexts, PerformanceCollector performanceCollector)
		{
			using (new PerformanceTracker(performanceCollector))
			{
				foreach (var group in connectionContexts.GroupBy(x => x.Destination.Element))
				{
					var elementKey = group.Key;

					ExecuteConnectionHandlerScript(engine, elementKey, group, performanceCollector);
				}
			}
		}

		private void ExecuteConnectionHandlerScript(IEngine engine, string elementKey, IEnumerable<CreateConnectionContext> connectionContexts, PerformanceCollector performanceCollector)
		{
			using (var tracker = new PerformanceTracker(performanceCollector))
			{
				var createConnectionsRequest = new CreateConnectionsRequest
				{
					Connections = connectionContexts.Select(x => ConnectionInfo.Create(x.Source, x.Destination)).ToArray(),
				};

				var script = FindConnectionHandlerScript(engine, elementKey);
				engine.GenerateInformation($"Connection handler script: {script}");

				tracker.AddMetadata("Script", script);

				var subScript = engine.PrepareSubScript(script);
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

		private void NotifyPendingConnections(ICollection<CreateConnectionContext> connectionContexts, PerformanceCollector performanceCollector)
		{
			using (new PerformanceTracker(performanceCollector))
			using (new ConnectionUpdateLock())
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

				if (updatedConnections.Count > 0)
				{
					_api.Helper.DomHelper.DomInstances.CreateOrUpdateInBatches(updatedConnections.Select(x => x.ToInstance())).ThrowOnFailure();
				}
			}
		}

		private void ClearPendingSourceOnFailedConnections(ICollection<CreateConnectionContext> connectionContexts, PerformanceCollector performanceCollector)
		{
			var failedConnections = connectionContexts.Where(x => !x.HasSucceeded).ToList();

			if (failedConnections.Count == 0)
			{
				return;
			}

			using (new PerformanceTracker(performanceCollector))
			using (new ConnectionUpdateLock())
			{
				// refresh DOM connections
				RefreshDomConnections(failedConnections, performanceCollector);

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

				if (updatedConnections.Count > 0)
				{
					_api.Helper.DomHelper.DomInstances.CreateOrUpdateInBatches(updatedConnections.Select(x => x.ToInstance())).ThrowOnFailure();
				}
			}
		}

		private void RefreshDomConnections(ICollection<CreateConnectionContext> connectionContexts, PerformanceCollector performanceCollector)
		{
			using (new PerformanceTracker(performanceCollector))
			{
				var newConnections = _api.Helper.GetConnections(connectionContexts.Select(x => x.DomConnection.ID.Id));

				foreach (var connectionToCreate in connectionContexts)
				{
					if (newConnections.TryGetValue(connectionToCreate.DomConnection.ID.Id, out var newConnection))
					{
						connectionToCreate.DomConnection = newConnection;
					}
				}
			}
		}
	}
}