namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Core.DataMinerSystem.Common;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.ResourceManager.Objects;

	public class OrchestrationScheduler
	{
		private readonly IDms _dms;
		private readonly ResourceManagerHelper _resourceManager;

		public OrchestrationScheduler(ICommunication communication)
		{
			_dms = DmsFactory.CreateDms(communication);
			_resourceManager = new ResourceManagerHelper(communication.SendSingleResponseMessage);
		}

		public IEnumerable<OrchestrationSchedulerTask> GetAllScheduledOrchestrationTasks()
		{
			List<OrchestrationSchedulerTask> tasks = new List<OrchestrationSchedulerTask>();

			foreach (IDma dma in _dms.GetAgents())
			{
				IDmsScheduler dmaScheduler = dma.Scheduler;

				tasks.AddRange(dmaScheduler.GetTasks().Select(task => new OrchestrationSchedulerTask
				{
					Name = task.TaskName,
					TaskId = task.Id,
					DateTime = task.StartTime,
					Scheduler = dmaScheduler,
				}));
			}

			return tasks;
		}

		public void CreateOrUpdateTask(OrchestrationSchedulerTask task)
		{
			if (task.Scheduler == null)
			{
				task.Scheduler = SelectRandomScheduler();
				task.Create();
			}

			task.Update();
		}

		public static void DeleteTask(OrchestrationSchedulerTask task)
		{
			task.Scheduler.DeleteTask(task.TaskId);
		}

		private IDmsScheduler SelectRandomScheduler()
		{
			var agents = _dms.GetAgents().ToList();
			return agents[new Random().Next(agents.Count)].Scheduler;
		}
	}
}
