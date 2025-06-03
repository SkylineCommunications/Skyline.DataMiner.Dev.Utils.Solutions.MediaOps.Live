namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	/// <summary>
	/// Contains information about scheduler orchestration tasks.
	/// </summary>
	public class OrchestrationSchedulerTask
	{
		/// <summary>
		/// The name of the main MediaOps Live Orchestration script.
		/// </summary>
		public static readonly string OrchestrationScriptName = "ORC-AS-EventOrchestration";

		/// <summary>
		/// Default naming for the orchestration tasks.
		/// </summary>
		public static readonly string OrchestrationTaskNaming = "MediaOps Live Orchestration Event";

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationSchedulerTask"/> class.
		/// </summary>
		/// <param name="dateTimeOffset">Timestamp for the task.</param>
		/// <param name="orchestrationEventIds">IDs of the events to be orchestrated by the task.</param>
		public OrchestrationSchedulerTask(DateTimeOffset dateTimeOffset, IEnumerable<Guid> orchestrationEventIds)
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
		public OrchestrationSchedulerTask(DateTimeOffset dateTimeOffset, IEnumerable<Guid> orchestrationEventIds, ScheduledTaskId taskId) : this(dateTimeOffset, orchestrationEventIds)
		{
			ScheduledTaskId = taskId;
		}

		/// <summary>
		/// Gets or sets Unique identifier of the scheduled task in the DataMiner system.
		/// </summary>
		public ScheduledTaskId ScheduledTaskId { get; set; }

		/// <summary>
		/// Gets the IDs of all events that need to orchestrated by the scheduled task.
		/// </summary>
		public List<Guid> OrchestrationEventIds { get; }

		/// <summary>
		/// Gets the time of the scheduled task.
		/// </summary>
		public DateTimeOffset DateTime { get; }

		/// <summary>
		/// Generates the full object array data needed to create or update a scheduled orchestration task via IDms Class Library methods.
		/// </summary>
		/// <returns>An object array contains all configuration needed to create a scheduled orchestration task.</returns>
		public object[] GenerateSchedulerTaskData()
		{
			return new object[]
			{
				new object[] { GenerateGeneralInfoTaskData() },
				new object[] { GenerateActionsTaskData() },
				new object[] { },
			};
		}

		private string[] GenerateGeneralInfoTaskData()
		{
			List<string> generalInfoTaskData = ScheduledTaskId == null ? new List<string>() : new List<string> { ScheduledTaskId.TaskId.ToString() };

			generalInfoTaskData.AddRange(new[]
			{
				$"{OrchestrationTaskNaming} {DateTime.LocalDateTime:yyyy-MM-dd_HH:mm:ss}",
				DateTime.LocalDateTime.ToString("yyyy-MM-dd"),
				DateTime.LocalDateTime.AddDays(1).ToString("yyyy-MM-dd"),
				DateTime.LocalDateTime.ToString("HH:mm:ss"),
				"once",
				"1",
				String.Empty,
				OrchestrationTaskNaming,
				"TRUE",
				String.Empty,
				String.Empty,
			});

			return generalInfoTaskData.ToArray();
		}

		private string[] GenerateActionsTaskData()
		{
			return new[]
			{
				"automation",
				OrchestrationScriptName,
				$"PARAMETER:2:{JsonConvert.SerializeObject(OrchestrationEventIds)}",
				"CHECKSETS:FALSE",
				"DEFER:TRUE",
			};
		}
	}
}