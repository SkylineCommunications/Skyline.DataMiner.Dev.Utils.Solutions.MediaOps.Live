namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;

	// Keep in sync with SlcOrchestrationIds.Enums.EventState
	public enum EventState
	{
		Confirmed = SlcOrchestrationIds.Enums.EventState.Confirmed,
		Configuring = SlcOrchestrationIds.Enums.EventState.Configuring,
		Cancelled = SlcOrchestrationIds.Enums.EventState.Cancelled,
		Completed = SlcOrchestrationIds.Enums.EventState.Completed,
		Failed = SlcOrchestrationIds.Enums.EventState.Failed,
		Draft = SlcOrchestrationIds.Enums.EventState.Draft,
	}
}
