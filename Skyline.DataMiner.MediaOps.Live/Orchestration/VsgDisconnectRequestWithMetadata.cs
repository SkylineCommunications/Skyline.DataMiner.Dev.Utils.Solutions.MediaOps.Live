namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;

	internal class VsgDisconnectRequestWithMetadata : VsgDisconnectRequest
	{
		public VsgDisconnectRequestWithMetadata(VirtualSignalGroup destination, ICollection<ApiObjectReference<Level>> levels = null)
			: base(destination, levels)
		{
		}

		public object MetaData { get; set; }
	}
}
