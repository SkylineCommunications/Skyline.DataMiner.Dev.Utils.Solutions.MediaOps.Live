namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;

	// Keep in sync with SlcOrchestrationIds.Enums.PhaseState
	/// <summary>
	/// The state of an orchestration phase.
	/// </summary>
	public enum PhaseState
	{
		/// <summary>The phase still has to be executed.</summary>
		NotStarted = SlcOrchestrationIds.Enums.PhaseState.NotStarted,

		/// <summary>The phase was executed, but its result is not available yet.</summary>
		InProgress = SlcOrchestrationIds.Enums.PhaseState.InProgress,

		/// <summary>The phase is done; it is skipped on a retrigger.</summary>
		Completed = SlcOrchestrationIds.Enums.PhaseState.Completed,

		/// <summary>The phase failed; the following phases are not executed.</summary>
		Failed = SlcOrchestrationIds.Enums.PhaseState.Failed,
	}
}
