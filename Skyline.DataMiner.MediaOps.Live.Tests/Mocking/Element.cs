namespace Skyline.DataMiner.MediaOps.Live.Tests.Mocking
{
	using System.Collections.Concurrent;

	using Skyline.DataMiner.Net.Messages;

	using DmsElementId = Skyline.DataMiner.Core.DataMinerSystem.Common.DmsElementId;

	internal class Element
	{
		private readonly ConcurrentDictionary<int, TableParameter> _tables = new();

		public Element(int dmaId, int elementId, string name)
		{
			Id = new DmsElementId(dmaId, elementId);
			Name = name;
		}

		public DmsElementId Id { get; }

		public int DmaId => Id.AgentId;

		public int HostingDmaId => Id.AgentId;

		public int ElementId => Id.ElementId;

		public string Name { get; }

		public string ProtocolName { get; set; }

		public string ProtocolVersion { get; set; }

		public ElementState State { get; set; } = ElementState.Active;

		public IReadOnlyDictionary<int, TableParameter> Tables => _tables;

		public TableParameter AddTable(int id)
		{
			var table = new TableParameter(id);
			_tables.TryAdd(id, table);

			return table;
		}

		internal LiteElementInfoEvent ToLiteElementInfo()
		{
			return new LiteElementInfoEvent
			{
				DataMinerID = DmaId,
				HostingAgentID = HostingDmaId,
				ElementID = ElementId,
				Name = Name,
				Protocol = ProtocolName,
				ProtocolVersion = ProtocolVersion,
				State = State,
			};
		}

		internal ElementInfoEventMessage ToElementInfo()
		{
			return new ElementInfoEventMessage
			{
				DataMinerID = DmaId,
				HostingAgentID = HostingDmaId,
				ElementID = ElementId,
				Name = Name,
				Protocol = ProtocolName,
				ProtocolVersion = ProtocolVersion,
				State = State,
			};
		}
	}
}
