namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// This class contains all logic to remove unnecessary objects from past orchestration events.
	/// </summary>
	public class OrchestrationCleanup
	{
		private readonly OrchestrationScheduler _scheduler;
		private readonly OrchestrationEventRepository _repository;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationCleanup"/> class.
		/// </summary>
		/// <param name="repository">Repository object needed for DOM updates.</param>
		public OrchestrationCleanup(OrchestrationEventRepository repository)
		{
			_scheduler = new OrchestrationScheduler(repository.Connection);
			_repository = repository;
		}

		/// <summary>
		/// Cleanup all past scheduler orchestration tasks and remove the task reference from events.
		/// </summary>
		/// <param name="time">The reference time.</param>
		public void CleanupSchedulerTasksBeforeTime(DateTimeOffset time, IEngine engine)
		{
			IEnumerable<OrchestrationSchedulerTask> tasksToRemove = _scheduler.GetEventTasksBeforeTime(time);
			engine.GenerateInformation($"Tasks to remove {JsonConvert.SerializeObject(tasksToRemove)}");
			CleanupTasks(tasksToRemove);
		}

		private void CleanupTasks(IEnumerable<OrchestrationSchedulerTask> tasksToRemove)
		{
			IEnumerable<OrchestrationSchedulerTask> orchestrationSchedulerTasksToRemove = tasksToRemove.ToList();
			if (!orchestrationSchedulerTasksToRemove.Any())
			{
				return;
			}

			IEnumerable<Guid> eventsFromTasksToRemove = orchestrationSchedulerTasksToRemove.SelectMany(task => task.OrchestrationEventIds);

			ORFilterElement<DomInstance> filter = new ORFilterElement<DomInstance>(eventsFromTasksToRemove.Select(id => FilterElementFactory.Create(DomInstanceExposers.Id, Comparer.Equals, id)).ToArray());
			List<OrchestrationEvent> pastEvents = _repository.Read(filter).ToList();

			_scheduler.DeleteEventTasks(pastEvents);

			_repository.CreateOrUpdate(pastEvents);
		}
	}
}