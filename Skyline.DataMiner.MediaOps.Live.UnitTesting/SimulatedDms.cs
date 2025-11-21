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
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Automation.CustomEntryPoint;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.Advanced;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;
	using ParameterValue = Skyline.DataMiner.Net.Profiles.ParameterValue;

	public sealed class SimulatedDms
	{
		private readonly ConcurrentDictionary<int, SimulatedDma> _agents = new();
		private readonly ConcurrentBag<SimulatedAutomationScript> _scripts = [];
		private readonly ConcurrentBag<SLNetConnectionMock> _connections = [];
		private readonly ConcurrentBag<Parameter> _profileParameters = [];
		private readonly ConcurrentBag<ProfileDefinition> _profileDefinitions = [];
		private readonly ConcurrentBag<ProfileInstance> _profileInstances = [];
		private readonly DomSLNetMessageHandler _domSlNetMessageHandler = new(validateAgainstDefinition: true);

		public SimulatedDms()
		{
			_domSlNetMessageHandler.OnInstancesChanged += (s, e) => NotifySubscriptions(e);
		}

		public IReadOnlyDictionary<int, SimulatedDma> Agents => _agents;

		public IReadOnlyCollection<SimulatedAutomationScript> Scripts => _scripts;

		public IReadOnlyCollection<Parameter> ProfileParameters => _profileParameters;

		public IReadOnlyCollection<ProfileDefinition> ProfileDefinitions => _profileDefinitions;

		public IReadOnlyCollection<ProfileInstance> ProfileInstances => _profileInstances;

		public SimulatedDma GetOrCreateAgent(int dmaId)
		{
			return _agents.GetOrAdd(
				dmaId,
				id => new SimulatedDma(this, id));
		}

		public void AddScript(string name, ICollection<string> parameters = null, ICollection<string> dummies = null, ScriptInfo orchestrationScriptInfo = null)
		{
			parameters ??= [];
			dummies ??= [];

			if (orchestrationScriptInfo == null)
			{
				_scripts.Add(new SimulatedAutomationScript(name, parameters, dummies, new ScriptInfo()));
				return;
			}

			_scripts.Add(new SimulatedAutomationScript(name, parameters, dummies, orchestrationScriptInfo) { Folder = "MediaOps/OrchestrationScripts" });
		}

		public void AddProfileParameter(string parameterName, Guid parameterId, Parameter.ParameterType type)
		{
			Parameter param = new Parameter(parameterId)
			{
				Name = parameterName,
				Categories = ProfileParameterCategory.Monitoring,
				Type = type,
			};

			if (type == Parameter.ParameterType.Number)
			{
				param.Decimals = 2;
				param.RangeMax = 1000;
				param.RangeMin = 0;
				param.Stepsize = 0.01;
				param.Units = "Units";
			}

			_profileParameters.Add(param);
		}

		public void AddProfileDefinition(string name, Guid id, List<Guid> parameterIds)
		{
			ProfileDefinition definition = new ProfileDefinition(id)
			{
				Name = name,
			};

			foreach (Guid parameterId in parameterIds)
			{
				definition.Parameters.Add(_profileParameters.FirstOrDefault(param => param.ID == parameterId));
			}

			_profileDefinitions.Add(definition);
		}

		public void AddProfileInstance(string name, Guid id, Guid definition, Dictionary<Guid, object> parameterValues)
		{
			ProfileInstance instance = new ProfileInstance(id)
			{
				Name = name,
				AppliesToID = definition,
				AppliesTo = _profileDefinitions.FirstOrDefault(def => def.ID == definition),
			};

			List<ProfileParameterEntry> values = new List<ProfileParameterEntry>();
			foreach (Parameter parameter in instance.AppliesTo.Parameters)
			{
				ParameterValue paramValue = new ParameterValue
				{
					Type = parameter.Type == Parameter.ParameterType.Number ? ParameterValue.ValueType.Double : ParameterValue.ValueType.String,
				};

				if (paramValue.Type == ParameterValue.ValueType.Double)
				{
					paramValue.DoubleValue = Convert.ToDouble(parameterValues[parameter.ID]);
				}
				else
				{
					paramValue.StringValue = Convert.ToString(parameterValues[parameter.ID]);
				}
			}

			_profileInstances.Add(instance);
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

		public SLNetConnectionMock CreateConnection()
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

			foreach (SLNetConnectionMock connection in _connections)
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

			if (_domSlNetMessageHandler.TryHandleMessage(message, out DMSMessage response))
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

				case CheckAutomationCSharpSyntaxMessage msg:
					responses = HandleMessage(msg);
					return true;

				case ManagerStoreStartPagingRequest<Parameter> msg:
					responses = HandleMessage(msg);
					return true;

				case ManagerStoreStartPagingRequest<ProfileInstance> msg:
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

		private IEnumerable<DMSMessage> HandleMessage(ManagerStoreStartPagingRequest<ProfileInstance> msg)
		{
			yield return new ManagerStorePagingResponse<ProfileInstance>
			{
				IsFinalPage = true,
				Objects = _profileInstances.Where(msg.Filter.Filter.getLambda()).ToList(),
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(GetLiteElementInfo msg)
		{
			IEnumerable<SimulatedElement> elements = Agents.Values.SelectMany(x => x.Elements.Values);

			if (!String.IsNullOrEmpty(msg.ProtocolName))
			{
				elements = elements.Where(x => String.Equals(x.ProtocolName, msg.ProtocolName));
			}

			foreach (SimulatedElement element in elements)
			{
				yield return element.ToLiteElementInfo();
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetElementByIDMessage msg)
		{
			if (Agents.TryGetValue(msg.DataMinerID, out SimulatedDma dma) &&
				dma.Elements.TryGetValue(msg.ElementID, out SimulatedElement element))
			{
				yield return element.ToElementInfo();
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetElementByNameMessage msg)
		{
			IEnumerable<SimulatedElement> elements = Agents.Values.SelectMany(x => x.Elements.Values);
			SimulatedElement element = elements.FirstOrDefault(x => String.Equals(x.Name, msg.ElementName));

			if (element != null)
			{
				yield return element.ToElementInfo();
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetPartialTableMessage msg)
		{
			if (Agents.TryGetValue(msg.DataMinerID, out SimulatedDma dma) &&
				dma.Elements.TryGetValue(msg.ElementID, out SimulatedElement element) &&
				element.Tables.TryGetValue(msg.ParameterID, out TableParameter table))
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
			if (Agents.TryGetValue(msg.DataMinerID, out SimulatedDma dma))
			{
				int returnId;
				if (msg.What == 3) // Delete
				{
					dma.Scheduler.Tasks.Remove(msg.Info);
					returnId = 0;
				}
				else
				{
					PSA[] info = msg.Ppsa.Ppsa;
					SA[] generalInfo = info[0].Psa;
					string firstArgument = generalInfo[0].Sa[0];

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
			if (!Agents.TryGetValue(msg.DataMinerID, out SimulatedDma dma) ||
				!dma.Elements.TryGetValue(msg.ElId, out SimulatedElement element))
			{
				throw new InvalidOperationException($"Element with ID {msg.ElId} not found in DMA {msg.DataMinerID}.");
			}

			Net.Messages.ParameterValue paramValue;

			if (element.TryGetSpecialParameterValue(msg.ParameterId, out var specialValue))
			{
				paramValue = specialValue;
			}
			else if (element.Parameters.TryGetValue(msg.ParameterId, out StandaloneParameter param))
			{
				paramValue = param.ToParameterValue();
			}
			else
			{
				throw new InvalidOperationException($"Parameter with ID {msg.ParameterId} not found in Element {msg.ElId} on DMA {msg.DataMinerID}.");
			}

			yield return new GetParameterResponseMessage
			{
				DataMinerID = msg.DataMinerID,
				ElId = msg.ElId,
				ParameterId = msg.ParameterId,
				Value = paramValue,
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(SetDataMinerInfoMessage msg)
		{
			switch ((NotifyType)msg.What)
			{
				case NotifyType.GetKeyPosition:
					{
						int[] ids = (int[])msg.Var1;
						string key = (string)msg.Var2;

						if (Agents.TryGetValue(ids[0], out SimulatedDma dma) &&
							dma.Elements.TryGetValue(ids[1], out SimulatedElement element) &&
							element.Tables.TryGetValue(ids[2], out TableParameter table))
						{
							int index = table.Rows.Keys.ToList().IndexOf(key);

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
						object[] var1 = (object[])msg.Var1;

						if (Agents.TryGetValue((int)var1[0], out SimulatedDma dma) &&
							dma.Elements.TryGetValue((int)var1[1], out SimulatedElement element) &&
							element.Tables.TryGetValue((int)var1[2], out TableParameter table))
						{
							table.Rows.TryGetValue((string)var1[3], out object[] row);

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

				case InfoType.ElementInfo:
					return HandleElementInfoMessage();

				default:
					throw new NotSupportedException("Not Supported");
			}
		}

		private IEnumerable<DMSMessage> HandleElementInfoMessage()
		{
			foreach (SimulatedElement element in Agents.Values.SelectMany(agent => agent.Elements.Values))
			{
				yield return new ElementInfoEventMessage
				{
					Name = element.Name,
					Protocol = element.ProtocolName,
					ProtocolVersion = element.ProtocolVersion,
					DataMinerID = element.DmaId,
					ElementID = element.ElementId,
					State = element.State,
					HostingAgentID = element.HostingDmaId,
				};
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
			var responses = new List<DMSMessage>();

			foreach (var clientRequestMessage in msg.Messages)
			{
				if (TryHandleMessage(clientRequestMessage, out var msgResponses))
				{
					responses.AddRange(msgResponses);
				}
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
					Data = new Dictionary<string, string> { { OrchestrationScriptConstants.OrchestrationScriptInfoRequestScriptInfoKey, JsonConvert.SerializeObject(script.OrchestrationScriptInfo) } },
				}),
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(GetScriptInfoMessage msg)
		{
			var script = Scripts.FirstOrDefault(s => s.Name == msg.Name);

			if (script == null)
			{
				throw new InvalidOperationException($"Script with name '{msg.Name}' not found. Ensure the script is registered using {nameof(AddScript)}() before attempting to retrieve it. Available scripts: [{String.Join(", ", Scripts.Select(s => s.Name))}]");
			}

			int id = 1;
			yield return new GetScriptInfoResponseMessage
			{
				Parameters = script.Parameters.Select(param => new AutomationParameterInfo { Description = param, ParameterId = id++ }).ToArray(),
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

		private IEnumerable<DMSMessage> HandleMessage(CheckAutomationCSharpSyntaxMessage msg)
		{
			var script = Scripts.FirstOrDefault(s => s.Name == msg.ScriptName);

			if (script == null)
			{
				throw new InvalidOperationException($"Script with name '{msg.ScriptName}' not found. Ensure the script is registered using {nameof(AddScript)}() before attempting to retrieve it. Available scripts: [{String.Join(", ", Scripts.Select(s => s.Name))}]");
			}

			// For simulation purposes, we assume all scripts are syntactically correct.
			yield return new CheckAutomationCSharpSyntaxResponse
			{
				Errors = Array.Empty<string>(),
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
