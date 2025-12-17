namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages;

	using DmsElementId = Skyline.DataMiner.Core.DataMinerSystem.Common.DmsElementId;

	public sealed class SimulatedElement
	{
		private readonly ConcurrentDictionary<int, ParameterBase> _parameters = new();

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

		public StandaloneParameter GetStandaloneParameter(int id)
		{
			if (!_parameters.TryGetValue(id, out var param))
			{
				throw new KeyNotFoundException($"Parameter with ID {id} does not exist in element {Name}.");
			}

			if (param is not StandaloneParameter standaloneParameter)
			{
				throw new InvalidOperationException($"Parameter with ID {id} is not a standalone parameter.");
			}

			return standaloneParameter;
		}

		public bool TryGetStandaloneParameter(int id, out StandaloneParameter parameter)
		{
			if (_parameters.TryGetValue(id, out var param) && param is StandaloneParameter standaloneParam)
			{
				parameter = standaloneParam;
				return true;
			}

			parameter = null;
			return false;
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

		public TableParameter GetTableParameter(int id)
		{
			if (!_parameters.TryGetValue(id, out var param))
			{
				throw new KeyNotFoundException($"Parameter with ID {id} does not exist in element {Name}.");
			}

			if (param is not TableParameter tableParameter)
			{
				throw new InvalidOperationException($"Parameter with ID {id} is not a table parameter.");
			}

			return tableParameter;
		}

		public bool TryGetTableParameter(int id, out TableParameter parameter)
		{
			if (_parameters.TryGetValue(id, out var param) && param is TableParameter tableParam)
			{
				parameter = tableParam;
				return true;
			}

			parameter = null;
			return false;
		}

		public TableParameter CreateTable(int id)
		{
			var table = new TableParameter(this, id);

			if (!_parameters.TryAdd(id, table))
			{
				throw new InvalidOperationException($"Parameter with ID {id} already exists in element {Name}.");
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

		internal bool TryGetSpecialParameterValue(int parameterId, out ParameterValue specialValue)
		{
			switch (parameterId)
			{
				case 65003: // Number of active alarms
					specialValue = new ParameterValue(0);
					return true;
				case 65004: // Number of critical alarms
					specialValue = new ParameterValue(0);
					return true;
				case 65005: // Number of major alarms
					specialValue = new ParameterValue(0);
					return true;
				case 65006: // Number of minor alarms
					specialValue = new ParameterValue(0);
					return true;
				case 65007: // Number of warning alarms
					specialValue = new ParameterValue(0);
					return true;
				default:
					specialValue = null;
					return false;
			}
		}
	}
}
