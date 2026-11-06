namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Schedules the tasks that resume a node orchestration that is waiting on a phase.
	/// </summary>
	/// <remarks>
	/// A resume task runs the regular orchestration script for the event; the event is then in the Configuring state, so only
	/// the nodes whose resume is due are continued. Resume tasks use their own description, so the sliding window scheduler ignores them.
	/// </remarks>
	internal sealed class OrchestrationResumeScheduler
	{
		internal const string ResumeTaskNaming = "MediaOps Live Orchestration Resume";

		private readonly IConnection _connection;
		private readonly Lazy<IDms> _dms;
		private readonly int? _localAgentId;

		internal OrchestrationResumeScheduler(IConnection connection, int? localAgentId = null)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_dms = new Lazy<IDms>(() => connection.GetDms());
			_localAgentId = localAgentId;
		}

		/// <summary>
		/// Creates a task on the local agent, so the resumed execution runs where the concurrency guard of the node lives.
		/// </summary>
		/// <returns>The ID of the created task.</returns>
		internal ScheduledTaskId ScheduleResume(Guid eventId, DateTimeOffset resumeTime)
		{
			int agentId = _localAgentId ?? _connection.ServerDetails.AgentID;
			int taskId = _dms.Value.GetAgent(agentId).Scheduler.CreateTask(GenerateTaskData(eventId, resumeTime));

			return new ScheduledTaskId(agentId, taskId);
		}

		internal void DeleteResumeTask(ScheduledTaskId taskId)
		{
			if (taskId == null)
			{
				return;
			}

			// A task that no longer exists is fine: it already ran or was removed.
			_dms.Value.GetAgent(taskId.DmaId).Scheduler.DeleteTask(taskId.TaskId);
		}

		/// <summary>
		/// Gets the resume tasks that were due before the given time, with the event each one resumes.
		/// </summary>
		internal IReadOnlyCollection<KeyValuePair<ScheduledTaskId, Guid>> GetResumeTasksBefore(DateTimeOffset time)
		{
			var tasks = new List<KeyValuePair<ScheduledTaskId, Guid>>();

			foreach (SchedulerTask task in OrchestrationScheduler.LoadSchedulerTasks(_connection))
			{
				if (task.Description != ResumeTaskNaming
					|| new DateTimeOffset(DateTime.SpecifyKind(task.StartTime, DateTimeKind.Local)) >= time
					|| !TryGetEventId(task, out Guid eventId))
				{
					continue;
				}

				tasks.Add(new KeyValuePair<ScheduledTaskId, Guid>(new ScheduledTaskId(task.HandlingDMA, task.Id), eventId));
			}

			return tasks;
		}

		private static bool TryGetEventId(SchedulerTask task, out Guid eventId)
		{
			eventId = Guid.Empty;

			SchedulerAction action = task.Actions?.FirstOrDefault(x =>
				x.ActionType == SchedulerActionType.Automation && x.ScriptInstance?.ScriptName == Constants.OrchestrationScriptName);

			if (!(action?.ScriptInstance.ParameterIdToValue[0] is AutomationScriptInstanceInfo parameter))
			{
				return false;
			}

			try
			{
				eventId = JsonConvert.DeserializeObject<List<Guid>>(parameter.Value)?.FirstOrDefault() ?? Guid.Empty;
			}
			catch (JsonException)
			{
				return false;
			}

			return eventId != Guid.Empty;
		}

		internal static object[] GenerateTaskData(Guid eventId, DateTimeOffset resumeTime)
		{
			DateTime localTime = resumeTime.LocalDateTime;

			return
			[
				new object[]
				{
					new[]
					{
						$"{ResumeTaskNaming} {eventId} {localTime:yyyy-MM-dd_HH:mm:ss}",
						localTime.ToString("yyyy-MM-dd"),
						localTime.AddDays(1).ToString("yyyy-MM-dd"),
						localTime.ToString("HH:mm:ss"),
						"once",
						"1",
						String.Empty,
						ResumeTaskNaming,
						"TRUE",
						String.Empty,
						String.Empty,
					},
				},
				new object[]
				{
					new[]
					{
						"automation",
						Constants.OrchestrationScriptName,
						$"PARAMETER:2:{JsonConvert.SerializeObject(new List<Guid> { eventId })}",
						"CHECKSETS:FALSE",
						"DEFER:TRUE",
					},
				},
				new object[] { },
			];
		}
	}
}
