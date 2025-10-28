namespace Skyline.DataMiner.MediaOps.Live.API.Enums
{
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	/// <summary>
	/// Defines the state of an orchestration event.
	/// </summary>
	// Keep in sync with SlcOrchestrationIds.Enums.EventState
	public enum EventState
	{
		/// <summary>
		/// The event is confirmed and ready to execute.
		/// </summary>
		Confirmed = SlcOrchestrationIds.Enums.EventState.Confirmed,

		/// <summary>
		/// The event is currently being configured.
		/// </summary>
		Configuring = SlcOrchestrationIds.Enums.EventState.Configuring,

		/// <summary>
		/// The event has been cancelled.
		/// </summary>
		Cancelled = SlcOrchestrationIds.Enums.EventState.Cancelled,

		/// <summary>
		/// The event has completed successfully.
		/// </summary>
		Completed = SlcOrchestrationIds.Enums.EventState.Completed,

		/// <summary>
		/// The event has failed.
		/// </summary>
		Failed = SlcOrchestrationIds.Enums.EventState.Failed,

		/// <summary>
		/// The event is in draft state.
		/// </summary>
		Draft = SlcOrchestrationIds.Enums.EventState.Draft,
	}
}
