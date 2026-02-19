namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Mediation.ConnectionHandlers
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Logging;

	using LogType = Skyline.DataMiner.Solutions.MediaOps.Live.Logging.LogType;

	public interface IConnectionHandlerEngine
	{
		IMediaOpsLiveApi Api { get; }

		ILogger Logger { get; }

		void Log(string message, LogType logLevel = LogType.Information);

		void RegisterConnection(ConnectionUpdate connection);

		void RegisterConnections(ICollection<ConnectionUpdate> connections);

		Endpoint GetEndpointById(ApiObjectReference<Endpoint> id);

		Endpoint GetEndpointByName(string name);

		VirtualSignalGroup GetVirtualSignalGroupById(ApiObjectReference<VirtualSignalGroup> id);

		VirtualSignalGroup GetVirtualSignalGroupByName(string name);

		IEnumerable<Endpoint> GetAllEndpoints();

		IEnumerable<Endpoint> GetAllEndpoints(EndpointRole role);

		IEnumerable<VirtualSignalGroup> GetAllVirtualSignalGroups();

		IEnumerable<VirtualSignalGroup> GetAllVirtualSignalGroups(EndpointRole role);

		IEnumerable<Endpoint> GetEndpointsWithElement(EndpointRole role, DmsElementId elementId);

		IEnumerable<Endpoint> GetEndpointsWithElement(EndpointRole role, DmsElementId elementId, string identifier);

		IEnumerable<Endpoint> GetEndpointsWithIdentifier(EndpointRole role, string identifier);

		IEnumerable<Endpoint> GetEndpointsWithTransportMetadata(EndpointRole role, params (string fieldName, string value)[] metadataFilters);

		IEnumerable<Endpoint> GetEndpointsWithTransportMetadata(EndpointRole role, string fieldName, string value);

		IEnumerable<Endpoint> GetEndpointsWithTransportType(EndpointRole role, ApiObjectReference<TransportType> transportType);

		TransportType GetTransportTypeById(ApiObjectReference<TransportType> id);

		TransportType GetTransportTypeByName(string name);

		IEnumerable<TransportType> GetAllTransportTypes();

		Level GetLevelById(ApiObjectReference<Level> id);

		Level GetLevelByName(string name);

		IEnumerable<Level> GetAllLevels();
	}
}
