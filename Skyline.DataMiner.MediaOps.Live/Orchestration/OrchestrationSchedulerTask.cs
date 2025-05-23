namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration;

	public class OrchestrationSchedulerTask
	{
		public static readonly string OrchestrationScriptName = "ORC-AS-EventOrchestration";
		public static readonly string OrchestrationTaskNaming = "MediaOps Live Orchestration Event";

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

		public OrchestrationSchedulerTask(DateTimeOffset dateTimeOffset, IEnumerable<Guid> orchestrationEventIds, ScheduledTaskId taskId) : this(dateTimeOffset, orchestrationEventIds)
		{
			ScheduledTaskId = taskId;
		}

		public ScheduledTaskId ScheduledTaskId { get; set; }

		public List<Guid> OrchestrationEventIds { get; private set; }

		public DateTimeOffset DateTime { get;}

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

	public class ScheduledTaskId
	{
		public ScheduledTaskId(int dmaId, int taskId)
		{
			DmaId = dmaId;
			TaskId = taskId;
		}

		public int DmaId { get; set; }

		public int TaskId { get; set; }

		public bool Equals(ScheduledTaskId obj)
		{
			return DmaId == obj.DmaId && TaskId == obj.TaskId;
		}
	}
}
