namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Logging;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.InterApp.Messages;

	using Automation = Skyline.DataMiner.Automation;
	using LogType = Skyline.DataMiner.Solutions.MediaOps.Live.Logging.LogType;

	internal class ConnectionHandlerEngine : IConnectionHandlerEngine
	{
		private readonly MediaOpsLiveCache _cache;

		internal ConnectionHandlerEngine(Automation.IEngine engine, ILogger logger)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));

			Api = engine.GetMediaOpsLiveApi();
			Api.SetLogger(logger);

			_cache = engine.GetMediaOpsLiveCache();
		}

		public Automation.IEngine Engine { get; }

		public ILogger Logger { get; }

		public IMediaOpsLiveApi Api { get; }

		public void Log(string message, LogType logLevel = LogType.Information)
		{
			Logger?.Log(message, logLevel);
		}

		public void RegisterConnection(ConnectionUpdate connection)
		{
			if (connection == null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			RegisterConnections([connection]);
		}

		public void RegisterConnections(ICollection<ConnectionUpdate> connections)
		{
			if (connections == null)
			{
				throw new ArgumentNullException(nameof(connections));
			}

			Log($"Registering {connections.Count} connection updates.");

			NotifyConnectionChanges(connections);
		}

		private void NotifyConnectionChanges(ICollection<ConnectionUpdate> connections)
		{
			var now = DateTimeOffset.UtcNow;

			var mediationElementMap = Api.MediationElements.GetElementsForEndpoints(
				connections.Select(x => x.DestinationEndpoint));

			foreach (var group in connections
				.Where(x => mediationElementMap.ContainsKey(x.DestinationEndpoint))
				.GroupBy(x => mediationElementMap[x.DestinationEndpoint]))
			{
				var mediationElement = group.Key;

				// Build list with active connections to register
				var connectionChanges = new List<ConnectionChange>();

				foreach (var connection in group)
				{
					var connectionChange = new ConnectionChange
					{
						Time = now,
						Destination = new EndpointInfo(connection.DestinationEndpoint),
						IsConnected = connection.IsConnected,
					};

					if (connection.SourceEndpoint != null)
					{
						connectionChange.ConnectedSource = new EndpointInfo(connection.SourceEndpoint);
					}

					connectionChanges.Add(connectionChange);
				}

				// Log
				Log($"Notifying {connectionChanges.Count} connection changes to mediation element '{mediationElement.Name}' [{mediationElement.DmsElementId}]:\n" +
					JsonConvert.SerializeObject(connectionChanges, Formatting.Indented));

				// Send message
				var commands = InterAppCallFactory.CreateNew();

				var message = new NotifyConnectionChangesMessage
				{
					ConnectionHandlerScript = Engine.GetScriptName(),
					Changes = connectionChanges,
				};
				commands.Messages.Add(message);

				commands.Send(
					Engine.GetUserConnection(),
					mediationElement.DmaId,
					mediationElement.ElementId,
					9000000,
					[typeof(NotifyPendingConnectionActionMessage)]);
			}
		}

		public Endpoint GetEndpointById(ApiObjectReference<Endpoint> id)
		{
			return _cache.VirtualSignalGroupEndpointsCache.Endpoints.GetEndpoint(id);
		}

		public Endpoint GetEndpointByName(string name)
		{
			return _cache.VirtualSignalGroupEndpointsCache.Endpoints.GetEndpoint(name);
		}

		public VirtualSignalGroup GetVirtualSignalGroupById(ApiObjectReference<VirtualSignalGroup> id)
		{
			return _cache.VirtualSignalGroupEndpointsCache.VirtualSignalGroups.GetVirtualSignalGroup(id);
		}

		public VirtualSignalGroup GetVirtualSignalGroupByName(string name)
		{
			return _cache.VirtualSignalGroupEndpointsCache.VirtualSignalGroups.GetVirtualSignalGroup(name);
		}

		public IEnumerable<Endpoint> GetAllEndpoints()
		{
			return _cache.VirtualSignalGroupEndpointsCache.Endpoints.GetAllEndpoints();
		}

		public IEnumerable<Endpoint> GetAllEndpoints(EndpointRole role)
		{
			return _cache.VirtualSignalGroupEndpointsCache.Endpoints.GetEndpointsWithRole(role);
		}

		public IEnumerable<VirtualSignalGroup> GetAllVirtualSignalGroups()
		{
			return _cache.VirtualSignalGroupEndpointsCache.VirtualSignalGroups.GetAllVirtualSignalGroups();
		}

		public IEnumerable<VirtualSignalGroup> GetAllVirtualSignalGroups(EndpointRole role)
		{
			return _cache.VirtualSignalGroupEndpointsCache.VirtualSignalGroups.GetVirtualSignalGroupsWithRole(role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithElement(EndpointRole role, DmsElementId elementId)
		{
			return _cache.VirtualSignalGroupEndpointsCache.GetEndpointsWithElement(elementId).Where(e => e.Role == role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithIdentifier(EndpointRole role, string identifier)
		{
			return _cache.VirtualSignalGroupEndpointsCache.GetEndpointsWithIdentifier(identifier).Where(e => e.Role == role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithElement(EndpointRole role, DmsElementId elementId, string identifier)
		{
			return _cache.VirtualSignalGroupEndpointsCache.GetEndpointsWithElementAndIdentifier(elementId, identifier).Where(e => e.Role == role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithTransportType(EndpointRole role, ApiObjectReference<TransportType> transportType)
		{
			return _cache.VirtualSignalGroupEndpointsCache.GetEndpointsWithTransportType(transportType)
				.Where(e => e.Role == role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithTransportMetadata(EndpointRole role, string fieldName, string value)
		{
			return _cache.VirtualSignalGroupEndpointsCache.GetEndpointsWithTransportMetadata(fieldName, value)
				.Where(e => e.Role == role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithTransportMetadata(EndpointRole role, params (string fieldName, string value)[] metadataFilters)
		{
			return _cache.VirtualSignalGroupEndpointsCache.GetEndpointsWithTransportMetadata(metadataFilters)
				.Where(e => e.Role == role);
		}

		public TransportType GetTransportTypeById(ApiObjectReference<TransportType> id)
		{
			return _cache.TransportTypesCache.GetTransportType(id);
		}

		public TransportType GetTransportTypeByName(string name)
		{
			return _cache.TransportTypesCache.GetTransportType(name);
		}

		public IEnumerable<TransportType> GetAllTransportTypes()
		{
			return _cache.TransportTypesCache.GetAllTransportTypes();
		}

		public Level GetLevelById(ApiObjectReference<Level> id)
		{
			return _cache.LevelsCache.GetLevel(id);
		}

		public Level GetLevelByName(string name)
		{
			return _cache.LevelsCache.GetLevel(name);
		}

		public IEnumerable<Level> GetAllLevels()
		{
			return _cache.LevelsCache.GetAllLevels();
		}
	}
}
