namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Automation.CustomEntryPoint;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.Advanced;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;

	public sealed class SimulatedDms
	{
		private readonly ConcurrentDictionary<int, SimulatedDma> _agents = new();
		private readonly ConcurrentBag<SimulatedAutomationScript> _scripts = [];
		private readonly ConcurrentBag<SLNetConnectionMock> _connections = [];
		private readonly ConcurrentBag<Parameter> _profileParameters = [];
		private readonly DomSLNetMessageHandler _domSlNetMessageHandler = new();

		public SimulatedDms()
		{
			_domSlNetMessageHandler.OnInstancesChanged += (s, e) => NotifySubscriptions(e);
		}

		public IReadOnlyDictionary<int, SimulatedDma> Agents => _agents;

		public IReadOnlyCollection<SimulatedAutomationScript> Scripts => _scripts;

		public IReadOnlyCollection<Parameter> ProfileParameters => _profileParameters;

		public SimulatedDma GetOrCreateAgent(int dmaId)
		{
			return _agents.GetOrAdd(
				dmaId,
				id => new SimulatedDma(this, id));
		}

		public void AddScript(string name, List<string> parameters, List<string> dummies, ScriptInfo orchestrationScriptInfo = null)
		{
			if (orchestrationScriptInfo == null)
			{
				_scripts.Add(new SimulatedAutomationScript(name, parameters, dummies, new ScriptInfo()));
				return;
			}

			_scripts.Add(new SimulatedAutomationScript(name, parameters, dummies, orchestrationScriptInfo) {Folder = "MediaOps/OrchestrationScripts" });
			foreach (KeyValuePair<string, Guid> profileParameter in orchestrationScriptInfo.ProfileParameters)
			{
				var param = new Parameter(profileParameter.Value)
				{
					Name = profileParameter.Key,
					Categories = ProfileParameterCategory.Monitoring,
				};

				if (profileParameter.Key.EndsWith("_String"))
				{
					param.Type = Parameter.ParameterType.Text;
				}
				else
				{
					param.Type = Parameter.ParameterType.Number;
					param.Decimals = 2;
					param.RangeMax = 1000;
					param.RangeMin = 0;
					param.Stepsize = 0.01;
					param.Units = "Units";
				}

				_profileParameters.Add(param);
			}
		}

		public IEnumerable<SimulatedSchedulerTask> GetAllDmsSchedulerTasks()
		{
			List<SimulatedSchedulerTask> tasks = [];

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

			if (_domSlNetMessageHandler.TryHandleMessage(message, out var response))
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

				case GetDataMinerByIDMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetAgentBuildInfo msg:
					responses = HandleMessage(msg);
					return true;

				case GetParameterMessage msg:
					responses = HandleMessage(msg);
					return true;

				case SetDataMinerInfoMessage msg:
					responses = HandleMessage(msg);
					return true;

				case ImpersonateMessage msg:
					responses = HandleMessage(msg);
					return true;

				case ExecuteScriptMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetScriptInfoMessage msg:
					responses = HandleMessage(msg);
					return true;

				case ManagerStoreStartPagingRequest<Parameter> msg:
					responses = HandleMessage(msg);
					return true;

				default:
					responses = [];
					return false;
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(ManagerStoreStartPagingRequest<Parameter> msg)
		{
			yield return new ManagerStorePagingResponse<Parameter>
			{
				IsFinalPage = true,
				Objects = _profileParameters.Where(msg.Filter.Filter.getLambda()).ToList(),
			};
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

		private IEnumerable<DMSMessage> HandleMessage(GetInfoMessage msg)
		{
			switch (msg.Type)
			{
				case InfoType.SchedulerTasks:
					return HandleSchedulerTaskInfoMessage();

				case InfoType.DataMinerInfo:
					return HandleDataMinerInfoMessage();

				case InfoType.Scripts:
					return HandleScriptsInfoMessage();

				default:
					throw new NotSupportedException("Not Supported");
			}
		}

		private IEnumerable<DMSMessage> HandleScriptsInfoMessage()
		{
			yield return new GetScriptsResponseMessage
			{
				Scripts = _scripts.Select(script => script.Name).ToArray(),
			};
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
				Agents =
				[
					new BuildInfoAgent
					{
						RawVersion = "10.5.6",
						DataMinerID = msg.DataMinerID,
					},
				],
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(ImpersonateMessage msg)
		{
			List<DMSMessage> responses = new();
			foreach (ClientRequestMessage clientRequestMessage in msg.Messages)
			{
				TryHandleMessage(clientRequestMessage, out IEnumerable<DMSMessage> msgResponses);
				responses.AddRange(msgResponses);
			}

			return responses;
		}

		private IEnumerable<DMSMessage> HandleMessage(ExecuteScriptMessage msg)
		{
			SimulatedAutomationScript script = Scripts.First(s => s.Name == msg.ScriptName);

			int returnCode = msg.ScriptName == "Script_Fail" ? -1 : 0;

			yield return new ExecuteScriptResponseMessage
			{
				saRet = new SA(
				[
					returnCode.ToString(), // Return code,
				]),
				EntryPointResult = new AutomationEntryPointResult(new RequestScriptInfoOutput
				{
					Data = new Dictionary<string, string> { { OrchestrationScript.OrchestrationScriptInfoRequestScriptInfoKey, JsonConvert.SerializeObject(script.OrchestrationScriptInfo) } },
				}),
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(GetScriptInfoMessage msg)
		{
			SimulatedAutomationScript script = Scripts.First(s => s.Name == msg.Name);

			int id = 1;
			yield return new GetScriptInfoResponseMessage
			{
				Parameters = script.Parameters.Select(param => new AutomationParameterInfo { Description = param, ParameterId = id++}).ToArray(),
				Name = msg.Name,
				Dummies = script.Dummies.Select(dummy => new AutomationProtocolInfo { ProtocolName = "Protocol", ProtocolVersion = "Production", Description = dummy, ProtocolId = id++ }).ToArray(),
				Memories = [],
				Type = AutomationScriptType.Automation,
				Exes =
				[
					new AutomationExeInfo()
					{
						Type = AutomationExeType.CSharpCode,
					},
				],
				Folder = script.Folder,
			};
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
	}
}
