namespace Skyline.DataMiner.MediaOps.Live.API.Enums
{
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	// Keep in sync with SlcOrchestrationIds.Enums.EventType
	public enum EventType
	{
		Other = SlcOrchestrationIds.Enums.EventType.Other,
		PrerollStart = SlcOrchestrationIds.Enums.EventType.Prerollstart,
		PrerollStop = SlcOrchestrationIds.Enums.EventType.Prerollstop,
		PostrollStart = SlcOrchestrationIds.Enums.EventType.Postrollstart,
		PostrollStop = SlcOrchestrationIds.Enums.EventType.Postrollstop,
		Start = SlcOrchestrationIds.Enums.EventType.Start,
		Stop = SlcOrchestrationIds.Enums.EventType.Stop,
	}
}
