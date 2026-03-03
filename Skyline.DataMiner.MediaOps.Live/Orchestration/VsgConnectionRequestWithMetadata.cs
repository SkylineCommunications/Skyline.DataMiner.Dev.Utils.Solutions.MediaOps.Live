namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;

	internal class VsgConnectionRequestWithMetadata : VsgConnectionRequest
	{
		public VsgConnectionRequestWithMetadata(VirtualSignalGroup source, VirtualSignalGroup destination, ICollection<LevelMapping> levelMappings = null)
			: base(source, destination, levelMappings)
		{
		}

		public object MetaData { get; set; }
	}
}
