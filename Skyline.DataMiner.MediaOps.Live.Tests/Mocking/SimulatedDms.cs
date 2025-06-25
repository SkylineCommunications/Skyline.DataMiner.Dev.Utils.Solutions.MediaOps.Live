namespace Skyline.DataMiner.MediaOps.Live.Tests.Mocking
{
	using System;
	using System.Collections.Concurrent;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	public class SimulatedDms
	{
		private readonly ConcurrentDictionary<DmsElementId, SimulatedElement> _elements = new();
		private readonly ConcurrentBag<SLNetConnectionMock> _connections = new();
		private readonly DomSLNetMessageHandler _domSLNetMessageHandler = new();

		public SimulatedDms()
		{
			_domSLNetMessageHandler.OnInstancesChanged += DomSLNetMessageHandler_OnInstancesChanged;
		}

		public IReadOnlyDictionary<DmsElementId, SimulatedElement> Elements => _elements;

		public SimulatedElement CreateElement(int dmaId, int elementId, string name, string protocolName, string protocolVersion = "1.0.0.1")
		{
			var element = new SimulatedElement(this, dmaId, elementId, name, protocolName, protocolVersion);

			if (!_elements.TryAdd(element.Id, element))
			{
				throw new InvalidOperationException($"Element with ID {element.Id} already exists.");
			}

			return element;
		}

		public IConnection CreateConnection()
		{
			var connection = new SLNetConnectionMock(this);
			_connections.Add(connection);

			return connection;
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
			IEnumerable<SimulatedElement> elements = _elements.Values;

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
			var id = new DmsElementId(msg.DataMinerID, msg.ElementID);

			if (_elements.TryGetValue(id, out var element))
			{
				yield return element.ToElementInfo();
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetPartialTableMessage msg)
		{
			var id = new DmsElementId(msg.DataMinerID, msg.ElementID);

			if (_elements.TryGetValue(id, out var element) &&
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
