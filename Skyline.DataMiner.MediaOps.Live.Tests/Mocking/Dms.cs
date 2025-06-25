namespace Skyline.DataMiner.MediaOps.Live.Tests.Mocking
{
	using System;
	using System.Collections.Concurrent;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	internal class Dms
	{
		private readonly ConcurrentDictionary<DmsElementId, Element> _elements = new();
		private readonly ConcurrentBag<SLNetConnectionMock> _connections = new();
		private readonly DomSLNetMessageHandler _domSLNetMessageHandler = new();

		public Dms()
		{
			_domSLNetMessageHandler.OnInstancesChanged += DomSLNetMessageHandler_OnInstancesChanged;
		}

		public IReadOnlyDictionary<DmsElementId, Element> Elements => _elements;

		public void AddElement(Element element)
		{
			if (element is null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			if (!_elements.TryAdd(element.Id, element))
			{
				throw new InvalidOperationException($"Element with ID {element.Id} already exists.");
			}
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
			IEnumerable<Element> elements = _elements.Values;

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
