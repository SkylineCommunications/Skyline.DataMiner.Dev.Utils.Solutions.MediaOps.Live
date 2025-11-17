namespace Skyline.DataMiner.MediaOps.Live.Automation.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Logging;
	using Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages;

	using Automation = Skyline.DataMiner.Automation;
	using LogType = Skyline.DataMiner.MediaOps.Live.Logging.LogType;

	internal class ConnectionHandlerEngine : IConnectionHandlerEngine
	{
		private readonly StaticMediaOpsLiveCache _cache;

		internal ConnectionHandlerEngine(Automation.IEngine engine, ILogger logger)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));

			Api = new MediaOpsLiveApi(Automation.Engine.SLNetRaw);
			Api.SetLogger(logger);

			_cache = StaticMediaOpsLiveCache.GetOrCreate(Automation.Engine.SLNetRaw);
		}

		public Automation.IEngine Engine { get; }

		public ILogger Logger { get; }

		public MediaOpsLiveApi Api { get; }

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
			var now = DateTimeOffset.Now;

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

				var message = new NotifyConnectionChangesMessage { Changes = connectionChanges };
				commands.Messages.Add(message);

				commands.Send(
					Engine.GetUserConnection(),
					mediationElement.DmaId,
					mediationElement.ElementId,
					9000000,
					[typeof(NotifyPendingConnectionActionMessage)]);
			}
		}

		public IEnumerable<Endpoint> GetAllEndpoints()
		{
			return _cache.VirtualSignalGroupsCache.Endpoints.Values;
		}

		public IEnumerable<Endpoint> GetAllEndpoints(EndpointRole role)
		{
			return GetAllEndpoints().Where(e => e.Role == role);
		}

		public IEnumerable<VirtualSignalGroup> GetAllVirtualSignalGroups()
		{
			return _cache.VirtualSignalGroupsCache.VirtualSignalGroups.Values;
		}

		public IEnumerable<VirtualSignalGroup> GetAllVirtualSignalGroups(EndpointRole role)
		{
			return GetAllVirtualSignalGroups().Where(e => e.Role == role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithElement(EndpointRole role, DmsElementId elementId)
		{
			return _cache.VirtualSignalGroupsCache.GetEndpointsWithElement(elementId).Where(e => e.Role == role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithIdentifier(EndpointRole role, string identifier)
		{
			return _cache.VirtualSignalGroupsCache.GetEndpointsWithIdentifier(identifier).Where(e => e.Role == role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithElement(EndpointRole role, DmsElementId elementId, string identifier)
		{
			return _cache.VirtualSignalGroupsCache.GetEndpointsWithElementAndIdentifier(elementId, identifier).Where(e => e.Role == role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithTransportType(EndpointRole role, ApiObjectReference<TransportType> transportType)
		{
			return _cache.VirtualSignalGroupsCache.GetEndpointsWithTransportType(transportType)
				.Where(e => e.Role == role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithTransportMetadata(EndpointRole role, string fieldName, string value)
		{
			return _cache.VirtualSignalGroupsCache.GetEndpointsWithTransportMetadata(fieldName, value)
				.Where(e => e.Role == role);
		}

		public IEnumerable<Endpoint> GetEndpointsWithTransportMetadata(EndpointRole role, params (string fieldName, string value)[] metadataFilters)
		{
			return _cache.VirtualSignalGroupsCache.GetEndpointsWithTransportMetadata(metadataFilters)
				.Where(e => e.Role == role);
		}
	}
}
