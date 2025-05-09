namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	public class OrchestrationScheduler
	{
		private readonly IDms _dms;

		public OrchestrationScheduler(IDms dms)
		{
			_dms = dms ?? throw new ArgumentNullException(nameof(dms));
		}

		/// <summary>
		/// Triggers the creation or update of the scheduled task that is linked to the events.
		/// </summary>
		/// <param name="events">The list of events for which the scheduled task needs to be updated.</param>
		/// <returns>The list of events that was inputted, with an updated reference to the scheduled task.</returns>
		public IEnumerable<OrchestrationEvent> CreateOrUpdateEventTasks(IEnumerable<OrchestrationEvent> events)
		{
			IEnumerable<OrchestrationEvent> orchestrationEvents = events.ToList();
			foreach (OrchestrationEvent orchestrationEvent in orchestrationEvents)
			{
				switch (orchestrationEvent.EventState)
				{
					case SlcOrchestrationIds.Enums.EventState.Cancelled:
					case SlcOrchestrationIds.Enums.EventState.Draft:
						DeleteEventTask(orchestrationEvent);
						continue;

					case SlcOrchestrationIds.Enums.EventState.Confirmed:
						CreateOrUpdateEventTask(orchestrationEvent);
						continue;
				}
			}

			return orchestrationEvents;
		}

		public void DeleteEventTasks(IEnumerable<OrchestrationEvent> events)
		{
			foreach (OrchestrationEvent orchestrationEvent in events)
			{
				DeleteEventTask(orchestrationEvent);
			}
		}

		private void CreateOrUpdateEventTask(OrchestrationEvent orchestrationEvent)
		{
			OrchestrationSchedulerTask task = new OrchestrationSchedulerTask(orchestrationEvent);
			if (task.ScheduledTaskId != null)
			{
				_dms.GetAgent(task.ScheduledTaskId.DmaId).Scheduler.UpdateTask(task.GenerateSchedulerTaskData());
				return;
			}

			IDma dma = SelectRandomDma();
			int taskId = dma.Scheduler.CreateTask(task.GenerateSchedulerTaskData());
			orchestrationEvent.ReservationInstance = String.Join("/", dma.Id, taskId);
		}

		private void DeleteEventTask(OrchestrationEvent orchestrationEvent)
		{
			OrchestrationSchedulerTask task = new OrchestrationSchedulerTask(orchestrationEvent);

			if (task.ScheduledTaskId != null)
			{
				_dms.GetAgent(task.ScheduledTaskId.DmaId).Scheduler.DeleteTask(task.ScheduledTaskId.TaskId);
			}

			orchestrationEvent.ReservationInstance = null;
		}

		private IDma SelectRandomDma()
		{
			var agents = _dms.GetAgents().ToList();
			return agents[new Random().Next(agents.Count)];
		}
	}
}
