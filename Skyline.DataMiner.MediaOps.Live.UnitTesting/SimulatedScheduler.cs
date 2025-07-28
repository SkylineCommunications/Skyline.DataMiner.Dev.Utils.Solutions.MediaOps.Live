namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.Orchestration;

	public class SimulatedScheduler
	{
		private ConcurrentDictionary<int, SimulatedSchedulerTask> _tasks;

		public SimulatedScheduler(SimulatedDma dma)
		{
			Dma = dma ?? throw new ArgumentNullException(nameof(dma));
			_tasks = new ConcurrentDictionary<int, SimulatedSchedulerTask>();
		}

		public SimulatedDma Dma { get; }

		public IDictionary<int, SimulatedSchedulerTask> Tasks => _tasks;

		public SimulatedSchedulerTask CreateTask(OrchestrationSchedulerTask orchestrationSchedulerTask)
		{
			SimulatedSchedulerTask task = new SimulatedSchedulerTask(this, orchestrationSchedulerTask);

			if (!_tasks.TryAdd(orchestrationSchedulerTask.ScheduledTaskId.TaskId, task))
			{
				throw new InvalidOperationException($"Element with ID {orchestrationSchedulerTask.ScheduledTaskId.TaskId} already exists.");
			}

			return task;
		}

		public int GetFirstAvailableId()
		{
			return Enumerable.Range(1, Int32.MaxValue)
				.Except(_tasks.Keys.ToList())
				.FirstOrDefault();
		}
	}
}
