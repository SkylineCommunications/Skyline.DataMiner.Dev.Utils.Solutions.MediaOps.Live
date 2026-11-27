namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;

	/// <summary>
	/// The status of an orchestration phase: a state, plus an optional message for the operator explaining why the phase is in that state.
	/// </summary>
	public sealed class PhaseStatus
	{
		private PhaseStatus(PhaseState state, string message)
		{
			State = state;
			Message = String.IsNullOrWhiteSpace(message) ? null : message;
		}

		/// <summary>
		/// Gets the state of the phase. The state determines what the orchestration does next.
		/// </summary>
		public PhaseState State { get; }

		/// <summary>
		/// Gets the message that explains the state, or <see langword="null"/> when there is none. The message never influences the orchestration.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Creates a status indicating the phase still has to be executed.
		/// </summary>
		/// <param name="message">Optional message explaining the status.</param>
		/// <returns>The status.</returns>
		public static PhaseStatus NotStarted(string message = null) => new PhaseStatus(PhaseState.NotStarted, message);

		/// <summary>
		/// Creates a status indicating the phase was executed, but its result is not available yet.
		/// </summary>
		/// <param name="message">Optional message explaining the status.</param>
		/// <returns>The status.</returns>
		public static PhaseStatus InProgress(string message = null) => new PhaseStatus(PhaseState.InProgress, message);

		/// <summary>
		/// Creates a status indicating the phase is done.
		/// </summary>
		/// <param name="message">Optional message explaining the status.</param>
		/// <returns>The status.</returns>
		public static PhaseStatus Completed(string message = null) => new PhaseStatus(PhaseState.Completed, message);

		/// <summary>
		/// Creates a status indicating the phase failed. The reason is shown to the operator, on the phase and in the failure info of the event.
		/// </summary>
		/// <param name="reason">The reason why the phase failed.</param>
		/// <returns>The status.</returns>
		public static PhaseStatus Failed(string reason = null) => new PhaseStatus(PhaseState.Failed, reason);

		/// <summary>
		/// Gets the reason why the phase failed, or <see langword="null"/> when it did not fail.
		/// </summary>
		public string FailureReason => State == PhaseState.Failed ? Message : null;

		/// <inheritdoc/>
		public override string ToString()
		{
			return Message == null ? State.ToString() : $"{State}: {Message}";
		}
	}
}
