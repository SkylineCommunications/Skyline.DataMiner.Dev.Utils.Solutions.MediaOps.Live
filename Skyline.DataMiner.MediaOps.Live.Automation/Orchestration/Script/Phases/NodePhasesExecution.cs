namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling;

	/// <summary>
	/// Runs the phases of a node for an event, ending the execution and scheduling a resume when a wait is long.
	/// </summary>
	internal static class NodePhasesExecution
	{
		internal static PhaseRunResult Run(
			IReadOnlyCollection<OrchestrationPhase> phases,
			OrchestrationPhaseContext context,
			NodePhasesStatusStore store,
			OrchestrationResumeScheduler resumeScheduler,
			Guid eventId,
			bool resume,
			IEventNodes eventNodes,
			Action<TimeSpan> sleep = null,
			Func<DateTimeOffset> now = null)
		{
			// A changed script, or a trigger that is not a resume, walks all phases from the first one.
			ResumePoint resumePoint = resume ? store.GetResumePoint() : null;

			// Any resume that is still scheduled is replaced by this execution.
			resumeScheduler.DeleteResumeTask(store.PendingResumeTask);
			store.ClearSuspension();

			PhaseRunResult result = new PhaseRunner(phases, context, store, sleep, now, canSuspend: true, eventNodes: eventNodes).Run(resumePoint);
			if (!result.IsSuspended)
			{
				return result;
			}

			ScheduledTaskId resumeTask;

			try
			{
				resumeTask = resumeScheduler.ScheduleResume(eventId, result.ResumeTime.Value);
			}
			catch (Exception e) when (!PhaseRunner.IsAbort(e))
			{
				string failure = $"Unable to schedule the resume of the orchestration: {e.Message}";
				if (result.WaitingPhaseKey != null)
				{
					store.Record(result.WaitingPhaseKey, PhaseStatus.Failed(failure), null);
				}

				return PhaseRunResult.Failure(failure);
			}

			store.SaveSuspension(result.SuspendedAt, result.ResumeTime.Value, resumeTask);

			return result;
		}
	}
}
