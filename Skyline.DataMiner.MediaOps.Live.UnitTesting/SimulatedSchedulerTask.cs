namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.Advanced;

	public class SimulatedSchedulerTask
	{
		private readonly SimulatedScheduler _scheduler;

		public SimulatedSchedulerTask(SimulatedScheduler scheduler, OrchestrationSchedulerTask orchestrationSchedulerTask) : this(scheduler)
		{
			Description = Constants.OrchestrationTaskNaming;
			TaskName = $"{Constants.OrchestrationTaskNaming} {orchestrationSchedulerTask.DateTime.LocalDateTime:yyyy-MM-dd_HH:mm:ss}";

			StartTime = orchestrationSchedulerTask.DateTime.LocalDateTime;
			EndTime = orchestrationSchedulerTask.DateTime.LocalDateTime.AddDays(1);
			Repetitions = 1;

			Actions.Add(new SchedulerAction
			{
				ActionType = SchedulerActionType.Automation,

				ScriptInstance = new AutomationScriptInstance()
				{
					ScriptName = Constants.OrchestrationScriptName,
					CheckSets = false,
					Synchronous = false,
					ParameterIdToValue = new ArrayList
					{
						new AutomationScriptInstanceInfo
						{
							IsValue = true,
							Key = 2,
							Value = JsonConvert.SerializeObject(orchestrationSchedulerTask.OrchestrationEventIds),
						},
					},
					Options = 0,
				},
			});
		}

		public SimulatedSchedulerTask(SimulatedScheduler scheduler)
		{
			_scheduler = scheduler;
			HandlingAgentId = scheduler.Dma.DmaId;
			Actions = new List<SchedulerAction>();
		}

		public SimulatedSchedulerTask(SimulatedScheduler scheduler, SetSchedulerInfoMessage msg) : this(scheduler)
		{
			var info = msg.Ppsa.Ppsa;
			var generalInfo = info[0].Psa[0].Sa.ToList();

			if (Int32.TryParse(generalInfo[0], out int taskId))
			{
				Id = taskId;
				generalInfo = generalInfo.Skip(1).ToList();
			}
			else
			{
				Id = _scheduler.GetFirstAvailableId();
			}

			TaskName = generalInfo[0];
			StartTime = DateTime.SpecifyKind(DateTime.ParseExact(generalInfo[1], "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTimeKind.Local) +
			            TimeSpan.ParseExact(generalInfo[3], @"hh\:mm\:ss", CultureInfo.InvariantCulture);
			EndTime = DateTime.SpecifyKind(DateTime.ParseExact(generalInfo[2], "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTimeKind.Local);
			RepetitionType = ParseTaskType(generalInfo[4]);
			RepetitionInterval = generalInfo[5];
			Repetitions = String.IsNullOrEmpty(generalInfo[6]) ? 0 : Convert.ToInt32(generalInfo[6]);
			Description = generalInfo[7];
			IsEnabled = generalInfo[8].ToLower() == "true";

			foreach (SA taskActionSa in info[1].Psa)
			{
				var actionsInfo = taskActionSa.Sa.ToList();

				Actions.Add(ParseAction(actionsInfo));
			}
		}

		public List<Guid> GetOrchestrationSchedulingInputList()
		{
			if (Description != Constants.OrchestrationTaskNaming)
			{
				return new List<Guid>();
			}

			SchedulerAction eventOrchestrationTask = Actions.FirstOrDefault(action =>
				action.ActionType == SchedulerActionType.Automation && action.ScriptInstance.ScriptName == Constants.OrchestrationScriptName);

			if (eventOrchestrationTask == null)
			{
				return new List<Guid>();
			}

			AutomationScriptInstanceInfo automationScriptInfo = (AutomationScriptInstanceInfo)eventOrchestrationTask.ScriptInstance.ParameterIdToValue[0];

			return JsonConvert.DeserializeObject<List<Guid>>(automationScriptInfo.Value);
		}

		private SchedulerAction ParseAction(List<string> actionsInfo)
		{
			return actionsInfo[0] switch
			{
				"automation" => ParseAutomationAction(actionsInfo),
				"information" => ParseInformationAction(actionsInfo),
				"notification" => ParseNotificationAction(actionsInfo),
				_ => null,
			};
		}

		private SchedulerAction ParseNotificationAction(List<string> actionsInfo)
		{
			throw new NotImplementedException();
		}

		private SchedulerAction ParseInformationAction(List<string> actionsInfo)
		{
			throw new NotImplementedException();
		}

		private SchedulerAction ParseAutomationAction(List<string> actionsInfo)
		{
			var scriptInstance = new AutomationScriptInstance
			{
				ScriptName = actionsInfo[1],
			};

			for (int i = 2; i < actionsInfo.Count; i++)
			{
				ParseAutomationScriptOption(scriptInstance, actionsInfo[i]);
			}

			return new SchedulerAction
			{
				ActionType = SchedulerActionType.Automation,
				ScriptInstance = scriptInstance,
			};
		}

		private void ParseAutomationScriptOption(AutomationScriptInstance scriptInstance, string option)
		{
			var splitOption = option.Split(':');

			switch (splitOption[0].ToLower())
			{
				case "checksets":
					scriptInstance.CheckSets = splitOption[1].ToLower() == "true";
					return;

				case "defer":
					scriptInstance.Synchronous = splitOption[1].ToLower() == "false";
					return;

				case "protocol":
					scriptInstance.ProtocolIdToElementId.Add(new AutomationScriptInstanceInfo
					{
						IsValue = true,
						Key = Convert.ToInt32(splitOption[1]),
						Value = String.Join("/", splitOption[2], splitOption[3]),
					});
					return;

				case "parameter":
					scriptInstance.ParameterIdToValue.Add(new AutomationScriptInstanceInfo
					{
						IsValue = true,
						Key = Convert.ToInt32(splitOption[1]),
						Value = splitOption[2],
					});
					return;

				default:
					return;
			}
		}

		private SchedulerRepeatType ParseTaskType(string type)
		{
			return type switch
			{
				"once" => SchedulerRepeatType.Once,
				"daily" => SchedulerRepeatType.Daily,
				"monthly" => SchedulerRepeatType.Monthly,
				"weekly" => SchedulerRepeatType.Weekly,
				_ => SchedulerRepeatType.Undefined,
			};
		}

		public bool ChooseAgent => false;

		public string Description { get; }

		public bool IsEnabled { get; }

		public DateTime EndTime { get; }

		public int HasRun { get; }

		public bool IsFinished { get; }

		public int HandlingAgentId { get; }

		public int Id { get; }

		public string LastRunResult { get; }

		public string LastRunTime { get; }

		public string NextRunTime { get; }

		public int Repetitions { get; }

		public string RepetitionInterval { get; }

		public string RepetitionIntervalInMinutes { get; }

		public bool Show { get; }

		public DateTime StartTime { get; }

		public string TaskName { get; }

		public SchedulerRepeatType RepetitionType { get; }

		public List<SchedulerAction> Actions { get; }

		public SchedulerTask ToSchedulerTaskInfo()
		{
			return new SchedulerTask
			{
				StartTime = StartTime,
				Actions = Actions.ToArray(),
				ChooseDMA = ChooseAgent,
				Description = Description,
				Enabled = IsEnabled,
				EndTime = EndTime,
				Executed = 0,
				Repeat = Repetitions,
				TaskName = TaskName,
				HandlingDMA = HandlingAgentId,
				Id = Id,
				LastRunTime = LastRunTime,
				NextRunTime = NextRunTime,
				RepeatInterval = RepetitionInterval,
				RepeatIntervalInMinutes = RepetitionIntervalInMinutes,
				Show = Show,
				Finished = IsFinished,
				LastExecuteResult = LastRunResult,
				RepeatType = RepetitionType,
				FinalActions = [],
			};
		}
	}
}
