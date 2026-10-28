namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Async;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.MediaOps.Live;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

	/// <summary>
	/// Class that handled orchestration scheduled tasks that execute the orchestration for orchestration events.
	/// </summary>
	internal class OrchestrationScheduler
	{
		// A task this close to its time may already be running when a change reaches the scheduler.
		private static readonly TimeSpan DueMargin = TimeSpan.FromSeconds(5);

		private readonly IDms _dms;
		private readonly IConnection _connection;

		private HashSet<OrchestrationSchedulerTask> _taskList;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationScheduler"/> class.
		/// </summary>
		/// <param name="connection">DataMiner user connection.</param>
		/// <exception cref="ArgumentNullException">Connection cannot be null.</exception>
		internal OrchestrationScheduler(IConnection connection)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_dms = connection.GetDms();
		}

		private HashSet<OrchestrationSchedulerTask> TaskList => _taskList ??= LoadInternalTaskList();

		private HashSet<OrchestrationSchedulerTask> LoadInternalTaskList()
		{
			HashSet<OrchestrationSchedulerTask> list = [];
			GetInfoMessage getSchedulerTaskInfoMessage = new(InfoType.SchedulerTasks);

			AsyncProgress progress = _connection.Async.Launch(getSchedulerTaskInfoMessage);

			/*var pool = AsyncResponseHandler*/

			AsyncResponseEvent result = progress.WaitForAsyncResponse(/*5 * 60*/5);

			if (result == null || !result.Messages.Any())
			{
				throw new DataMinerException("Scheduler task information could not be retrieved");
			}

			if (result.Failure != null)
			{
				throw result.Failure;
			}

			GetSchedulerTasksResponseMessage getSchedulerTasksResponse = (GetSchedulerTasksResponseMessage)result.Messages.FirstOrDefault();

			if (getSchedulerTasksResponse == null)
			{
				throw new InvalidOperationException("Scheduler task could not be retrieved.");
			}

			foreach (object taskObject in getSchedulerTasksResponse.Tasks)
			{
				SchedulerTask task = (SchedulerTask)taskObject;

				if (OrchestrationSchedulerTask.TryParseFromSchedulerTask(task, out OrchestrationSchedulerTask orchestrationTask))
				{
					list.Add(orchestrationTask);
				}
			}

			return list;
		}

		/// <summary>
		/// Get all events that executed before a certain timestamp.
		/// </summary>
		/// <param name="time">The reference timestamp.</param>
		/// <returns>A collection of tasks scheduled before the give time.</returns>
		internal IEnumerable<OrchestrationSchedulerTask> GetEventTasksBeforeTime(DateTimeOffset time)
		{
			return TaskList.Where(task => task.DateTime.UtcDateTime < time.ToUniversalTime());
		}

		/// <summary>
		/// Get all events that executed after a certain time.
		/// </summary>
		/// <param name="time">The reference timestamp.</param>
		/// <returns>A collection of tasks scheduled after the give time.</returns>
		internal IEnumerable<OrchestrationSchedulerTask> GetEventTasksAfterTime(DateTimeOffset time)
		{
			return TaskList.Where(task => task.DateTime.UtcDateTime > time.ToUniversalTime());
		}

		/// <summary>
		/// Get all events in a time range.
		/// </summary>
		/// <param name="from">The starting reference timestamp.</param>
		/// <param name="to">The ending reference timestamp.</param>
		/// <returns>A collection of task in given time range.</returns>
		internal IEnumerable<OrchestrationSchedulerTask> GetEventTasksInTimeRange(DateTimeOffset from, DateTimeOffset to)
		{
			return TaskList.Where(task => task.DateTime.UtcDateTime <= to.ToUniversalTime() && task.DateTime.UtcDateTime >= from.ToUniversalTime());
		}

		/// <summary>
		/// Triggers the creation or update of the scheduled task that is linked to the events.
		/// </summary>
		/// <param name="events">The list of events for which the scheduled task needs to be updated.</param>
		internal void CreateOrUpdateEventScheduling(IEnumerable<OrchestrationEvent> events)
		{
			List<OrchestrationEvent> orchestrationEvents = events.ToList();
			IEnumerable<IGrouping<EventState, OrchestrationEvent>> groupedByState = orchestrationEvents.GroupBy(e => e.EventState);

			foreach (IGrouping<EventState, OrchestrationEvent> groupedByStateEvent in groupedByState)
			{
				switch (groupedByStateEvent.Key)
				{
					case EventState.Cancelled:
					case EventState.Draft:
						DeleteEventTasks(groupedByStateEvent.Where(e => e.SchedulerReference != null));
						continue;

					case EventState.Confirmed:
						CreateOrUpdateEventTasks(groupedByStateEvent);
						continue;
				}
			}
		}

		/// <summary>
		/// Delete scheduled orchestration task and remove reference on the orchestration event.
		/// </summary>
		/// <param name="events">List of event to remove corresponding task for.</param>
		internal void DeleteEventTasks(IEnumerable<OrchestrationEvent> events)
		{
			List<OrchestrationEvent> scheduledEvents = events.Where(e => e.SchedulerReference != null).ToList();
			if (!scheduledEvents.Any())
			{
				return;
			}

			// The task is resolved through the event's scheduler reference rather than through its time, so an event whose
			// time changed after it was scheduled (for example when it is moved into the past to execute immediately) still
			// has its original task removed.
			bool refreshed = false;

			foreach (OrchestrationEvent orchestrationEvent in scheduledEvents)
			{
				if (!DeleteEventTaskForEvent(orchestrationEvent, ref refreshed)
					|| orchestrationEvent.EventState == EventState.Cancelled
					|| !orchestrationEvent.EventTime.HasValue
					|| orchestrationEvent.ActualStartTime >= orchestrationEvent.EventTime)
				{
					continue;
				}

				// The due task executes the event, so it counts as started; the scheduled time keeps the event order valid.
				orchestrationEvent.ActualStartTime = orchestrationEvent.EventTime;
			}
		}

		internal ICollection<ScheduledTaskId> DeleteTasks(IEnumerable<ScheduledTaskId> taskIds)
		{
			var deletedTaskIds = new List<ScheduledTaskId>();
			foreach (ScheduledTaskId taskId in taskIds)
			{
				if (_dms.GetAgent(taskId.DmaId).Scheduler.DeleteTask(taskId.TaskId) == 0)
				{
					TaskList.RemoveWhere(t => t.ScheduledTaskId.Equals(taskId));
					deletedTaskIds.Add(taskId);
				}
			}

			return deletedTaskIds;
		}

		private void CreateOrUpdateEventTasks(IEnumerable<OrchestrationEvent> orchestrationEvents)
		{
			IEnumerable<IGrouping<DateTimeOffset?, OrchestrationEvent>> groupedByTimeEvents = orchestrationEvents.GroupBy(e => e.EventTime).OrderBy(g => g.Key);

			foreach (IGrouping<DateTimeOffset?, OrchestrationEvent> groupedByTimeEvent in groupedByTimeEvents)
			{
				if (groupedByTimeEvent.Key.HasValue)
				{
					CreateOrUpdateEventTasksForTimeStamp(groupedByTimeEvent.Key.Value, groupedByTimeEvent.ToList());
				}
			}
		}

		private void CreateOrUpdateEventTasksForTimeStamp(DateTimeOffset timestamp, List<OrchestrationEvent> orchestrationEvents)
		{
			// Since a due task is never updated, several tasks can exist for the same time.
			List<OrchestrationSchedulerTask> tasksForTimeStamp = TaskList.Where(task => task.DateTime == timestamp).ToList();
			OrchestrationSchedulerTask upcomingTask = tasksForTimeStamp.FirstOrDefault(task => !IsDue(task));
			var addedEvents = new List<OrchestrationEvent>();
			var eventsForNewTask = new List<OrchestrationEvent>();
			bool refreshed = false;

			foreach (OrchestrationEvent orchestrationEvent in orchestrationEvents)
			{
				// Tasks that already ran are kept until the sliding window removes them, so only upcoming tasks count.
				if (TaskList.Count(task => task.DateTime > DateTimeOffset.UtcNow) >= Constants.SchedulerMaxTotalOrchestrationEvents)
				{
					throw new InvalidOperationException($"Orchestration task limit: {Constants.SchedulerMaxTotalOrchestrationEvents} has been reached. Likely some events have not been scheduled");
				}

				// Cached task contents can be stale, so a reference to another task takes precedence.
				OrchestrationSchedulerTask owningTask = tasksForTimeStamp.FirstOrDefault(task =>
					task.OrchestrationEventIds.Contains(orchestrationEvent.ID)
					&& (orchestrationEvent.SchedulerReference == null || orchestrationEvent.SchedulerReference.Equals(task.ScheduledTaskId)));
				if (owningTask != null)
				{
					orchestrationEvent.SchedulerReference = owningTask.ScheduledTaskId;
					continue;
				}

				// An event that its due task or a hand-off already started is not scheduled again.
				if (orchestrationEvent.ActualStartTime >= orchestrationEvent.EventTime)
				{
					continue;
				}

				// A due task executes the event itself, so it must not be added to another task.
				if (DeleteEventTaskForEvent(orchestrationEvent, ref refreshed))
				{
					continue;
				}

				if (upcomingTask == null)
				{
					eventsForNewTask.Add(orchestrationEvent);
					continue;
				}

				if (!upcomingTask.OrchestrationEventIds.Contains(orchestrationEvent.ID))
				{
					upcomingTask.OrchestrationEventIds.Add(orchestrationEvent.ID);
				}

				orchestrationEvent.SchedulerReference = upcomingTask.ScheduledTaskId;
				addedEvents.Add(orchestrationEvent);
			}

			if (addedEvents.Any())
			{
				if (IsDue(upcomingTask))
				{
					// The task became due while the events were added, so they move to a new task instead.
					upcomingTask.OrchestrationEventIds.RemoveAll(id => addedEvents.Any(e => e.ID == id));
					eventsForNewTask.AddRange(addedEvents);
				}
				else
				{
					_dms.GetAgent(upcomingTask.ScheduledTaskId.DmaId).Scheduler.UpdateTask(upcomingTask.GenerateSchedulerTaskData());
				}
			}

			// One task for all of them, so events at the same time keep running in event type order.
			if (eventsForNewTask.Any())
			{
				CreateTask(timestamp, eventsForNewTask);
			}
		}

		private OrchestrationSchedulerTask CreateTask(DateTimeOffset timestamp, List<OrchestrationEvent> orchestrationEvents)
		{
			var task = new OrchestrationSchedulerTask(timestamp, orchestrationEvents.Select(e => e.ID));
			IDma dma = SelectRandomDma();
			int newTaskId = dma.Scheduler.CreateTask(task.GenerateSchedulerTaskData());
			task.ScheduledTaskId = new ScheduledTaskId(dma.Id, newTaskId);
			TaskList.Add(task);

			foreach (OrchestrationEvent orchestrationEvent in orchestrationEvents)
			{
				orchestrationEvent.SchedulerReference = task.ScheduledTaskId;
			}

			return task;
		}

		// Removes the event from its task, unless that task is due: the scheduler runs a (nearly) due task again when it is
		// updated, so such a task keeps the event and is left to the sliding window cleanup. Returns whether a due task kept it.
		private bool DeleteEventTaskForEvent(OrchestrationEvent orchestrationEvent, ref bool refreshed)
		{
			if (orchestrationEvent.SchedulerReference == null)
			{
				return false;
			}

			OrchestrationSchedulerTask task = FindExistingTaskByTaskId(orchestrationEvent.SchedulerReference);

			// A missing task can have been created, and a due task deleted, by another process after the list was loaded.
			if ((task == null || IsDue(task)) && !refreshed)
			{
				RefreshTaskList();
				refreshed = true;
				task = FindExistingTaskByTaskId(orchestrationEvent.SchedulerReference);
			}

			if (task == null)
			{
				orchestrationEvent.SchedulerReference = null;
				return false;
			}

			if (IsDue(task))
			{
				return true;
			}

			task.OrchestrationEventIds.Remove(orchestrationEvent.ID);

			if (!task.OrchestrationEventIds.Any())
			{
				_dms.GetAgent(task.ScheduledTaskId.DmaId).Scheduler.DeleteTask(task.ScheduledTaskId.TaskId);
				TaskList.RemoveWhere(t => t.ScheduledTaskId.Equals(task.ScheduledTaskId));
			}
			else
			{
				_dms.GetAgent(task.ScheduledTaskId.DmaId).Scheduler.UpdateTask(task.GenerateSchedulerTaskData());
			}

			orchestrationEvent.SchedulerReference = null;
			return false;
		}

		// Brings the cached task list in line with the scheduler, keeping the cached task objects because they may have
		// pending changes.
		private void RefreshTaskList()
		{
			List<OrchestrationSchedulerTask> currentTasks = LoadInternalTaskList().ToList();
			var currentTaskIds = new HashSet<ScheduledTaskId>(currentTasks.Select(task => task.ScheduledTaskId));

			TaskList.RemoveWhere(task => !currentTaskIds.Contains(task.ScheduledTaskId));

			foreach (OrchestrationSchedulerTask task in currentTasks)
			{
				if (FindExistingTaskByTaskId(task.ScheduledTaskId) == null)
				{
					TaskList.Add(task);
				}
			}
		}

		private static bool IsDue(OrchestrationSchedulerTask task)
		{
			return task.DateTime <= DateTimeOffset.UtcNow + DueMargin;
		}

		private IDma SelectRandomDma()
		{
			List<IDma> agents = _dms.GetAgents().ToList();
			return agents[new Random().Next(agents.Count)];
		}

		private OrchestrationSchedulerTask FindExistingTaskByTaskId(ScheduledTaskId taskId)
		{
			return TaskList.FirstOrDefault(task => task.ScheduledTaskId.Equals(taskId));
		}
	}
}
