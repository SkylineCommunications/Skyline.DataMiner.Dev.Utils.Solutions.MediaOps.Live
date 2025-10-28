namespace Skyline.DataMiner.MediaOps.Live.API.Enums
{
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	/// <summary>
	/// Defines the types of orchestration events.
	/// </summary>
	// Keep in sync with SlcOrchestrationIds.Enums.EventType
	public enum EventType
	{
		/// <summary>
		/// Other event type.
		/// </summary>
		Other = SlcOrchestrationIds.Enums.EventType.Other,

		/// <summary>
		/// Preroll start event.
		/// </summary>
		PrerollStart = SlcOrchestrationIds.Enums.EventType.Prerollstart,

		/// <summary>
		/// Preroll stop event.
		/// </summary>
		PrerollStop = SlcOrchestrationIds.Enums.EventType.Prerollstop,

		/// <summary>
		/// Postroll start event.
		/// </summary>
		PostrollStart = SlcOrchestrationIds.Enums.EventType.Postrollstart,

		/// <summary>
		/// Postroll stop event.
		/// </summary>
		PostrollStop = SlcOrchestrationIds.Enums.EventType.Postrollstop,

		/// <summary>
		/// Start event.
		/// </summary>
		Start = SlcOrchestrationIds.Enums.EventType.Start,

		/// <summary>
		/// Stop event.
		/// </summary>
		Stop = SlcOrchestrationIds.Enums.EventType.Stop,
	}
}
