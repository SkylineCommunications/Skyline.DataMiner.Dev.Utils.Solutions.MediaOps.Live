namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices.ComTypes;
	using System.Text;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration;
	using Skyline.DataMiner.Net;

	public class OrchestrationSchedulerTask
	{
		private const string ScriptName = "ORC-AS-EventOrchestration";
		private readonly OrchestrationEvent _orchestrationEvent;

		public OrchestrationSchedulerTask(OrchestrationEvent orchestrationEvent)
		{
			if (!orchestrationEvent.EventTime.HasValue)
			{
				throw new InvalidOperationException($"Invalid event time for event {orchestrationEvent.Name}");
			}

			if (!String.IsNullOrEmpty(orchestrationEvent.ReservationInstance))
			{
				string[] splitTaskReference = orchestrationEvent.ReservationInstance.Split('/');

				if (splitTaskReference.Length != 2)
				{
					throw new InvalidOperationException($"Invalid task reference for event {orchestrationEvent.Name}");
				}

				int dmaId = Convert.ToInt32(splitTaskReference[0]);
				int taskId = Convert.ToInt32(splitTaskReference[1]);

				ScheduledTaskId = new ScheduledTaskId(dmaId, taskId);
			}

			_orchestrationEvent = orchestrationEvent;
		}

		public ScheduledTaskId ScheduledTaskId { get; set; }

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
				_orchestrationEvent.Name,
				_orchestrationEvent.EventTime.Value.ToString("yyyy-MM-dd"),
				_orchestrationEvent.EventTime.Value.AddDays(1).ToString("yyyy-MM-dd"),
				_orchestrationEvent.EventTime.Value.ToString("HH:mm:ss"),
				"once",
				"1",
				String.Empty,
				$"MediaOps Live Orchestration Event \"{_orchestrationEvent.Name}\"",
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
				ScriptName,
				$"PARAMETER:2:{_orchestrationEvent.ID}",
				"CHECKSETS:FALSE",
				"DEFER:FALSE",
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
	}
}
