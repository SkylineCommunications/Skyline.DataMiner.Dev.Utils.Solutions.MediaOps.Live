namespace CheckMediaOpsLive
{
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;

	public static class Statistics
	{
		public static ICollection<Metric> CollectStatistics(MediaOpsLiveApi api)
		{
			var metrics = new List<Metric>
			{
				new Metric("NumberOfLevels", api.Levels.Query().Count()),
				new Metric("NumberOfTransportTypes", api.TransportTypes.Query().Count()),
				new Metric("NumberOfSourceEndpoints", api.Endpoints.Query().Count(x => x.Role == Role.Source)),
				new Metric("NumberOfDestinationEndpoints", api.Endpoints.Query().Count(x => x.Role == Role.Destination)),
				new Metric("NumberOfSourceVirtualSignalGroups", api.VirtualSignalGroups.Query().Count(x => x.Role == Role.Source)),
				new Metric("NumberOfDestinationVirtualSignalGroups", api.VirtualSignalGroups.Query().Count(x => x.Role == Role.Destination)),
			};

			return metrics;
		}
	}
}
