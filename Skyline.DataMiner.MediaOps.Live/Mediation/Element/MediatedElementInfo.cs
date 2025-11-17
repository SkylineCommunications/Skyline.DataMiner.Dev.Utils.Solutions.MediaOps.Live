namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	public class MediatedElementInfo
	{
		public MediatedElementInfo(DmsElementId id, string name)
		{
			Id = id;
			Name = name;
		}

		public MediatedElementInfo(int dmaId, int elementId, string name)
			: this(new DmsElementId(dmaId, elementId), name)
		{
		}

		public DmsElementId Id { get; }

		public string Name { get; }

		public string ConnectionHandlerScript { get; internal set; }

		public bool IsEnabled { get; internal set; }
	}
}
