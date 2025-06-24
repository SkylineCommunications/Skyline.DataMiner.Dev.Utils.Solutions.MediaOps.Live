namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation;
	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;
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

				PrepareData(engine, takeContexts, performanceTracker);

				NotifyPendingConnectionActions(engine, ScriptAction.Connect, takeContexts, performanceTracker);
				ExecuteConnectionHandlerScripts(engine, ScriptAction.Connect, takeContexts, performanceTracker);
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

				PrepareData(engine, takeContexts, performanceTracker);

				NotifyPendingConnectionActions(engine, ScriptAction.Disconnect, takeContexts, performanceTracker);
				ExecuteConnectionHandlerScripts(engine, ScriptAction.Disconnect, takeContexts, performanceTracker);
			}
		}

		private void PrepareData(IEngine engine, ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var dms = engine.GetDms();

				GetDestinationElements(dms, takeContexts, performanceTracker);
				GetMediationElements(dms, takeContexts, performanceTracker);
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

		private void GetMediationElements(IDms dms, ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var allMediationElements = MediationElement.GetAllMediationElements(dms).ToList();

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

		private void NotifyPendingConnectionActions(IEngine engine, ScriptAction action, ICollection<ConnectionOperationContext> takeContexts, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var now = DateTimeOffset.Now;

				foreach (var group in takeContexts.GroupBy(x => x.MediationElement))
				{
					var mediationElement = group.Key;

					var requests = new List<InterApp.Messages.PendingConnectionAction>();

					foreach (var connection in group)
					{
						var request = new InterApp.Messages.PendingConnectionAction
						{
							Time = now,
							Destination = new InterApp.Messages.EndpointInfo
							{
								ID = connection.Destination.ID,
								Name = connection.Destination.Name,
							},
						};

						switch (action)
						{
							case ScriptAction.Connect:
								request.Action = InterApp.Messages.ConnectionAction.Connect;
								request.PendingSource = new InterApp.Messages.EndpointInfo
								{
									ID = connection.Source.ID,
									Name = connection.Source.Name,
								};
								break;
							case ScriptAction.Disconnect:
								request.Action = InterApp.Messages.ConnectionAction.Disconnect;
								break;
							default:
								throw new InvalidOperationException($"Invalid action: {action}");
						}

						requests.Add(request);
					}

					var commands = InterAppCallFactory.CreateNew();

					var message = new InterApp.Messages.NotifyPendingConnectionActionMessage { Actions = requests };
					commands.Messages.Add(message);

					commands.Send(
						engine.GetUserConnection(),
						mediationElement.DmaId,
						mediationElement.ElementId,
						9000000,
						[typeof(InterApp.Messages.NotifyPendingConnectionActionMessage)]);
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
	}
}