namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.MediaOps.Live;

	/// <summary>
	/// Contains information about scheduler orchestration tasks.
	/// </summary>
	public class OrchestrationSchedulerTask
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationSchedulerTask"/> class.
		/// </summary>
		/// <param name="dateTimeOffset">Timestamp for the task.</param>
		/// <param name="orchestrationEventIds">IDs of the events to be orchestrated by the task.</param>
		internal OrchestrationSchedulerTask(DateTimeOffset dateTimeOffset, IEnumerable<Guid> orchestrationEventIds)
		{
			if (orchestrationEventIds == null)
			{
				throw new ArgumentNullException(nameof(orchestrationEventIds));
			}

			OrchestrationEventIds = orchestrationEventIds.ToList();
			DateTime = dateTimeOffset;

			if (!OrchestrationEventIds.Any())
			{
				throw new ArgumentException("List of events for scheduler task should not be empty");
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationSchedulerTask"/> class, with a known task ID.
		/// </summary>
		/// <param name="dateTimeOffset">Timestamp for the task.</param>
		/// <param name="orchestrationEventIds">IDs of the events to be orchestrated by the task.</param>
		/// <param name="taskId">The task ID.</param>
		private OrchestrationSchedulerTask(DateTimeOffset dateTimeOffset, IEnumerable<Guid> orchestrationEventIds, ScheduledTaskId taskId) : this(dateTimeOffset, orchestrationEventIds)
		{
			ScheduledTaskId = taskId;
		}

		/// <summary>
		/// Gets or sets Unique identifier of the scheduled task in the DataMiner system.
		/// </summary>
		internal ScheduledTaskId ScheduledTaskId { get; set; }

		/// <summary>
		/// Gets the IDs of all events that need to orchestrated by the scheduled task.
		/// </summary>
		internal List<Guid> OrchestrationEventIds { get; }

		/// <summary>
		/// Gets the time of the scheduled task.
		/// </summary>
		internal DateTimeOffset DateTime { get; }

		/// <summary>
		/// Generates the full object array data needed to create or update a scheduled orchestration task via IDms Class Library methods.
		/// </summary>
		/// <returns>An object array contains all configuration needed to create a scheduled orchestration task.</returns>
		internal object[] GenerateSchedulerTaskData()
		{
			return
			[
				new object[] { GenerateGeneralInfoTaskData() },
				new object[] { GenerateActionsTaskData() },
				new object[] { },
			];
		}

		internal static bool TryParseFromSchedulerTask(SchedulerTask task, out OrchestrationSchedulerTask orchestrationTask)
		{
			orchestrationTask = null;

			if (task.Description != Constants.OrchestrationTaskNaming)
			{
				return false;
			}

			SchedulerAction eventOrchestrationTask = task.Actions.FirstOrDefault(action =>
				action.ActionType == SchedulerActionType.Automation && action.ScriptInstance.ScriptName == Constants.OrchestrationScriptName);

			if (eventOrchestrationTask == null)
			{
				return false;
			}

			AutomationScriptInstanceInfo automationScriptInfo = (AutomationScriptInstanceInfo)eventOrchestrationTask.ScriptInstance.ParameterIdToValue[0];

			List<Guid> eventGuidsInput = JsonConvert.DeserializeObject<List<Guid>>(automationScriptInfo.Value);
			orchestrationTask = new(System.DateTime.SpecifyKind(task.StartTime, DateTimeKind.Local), eventGuidsInput, new ScheduledTaskId(task.HandlingDMA, task.Id));

			return true;
		}

		private string[] GenerateGeneralInfoTaskData()
		{
			List<string> generalInfoTaskData = ScheduledTaskId == null ? [] : [ScheduledTaskId.TaskId.ToString()];

			generalInfoTaskData.AddRange([
				$"{Constants.OrchestrationTaskNaming} {DateTime.LocalDateTime:yyyy-MM-dd_HH:mm:ss}",
				DateTime.LocalDateTime.ToString("yyyy-MM-dd"),
				DateTime.LocalDateTime.AddDays(1).ToString("yyyy-MM-dd"),
				DateTime.LocalDateTime.ToString("HH:mm:ss"),
				"once",
				"1",
				string.Empty,
				Constants.OrchestrationTaskNaming,
				"TRUE",
				string.Empty,
				string.Empty,
			]);

			return generalInfoTaskData.ToArray();
		}

		private string[] GenerateActionsTaskData()
		{
			return
			[
				"automation",
				Constants.OrchestrationScriptName,
				$"PARAMETER:2:{JsonConvert.SerializeObject(OrchestrationEventIds)}",
				"CHECKSETS:FALSE",
				"DEFER:TRUE",
			];
		}
	}
}