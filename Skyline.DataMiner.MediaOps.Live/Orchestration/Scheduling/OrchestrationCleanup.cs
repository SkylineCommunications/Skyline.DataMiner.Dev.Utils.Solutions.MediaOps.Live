namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Plan;

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
			if (!tasksToRemove.Any())
			{
				return;
			}

			ICollection<ScheduledTaskId> deletedTaskIds = _scheduler.DeleteTasks(tasksToRemove.Select(t => t.ScheduledTaskId).ToArray());
			UpdateEvents(tasksToRemove.Where(t => deletedTaskIds.Contains(t.ScheduledTaskId)));
		}

		/// <summary>
		/// Removes the past resume tasks that no node waits for anymore, for example because the resumed execution failed before it removed its task.
		/// With a plan helper, a resume that is still awaited but never ran fails its event.
		/// </summary>
		/// <param name="time">The reference time.</param>
		/// <param name="planHelper">The MediaOps.PLAN helper to report failed events to, or <see langword="null"/> to keep missed resumes.</param>
		internal void CleanupResumeTasksBeforeTime(DateTimeOffset time, IMediaOpsPlanHelper planHelper)
		{
			var resumeScheduler = new OrchestrationResumeScheduler(_repository.Connection);

			if (planHelper != null)
			{
				FailMissedResumes(time, resumeScheduler, planHelper);
			}

			foreach (IGrouping<Guid, ScheduledTaskId> tasksOfEvent in resumeScheduler.GetResumeTasksBefore(time).GroupBy(x => x.Value, x => x.Key))
			{
				HashSet<ScheduledTaskId> referenced = _repository.NodePhases.GetPendingResumes(tasksOfEvent.Key)
					.Select(x => x.ResumeSchedulerReference)
					.ToHashSet();

				foreach (ScheduledTaskId taskId in tasksOfEvent.Where(x => !referenced.Contains(x)))
				{
					resumeScheduler.DeleteResumeTask(taskId);
				}
			}
		}

		// A resumed execution clears the resume when it starts, so a resume that is still awaited this long after its time never ran.
		private void FailMissedResumes(DateTimeOffset time, OrchestrationResumeScheduler resumeScheduler, IMediaOpsPlanHelper planHelper)
		{
			foreach (IGrouping<Guid, OrchestrationNodePhases> missedOfEvent in _repository.NodePhases.GetOverdueResumes(time).GroupBy(x => x.EventReference.ID))
			{
				var failures = new List<string>();

				foreach (OrchestrationNodePhases missed in missedOfEvent)
				{
					failures.Add($"The orchestration of node '{missed.NodeId}' was not resumed: the resume that was due at {missed.ResumeTime:u} did not run.");
					resumeScheduler.DeleteResumeTask(missed.ResumeSchedulerReference);

					missed.ResumeSchedulerReference = null;
					missed.ResumePhaseIndex = null;
					missed.ResumeTime = null;
					missed.WaitStarted = null;
					_repository.NodePhases.CreateOrUpdate(missed);
				}

				OrchestrationEvent orchestrationEvent = _repository.Read(missedOfEvent.Key);
				if (orchestrationEvent?.EventState != EventState.Configuring)
				{
					continue;
				}

				orchestrationEvent.InternalSetState(EventState.Failed);
				foreach (string failure in failures)
				{
					orchestrationEvent.AppendFailureInfo(failure);
				}

				if (orchestrationEvent.ActualStartTime != null)
				{
					orchestrationEvent.OrchestrationDuration = DateTimeOffset.UtcNow - orchestrationEvent.ActualStartTime;
				}

				orchestrationEvent.SendPlanJobStateUpdate(planHelper);
				_repository.CreateOrUpdate(orchestrationEvent);
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