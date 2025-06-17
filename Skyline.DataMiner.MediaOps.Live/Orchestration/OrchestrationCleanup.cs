namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// This class contains all logic to remove unnecessary objects from past orchestration events.
	/// </summary>
	public class OrchestrationCleanup
	{
		private readonly OrchestrationScheduler _scheduler;
		private readonly SlcOrchestrationHelper _orchestrationHelper;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationCleanup"/> class.
		/// </summary>
		/// <param name="connection">DataMiner user connection.</param>
		public OrchestrationCleanup(IConnection connection)
		{
			_scheduler = new OrchestrationScheduler(connection);
			_orchestrationHelper = new SlcOrchestrationHelper(connection);
		}

		/// <summary>
		/// Cleanup all past scheduler orchestration tasks and remove the task reference from events.
		/// </summary>
		/// <param name="time">The reference time.</param>
		public void CleanupSchedulerTasksBeforeTime(DateTimeOffset time)
		{
			IEnumerable<OrchestrationSchedulerTask> tasksToRemove = _scheduler.GetEventTasksBeforeTime(time);
			CleanupTasks(tasksToRemove);
		}

		private void CleanupTasks(IEnumerable<OrchestrationSchedulerTask> tasksToRemove)
		{
			IEnumerable<Guid> eventsFromTasksToRemove = tasksToRemove.SelectMany(task => task.OrchestrationEventIds);

			ORFilterElement<DomInstance> filter = new ORFilterElement<DomInstance>(eventsFromTasksToRemove.Select(id => FilterElementFactory.Create(DomInstanceExposers.Id, Comparer.Equals, id)).ToArray());
			IEnumerable<OrchestrationEventInstance> pastEvents = _orchestrationHelper.GetOrchestrationEvents(filter);

			_scheduler.DeleteEventTasks(pastEvents.Select(instance => new OrchestrationEvent(instance)));
		}
	}
}