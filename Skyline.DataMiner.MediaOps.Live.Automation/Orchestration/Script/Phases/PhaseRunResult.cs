namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;

	internal sealed class PhaseRunResult
	{
		private PhaseRunResult(string failureMessage, ResumePoint suspendedAt = null, DateTimeOffset? resumeTime = null, string waitingPhaseKey = null)
		{
			FailureMessage = failureMessage;
			SuspendedAt = suspendedAt;
			ResumeTime = resumeTime;
			WaitingPhaseKey = waitingPhaseKey;
		}

		internal bool IsSuccess => FailureMessage == null;

		internal string FailureMessage { get; }

		/// <summary>
		/// Gets whether the run ended in a wait phase that must be resumed at <see cref="ResumeTime"/>.
		/// </summary>
		internal bool IsSuspended => SuspendedAt != null;

		internal ResumePoint SuspendedAt { get; }

		internal DateTimeOffset? ResumeTime { get; }

		/// <summary>
		/// Gets the key of the phase that reports the wait, or <see langword="null"/> when the wait is the last phase.
		/// </summary>
		internal string WaitingPhaseKey { get; }

		internal static PhaseRunResult Success() => new PhaseRunResult(null);

		internal static PhaseRunResult Failure(string message) => new PhaseRunResult(message);

		internal static PhaseRunResult Suspended(ResumePoint suspendedAt, DateTimeOffset resumeTime, string waitingPhaseKey)
		{
			return new PhaseRunResult(null, suspendedAt ?? throw new ArgumentNullException(nameof(suspendedAt)), resumeTime, waitingPhaseKey);
		}
	}
}
