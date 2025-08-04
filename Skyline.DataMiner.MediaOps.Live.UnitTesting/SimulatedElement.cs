namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages;

	using DmsElementId = Skyline.DataMiner.Core.DataMinerSystem.Common.DmsElementId;

	public sealed class SimulatedElement
	{
		private readonly ConcurrentDictionary<int, StandaloneParameter> _parameters = new();
		private readonly ConcurrentDictionary<int, TableParameter> _tables = new();

		public SimulatedElement(SimulatedDma dma, int elementId, string name, string protocolName, string protocolVersion)
		{
			Dma = dma ?? throw new ArgumentNullException(nameof(dma));
			ElementId = elementId;
			Name = name;
			ProtocolName = protocolName;
			ProtocolVersion = protocolVersion;
		}

		public SimulatedDma Dma { get; }

		public int ElementId { get; }

		public int DmaId => Dma.DmaId;

		public int HostingDmaId => DmaId;

		public DmsElementId Id => new DmsElementId(DmaId, ElementId);

		public string Name { get; }

		public string ProtocolName { get; }

		public string ProtocolVersion { get; }

		public ElementState State { get; private set; } = ElementState.Active;

		public IReadOnlyDictionary<int, StandaloneParameter> Parameters => _parameters;

		public IReadOnlyDictionary<int, TableParameter> Tables => _tables;

		public void Start()
		{
			if (State != ElementState.Active)
			{
				State = ElementState.Active;

				// send events
				var e1 = new ElementStateEventMessage(DmaId, ElementId, ElementState.Active, AlarmLevel.Normal);
				Dma.NotifySubscriptions(e1);

				var e2 = new ElementStateEventMessage(DmaId, ElementId, ElementState.Active, AlarmLevel.Normal)
				{
					IsElementStartupComplete = true,
				};
				Dma.NotifySubscriptions(e2);
			}
		}

		public void Stop()
		{
			if (State != ElementState.Stopped)
			{
				State = ElementState.Stopped;

				// send event
				var e = new ElementStateEventMessage(DmaId, ElementId, ElementState.Stopped, AlarmLevel.Normal);
				Dma.NotifySubscriptions(e);
			}
		}

		public StandaloneParameter CreateStandaloneParameter(int id)
		{
			var param = new StandaloneParameter(this, id);

			if (!_parameters.TryAdd(id, param))
			{
				throw new InvalidOperationException($"Parameter with ID {id} already exists in element {Name}.");
			}

			return param;
		}

		public TableParameter CreateTable(int id)
		{
			var table = new TableParameter(this, id);

			if (!_tables.TryAdd(id, table))
			{
				throw new InvalidOperationException($"Table with ID {id} already exists in element {Name}.");
			}

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
