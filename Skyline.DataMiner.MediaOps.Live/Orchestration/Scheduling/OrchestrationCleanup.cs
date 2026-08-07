namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Tools;

	/// <summary>
	/// This class contains all logic to remove unnecessary objects from past orchestration events.
	/// </summary>
	internal class OrchestrationCleanup
	{
		private readonly OrchestrationScheduler _scheduler;
		private readonly OrchestrationEventRepository _repository;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationCleanup"/> class.
		/// </summary>
		/// <param name="repository">Repository object needed for DOM updates.</param>
		internal OrchestrationCleanup(OrchestrationEventRepository repository)
		{
			_scheduler = new OrchestrationScheduler(repository.Connection);
			_repository = repository;
		}

		/// <summary>
		/// Cleanup all past scheduler orchestration tasks and remove the task reference from events.
		/// </summary>
		/// <param name="time">The reference time.</param>
		internal void CleanupSchedulerTasksBeforeTime(DateTimeOffset time)
		{
			OrchestrationSchedulerTask[] tasksToRemove = _scheduler.GetEventTasksBeforeTime(time).ToArray();
			if (tasksToRemove.Any())
			{
				IEnumerable<ScheduledTaskId> deletedTaskIds = _scheduler.DeleteTasks(tasksToRemove.Select(t => t.ScheduledTaskId));
				UpdateEvents(tasksToRemove.Where(t => deletedTaskIds.Contains(t.ScheduledTaskId)));
			}
		}

		private void UpdateEvents(IEnumerable<OrchestrationSchedulerTask> removedTasks)
		{
			if (!removedTasks.Any())
			{
				return;
			}

			HashSet<ScheduledTaskId> removedTaskIds = removedTasks.Select(t => t.ScheduledTaskId).ToHashSet();
			IEnumerable<Guid> eventsFromTasksToRemove = removedTasks.SelectMany(task => task.OrchestrationEventIds);
			ORFilterElement<DomInstance> filter = new ORFilterElement<DomInstance>(eventsFromTasksToRemove.Select(id => FilterElementFactory.Create(DomInstanceExposers.Id, Comparer.Equals, id)).ToArray());
			List<OrchestrationEvent> pastEvents = _repository.ReadDom(filter).ToList();
			foreach (var pastEvent in pastEvents)
			{
				if (removedTaskIds.Contains(pastEvent.SchedulerReference))
					pastEvent.SchedulerReference = null;
			}

			_repository.CreateOrUpdate(pastEvents);
		}
	}
}