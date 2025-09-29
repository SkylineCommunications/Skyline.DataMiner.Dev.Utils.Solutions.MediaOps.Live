namespace CheckMediaOpsLive
{
	using System.Collections.Generic;

	public class MediaOpsLiveStatistics
	{
		public int NumberOfLevels { get; set; }

		public int NumberOfTransportTypes { get; set; }

		public int NumberOfSourceEndpoints { get; set; }

		public int NumberOfDestinationEndpoints { get; set; }

		public int NumberOfSourceVirtualSignalGroups { get; set; }

		public int NumberOfDestinationVirtualSignalGroups { get; set; }

		public ICollection<ConnectionHandlerScriptStatistics> ConnectionHandlerScripts { get; set; }

	}
}
