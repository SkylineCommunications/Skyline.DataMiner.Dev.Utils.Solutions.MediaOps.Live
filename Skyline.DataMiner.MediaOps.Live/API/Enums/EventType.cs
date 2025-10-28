namespace Skyline.DataMiner.MediaOps.Live.API.Enums
{
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	/// <summary>
	/// Defines the type of orchestration event.
	/// </summary>
	// Keep in sync with SlcOrchestrationIds.Enums.EventType
	public enum EventType
	{
		/// <summary>
		/// Other type of event.
		/// </summary>
		Other = SlcOrchestrationIds.Enums.EventType.Other,

		/// <summary>
		/// Pre-roll start event.
		/// </summary>
		PrerollStart = SlcOrchestrationIds.Enums.EventType.Prerollstart,

		/// <summary>
		/// Pre-roll stop event.
		/// </summary>
		PrerollStop = SlcOrchestrationIds.Enums.EventType.Prerollstop,

		/// <summary>
		/// Post-roll start event.
		/// </summary>
		PostrollStart = SlcOrchestrationIds.Enums.EventType.Postrollstart,

		/// <summary>
		/// Post-roll stop event.
		/// </summary>
		PostrollStop = SlcOrchestrationIds.Enums.EventType.Postrollstop,

		/// <summary>
		/// Event start.
		/// </summary>
		Start = SlcOrchestrationIds.Enums.EventType.Start,

		/// <summary>
		/// Event stop.
		/// </summary>
		Stop = SlcOrchestrationIds.Enums.EventType.Stop,
	}
}
