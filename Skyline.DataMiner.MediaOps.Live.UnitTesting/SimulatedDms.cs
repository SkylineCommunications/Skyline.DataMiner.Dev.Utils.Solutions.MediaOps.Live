namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.Advanced;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	public sealed class SimulatedDms
	{
		private readonly ConcurrentDictionary<int, SimulatedDma> _agents = new();
		private readonly ConcurrentBag<SLNetConnectionMock> _connections = new();
		private readonly DomSLNetMessageHandler _domSLNetMessageHandler = new();

		public SimulatedDms()
		{
			_domSLNetMessageHandler.OnInstancesChanged += (s, e) => NotifySubscriptions(e);
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

		internal void NotifySubscriptions(EventMessage eventMessage)
		{
			if (eventMessage is null)
			{
				throw new ArgumentNullException(nameof(eventMessage));
			}

			foreach (var connection in _connections)
			{
				connection.NotifySubscriptions(eventMessage);
			}
		}

		internal bool TryHandleMessage(DMSMessage message, out IEnumerable<DMSMessage> responses)
		{
			if (message is null)
			{
				throw new ArgumentNullException(nameof(message));
			}

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

				case GetElementByNameMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetPartialTableMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetParameterMessage msg:
					responses = HandleMessage(msg);
					return true;

				case SetDataMinerInfoMessage msg:
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

		private IEnumerable<DMSMessage> HandleMessage(GetElementByNameMessage msg)
		{
			var elements = Agents.Values.SelectMany(x => x.Elements.Values);
			var element = elements.FirstOrDefault(x => String.Equals(x.Name, msg.ElementName));

			if (element != null)
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
			else
			{
				throw new InvalidOperationException($"Element with ID {msg.ElementID} not found in DMA {msg.DataMinerID} or table with ID {msg.ParameterID} not found.");
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetParameterMessage msg)
		{
			if (Agents.TryGetValue(msg.DataMinerID, out var dma) &&
				dma.Elements.TryGetValue(msg.ElId, out var element) &&
				element.Parameters.TryGetValue(msg.ParameterId, out var param))
			{
				yield return new GetParameterResponseMessage
				{
					DataMinerID = msg.DataMinerID,
					ElId = msg.ElId,
					ParameterId = msg.ParameterId,
					Value = param.ToParameterValue(),
				};
			}
			else
			{
				throw new InvalidOperationException($"Element with ID {msg.ElId} not found in DMA {msg.DataMinerID} or parameter with ID {msg.ParameterId} not found.");
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(SetDataMinerInfoMessage msg)
		{
			switch ((NotifyType)msg.What)
			{
				case NotifyType.GetKeyPosition:
					{
						var ids = (int[])msg.Var1;
						var key = (string)msg.Var2;

						if (Agents.TryGetValue(ids[0], out var dma) &&
							dma.Elements.TryGetValue(ids[1], out var element) &&
							element.Tables.TryGetValue(ids[2], out var table))
						{
							var index = table.Rows.Keys.ToList().IndexOf(key);

							yield return new SetDataMinerInfoResponseMessage
							{
								RawData = index + 1,
							};
						}
						else
						{
							throw new InvalidOperationException($"Element with ID {ids[1]} not found in DMA {ids[0]} or table with ID {ids[2]} not found.");
						}
					}

					break;

				case NotifyType.NT_GET_ROW:
					{
						var var1 = (object[])msg.Var1;

						if (Agents.TryGetValue((int)var1[0], out var dma) &&
							dma.Elements.TryGetValue((int)var1[1], out var element) &&
							element.Tables.TryGetValue((int)var1[2], out var table))
						{
							table.Rows.TryGetValue((string)var1[3], out var row);

							yield return new SetDataMinerInfoResponseMessage
							{
								RawData = row,
							};
						}
						else
						{
							throw new InvalidOperationException($"Element with ID {var1[1]} not found in DMA {var1[0]} or table with ID {var1[2]} not found.");
						}
					}

					break;

				default:
					throw new NotSupportedException($"NotifyType '{msg.What}' is not supported.");
			}
		}
	}
}
