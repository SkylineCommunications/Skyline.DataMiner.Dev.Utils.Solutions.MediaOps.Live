namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Take;

	internal class VsgConnectionRequestWithMetaData : VsgConnectionRequest
	{
		public VsgConnectionRequestWithMetaData(VirtualSignalGroup source, VirtualSignalGroup destination, ICollection<LevelMapping> levelMappings = null)
			: base(source, destination, levelMappings)
		{
		}

		public object MetaData { get; set; }
	}
}
