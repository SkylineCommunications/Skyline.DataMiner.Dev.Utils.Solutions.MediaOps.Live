namespace Skyline.DataMiner.MediaOps.Live.API.Enums
{
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	/// <summary>
	/// Defines the possible states of an orchestration event.
	/// </summary>
	// Keep in sync with SlcOrchestrationIds.Enums.EventState
	public enum EventState
	{
		/// <summary>
		/// Event is confirmed.
		/// </summary>
		Confirmed = SlcOrchestrationIds.Enums.EventState.Confirmed,

		/// <summary>
		/// Event is being configured.
		/// </summary>
		Configuring = SlcOrchestrationIds.Enums.EventState.Configuring,

		/// <summary>
		/// Event was cancelled.
		/// </summary>
		Cancelled = SlcOrchestrationIds.Enums.EventState.Cancelled,

		/// <summary>
		/// Event completed successfully.
		/// </summary>
		Completed = SlcOrchestrationIds.Enums.EventState.Completed,

		/// <summary>
		/// Event failed.
		/// </summary>
		Failed = SlcOrchestrationIds.Enums.EventState.Failed,

		/// <summary>
		/// Event is in draft state.
		/// </summary>
		Draft = SlcOrchestrationIds.Enums.EventState.Draft,
	}
}
