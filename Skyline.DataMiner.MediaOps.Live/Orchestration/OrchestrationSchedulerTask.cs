namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices.ComTypes;
	using System.Text;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	public class OrchestrationSchedulerTask
	{
		private const string ScriptName = "MediaOps_Orchestration_Script";

		public OrchestrationSchedulerTask()
		{
		}

		internal IDmsScheduler Scheduler { get; set; }

		public string Name { get; set; }

		public int TaskId { get; set; }

		public DateTime DateTime { get; set; }

		private object[] GenerateSchedulerTaskData()
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
			return new[]
			{
				Name,
				DateTime.ToString("yyyy-MM-dd"),
				DateTime.AddDays(1).ToString("yyyy-MM-dd"),
				DateTime.ToString("HH:mm:ss"),
				"once",
				"1",
				String.Empty,
				$"MediaOps Live Orchestration Event {Name}",
				"TRUE",
				String.Empty,
				String.Empty,
			};
		}

		private string[] GenerateActionsTaskData()
		{
			return new[]
			{
				"automation",
				ScriptName,
				String.Empty,
				String.Empty,
				"CHECKSETS:FALSE",
				"DEFER:False",
			};
		}

		internal void Create()
		{
			TaskId = Scheduler.CreateTask(GenerateSchedulerTaskData());
		}

		internal void Update()
		{
			Scheduler.UpdateTask(GenerateSchedulerTaskData());
		}
	}
}
