namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections;
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

		public IEnumerable<SimulatedSchedulerTask> GetAllDmsSchedulerTasks()
		{
			List<SimulatedSchedulerTask> tasks = new List<SimulatedSchedulerTask>();

			foreach (KeyValuePair<int, SimulatedDma> simulatedDma in _agents)
			{
				tasks.AddRange(simulatedDma.Value.Scheduler.Tasks.Values.ToList());
			}

			return tasks;
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

				case SetSchedulerInfoMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetInfoMessage msg:
					responses = HandleMessage(msg);
					return true;

				case AsyncMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetDataMinerByIDMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetAgentBuildInfo msg:
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
		}

		private IEnumerable<DMSMessage> HandleMessage(SetSchedulerInfoMessage msg)
		{
			if (Agents.TryGetValue(msg.DataMinerID, out var dma))
			{
				int returnId;
				if (msg.What == 3) // Delete
				{
					dma.Scheduler.Tasks.Remove(msg.Info);
					returnId = 0;
				}
				else
				{
					var info = msg.Ppsa.Ppsa;
					var generalInfo = info[0].Psa;
					var firstArgument = Convert.ToString(generalInfo[0].Sa[0]);

					returnId = !Int32.TryParse(firstArgument, out int taskId) ? dma.Scheduler.GetFirstAvailableId() : taskId;
					dma.Scheduler.Tasks[returnId] = new SimulatedSchedulerTask(dma.Scheduler, msg);
				}

				yield return new SetSchedulerInfoResponseMessage
				{
					iRet = returnId,
				};
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetInfoMessage msg)
		{
			switch (msg.Type)
			{
				case InfoType.SchedulerTasks:
					return HandleSchedulerTaskInfoMessage();

				case InfoType.DataMinerInfo:
					return HandleDataMinerInfoMessage();

				default:
					throw new NotSupportedException("Not Supported");
			}
		}

		private IEnumerable<DMSMessage> HandleSchedulerTaskInfoMessage()
		{
			List<SimulatedSchedulerTask> allDmsTasks = [];
			foreach (KeyValuePair<int, SimulatedDma> dma in Agents)
			{
				allDmsTasks.AddRange(dma.Value.Scheduler.Tasks.Values);
			}

			yield return new GetSchedulerTasksResponseMessage
			{
				Tasks = new ArrayList(allDmsTasks.Select(task => task.ToSchedulerTaskInfo()).ToList()),
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(AsyncMessage msg)
		{
			yield return new AsyncMessageStartResponse(msg.Cookie);

			/*List<DMSMessage> allResponses = [];
			if (msg.Requests != null && msg.Requests.Any())
			{
				foreach (var item in msg.Requests)
				{
					TryHandleMessage(item, out IEnumerable<DMSMessage> responses);
					allResponses.AddRange(responses);
				}
			}

			yield return new AsyncResponseEvent(msg.Cookie, allResponses.ToArray());*/

			/*yield return new AsyncProgressResponseEvent
			{
				Cookie = msg.Cookie,
				Response = allResponses.ToArray(),
			};*/
		}

		private IEnumerable<DMSMessage> HandleDataMinerInfoMessage()
		{
			foreach (KeyValuePair<int, SimulatedDma> simulatedDma in Agents)
			{
				yield return new GetDataMinerInfoResponseMessage
				{
					ID = simulatedDma.Key,
				};
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetDataMinerByIDMessage msg)
		{
			yield return new GetDataMinerInfoResponseMessage
			{
				ComputerName = $"SimulatedHost{msg.ID}",
				Name = $"Simulated Agent {msg.ID}",
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(GetAgentBuildInfo msg)
		{
			yield return new BuildInfoResponse
			{
				Agents = new[]
				{
					new BuildInfoAgent
					{
						RawVersion = "10.5.6",
					}
				},
			};
		}
	}
}
