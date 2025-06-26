namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	public sealed class SimulatedDms
	{
		private readonly ConcurrentDictionary<int, SimulatedDma> _agents = new();
		private readonly ConcurrentBag<SLNetConnectionMock> _connections = new();
		private readonly DomSLNetMessageHandler _domSLNetMessageHandler = new();

		public SimulatedDms()
		{
			_domSLNetMessageHandler.OnInstancesChanged += DomSLNetMessageHandler_OnInstancesChanged;
		}

		public IReadOnlyDictionary<int, SimulatedDma> Agents => _agents;

		public SimulatedDma GetOrCreateAgent(int dmaId)
		{
			return _agents.GetOrAdd(
				dmaId,
				id => new SimulatedDma(this, id));
		}

		public IConnection CreateConnection()
		{
			var connection = new SLNetConnectionMock(this);
			_connections.Add(connection);

			return connection;
		}

		internal void NotifyTableUpdate(ParameterTableUpdateEventMessage e)
		{
			foreach (var connection in _connections)
			{
				connection.NotifyTableUpdate(e);
			}
		}

		internal bool TryHandleMessage(DMSMessage message, out IEnumerable<DMSMessage> responses)
		{
			if (_domSLNetMessageHandler.TryHandleMessage(message, out var response))
			{
				responses = [response];
				return true;
			}

			switch (message)
			{
				case GetLiteElementInfo msg:
					responses = HandleMessage(msg);
					return true;

				case GetElementByIDMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetPartialTableMessage msg:
					responses = HandleMessage(msg);
					return true;

				default:
					responses = [];
					return false;
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetLiteElementInfo msg)
		{
			var elements = Agents.Values.SelectMany(x => x.Elements.Values);

			if (!String.IsNullOrEmpty(msg.ProtocolName))
			{
				elements = elements.Where(x => String.Equals(x.ProtocolName, msg.ProtocolName));
			}

			foreach (var element in elements)
			{
				yield return element.ToLiteElementInfo();
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetElementByIDMessage msg)
		{
			if (Agents.TryGetValue(msg.DataMinerID, out var dma) &&
				dma.Elements.TryGetValue(msg.ElementID, out var element))
			{
				yield return element.ToElementInfo();
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetPartialTableMessage msg)
		{
			if (Agents.TryGetValue(msg.DataMinerID, out var dma) &&
				dma.Elements.TryGetValue(msg.ElementID, out var element) &&
				element.Tables.TryGetValue(msg.ParameterID, out var table))
			{
				yield return new ParameterChangeEventMessage(msg.DataMinerID, msg.ElementID, msg.ParameterID)
				{
					NewValue = table.ToParameterValue(),
				};
			}
		}

		private void DomSLNetMessageHandler_OnInstancesChanged(object sender, DomInstancesChangedEventMessage e)
		{
			foreach (var connection in _connections)
			{
				connection.NotifyDomInstancesChanged(e);
			}
		}
	}
}
