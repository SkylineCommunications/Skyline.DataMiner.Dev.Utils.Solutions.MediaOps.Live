namespace CheckMediaOpsLive
{
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class VirtualSignalGroupsContext
	{
		public VirtualSignalGroupsContext(MediaOpsLiveApi api)
		{
			Levels = api.Levels.ReadAll().ToDictionary(x => x.Reference);
			TransportTypes = api.TransportTypes.ReadAll().ToDictionary(x => x.Reference);
			Endpoints = api.Endpoints.ReadAll().ToDictionary(x => x.Reference);
			VirtualSignalGroups = api.VirtualSignalGroups.ReadAll().ToDictionary(x => x.Reference);

			Mapping = new VirtualSignalGroupEndpointsMapping(VirtualSignalGroups.Values);
		}

		public IDictionary<ApiObjectReference<Level>, Level> Levels { get; }

		public IDictionary<ApiObjectReference<TransportType>, TransportType> TransportTypes { get; }

		public IDictionary<ApiObjectReference<Endpoint>, Endpoint> Endpoints { get; }

		public IDictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> VirtualSignalGroups { get; }

		public VirtualSignalGroupEndpointsMapping Mapping { get; }
	}
}
