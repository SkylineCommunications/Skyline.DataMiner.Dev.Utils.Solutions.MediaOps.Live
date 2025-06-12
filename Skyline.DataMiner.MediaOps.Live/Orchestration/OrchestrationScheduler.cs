namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;

	public class OrchestrationScheduler
	{
		private readonly IDms _dms;
		private readonly IConnection _connection;

		private Lazy<HashSet<OrchestrationSchedulerTask>> _internalTaskList;

		public OrchestrationScheduler(IConnection connection)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_dms = connection.GetDms();

			_internalTaskList = new Lazy<HashSet<OrchestrationSchedulerTask>>(LoadInternalTaskList);
		}

		private HashSet<OrchestrationSchedulerTask> LoadInternalTaskList()
		{
			HashSet<OrchestrationSchedulerTask> list = new HashSet<OrchestrationSchedulerTask>();
			GetInfoMessage getSchedulerTaskInfoMessage = new GetInfoMessage(InfoType.SchedulerTasks);

			var progress = _connection.Async.Launch(getSchedulerTaskInfoMessage);

			var result = progress.WaitForAsyncResponse(5 * 60);

			if (result == null || !result.Messages.Any())
			{
				throw new DataMinerException("Scheduler task information could not be retrieved");
			}

			if (result.Failure != null)
			{
				throw result.Failure;
			}

			GetSchedulerTasksResponseMessage getSchedulerTasksResponse = (GetSchedulerTasksResponseMessage)result.Messages.FirstOrDefault();

			foreach (object taskObject in getSchedulerTasksResponse.Tasks)
			{
				SchedulerTask task = (SchedulerTask)taskObject;

				if (task.Description != OrchestrationSchedulerTask.OrchestrationTaskNaming)
				{
					continue;
				}

				SchedulerAction eventOrchestrationTask = task.Actions.FirstOrDefault(action =>
					action.ActionType == SchedulerActionType.Automation && action.ScriptInstance.ScriptName == OrchestrationSchedulerTask.OrchestrationScriptName);

				if (eventOrchestrationTask == null)
				{
					continue;
				}

				AutomationScriptInstanceInfo automationScriptInfo = (AutomationScriptInstanceInfo)eventOrchestrationTask.ScriptInstance.ParameterIdToValue[0];

				List<Guid> eventGuidsInput = JsonConvert.DeserializeObject<List<Guid>>(automationScriptInfo.Value);
				var existingTask = new OrchestrationSchedulerTask(DateTime.SpecifyKind(task.StartTime, DateTimeKind.Local), eventGuidsInput, new ScheduledTaskId(task.HandlingDMA, task.Id));

				list.Add(existingTask);
			}

			return list;
		}

		/// <summary>
		/// Triggers the creation or update of the scheduled task that is linked to the events.
		/// </summary>
		/// <param name="events">The list of events for which the scheduled task needs to be updated.</param>
		public void CreateOrUpdateEventScheduling(IEnumerable<OrchestrationEvent> events)
		{
			List<OrchestrationEvent> orchestrationEvents = events.ToList();
			IEnumerable<IGrouping<SlcOrchestrationIds.Enums.EventState?, OrchestrationEvent>> groupedByState = orchestrationEvents.GroupBy(e => e.EventState);

			foreach (IGrouping<SlcOrchestrationIds.Enums.EventState?, OrchestrationEvent> groupedByStateEvent in groupedByState)
			{
				switch (groupedByStateEvent.Key)
				{
					case SlcOrchestrationIds.Enums.EventState.Cancelled:
					case SlcOrchestrationIds.Enums.EventState.Draft:
						DeleteEventTasks(groupedByStateEvent);
						continue;

					case SlcOrchestrationIds.Enums.EventState.Confirmed:
						CreateOrUpdateEventTasks(groupedByStateEvent);
						continue;
				}
			}
		}

		public void DeleteEventTasks(IEnumerable<OrchestrationEvent> events)
		{
			foreach (OrchestrationEvent orchestrationEvent in events)
			{
				DeleteEventTask(orchestrationEvent);
			}
		}

		private void CreateOrUpdateEventTasks(IEnumerable<OrchestrationEvent> orchestrationEvents)
		{
			IEnumerable<IGrouping<DateTimeOffset?, OrchestrationEvent>> groupedByTimeEvents = orchestrationEvents.GroupBy(e => e.EventTime);

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
			OrchestrationSchedulerTask taskForTimeStamp = FindExistingTaskForTimeStamp(timestamp);

			foreach (OrchestrationEvent orchestrationEvent in orchestrationEvents)
			{
				// Create new task for timestamp during first iteration if none exist yet
				if (taskForTimeStamp == null)
				{
					DeleteEventTask(orchestrationEvent);
					taskForTimeStamp = new OrchestrationSchedulerTask(timestamp, new List<Guid> { orchestrationEvent.ID });
					IDma dma = SelectRandomDma();
					int newTaskId = dma.Scheduler.CreateTask(taskForTimeStamp.GenerateSchedulerTaskData());
					taskForTimeStamp.ScheduledTaskId = new ScheduledTaskId(dma.Id, newTaskId);
					orchestrationEvent.ReservationInstance = taskForTimeStamp.ScheduledTaskId;
					_internalTaskList.Value.Add(taskForTimeStamp);
					continue;
				}

				// Event already added to correct task
				if (orchestrationEvent.ReservationInstance == taskForTimeStamp.ScheduledTaskId)
				{
					continue;
				}

				DeleteEventTask(orchestrationEvent);
				taskForTimeStamp.OrchestrationEventIds.Add(orchestrationEvent.ID);
				_dms.GetAgent(taskForTimeStamp.ScheduledTaskId.DmaId).Scheduler.UpdateTask(taskForTimeStamp.GenerateSchedulerTaskData());
				orchestrationEvent.ReservationInstance = taskForTimeStamp.ScheduledTaskId;
			}
		}

		private void DeleteEventTask(OrchestrationEvent orchestrationEvent)
		{
			if (orchestrationEvent.ReservationInstance == null)
			{
				return;
			}

			OrchestrationSchedulerTask task = FindExistingTaskByTaskId(orchestrationEvent.ReservationInstance);

			task.OrchestrationEventIds.Remove(orchestrationEvent.ID);

			if (!task.OrchestrationEventIds.Any())
			{
				_dms.GetAgent(task.ScheduledTaskId.DmaId).Scheduler.DeleteTask(task.ScheduledTaskId.TaskId);
				_internalTaskList.Value.RemoveWhere(t => t.ScheduledTaskId.Equals(task.ScheduledTaskId));
			}
			else
			{
				_dms.GetAgent(task.ScheduledTaskId.DmaId).Scheduler.UpdateTask(task.GenerateSchedulerTaskData());
			}

			orchestrationEvent.ReservationInstance = null;
		}

		private IDma SelectRandomDma()
		{
			var agents = _dms.GetAgents().ToList();
			return agents[new Random().Next(agents.Count)];
		}

		private OrchestrationSchedulerTask FindExistingTaskForTimeStamp(DateTimeOffset timestamp)
		{
			return _internalTaskList.Value.FirstOrDefault(task => task.DateTime == timestamp);
		}

		private OrchestrationSchedulerTask FindExistingTaskByTaskId(ScheduledTaskId taskId)
		{
			return _internalTaskList.Value.FirstOrDefault(task => task.ScheduledTaskId.Equals(taskId));
		}
	}
}
