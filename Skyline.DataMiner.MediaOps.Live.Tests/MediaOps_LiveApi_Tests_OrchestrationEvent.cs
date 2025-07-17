namespace Skyline.DataMiner.MediaOps.Live.Tests;

using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

[TestClass]
public class MediaOps_LiveApi_Tests_OrchestrationEvent
{
	[TestMethod]
	public void MediaOps_Live_Api_Tests_OrchestrationEvent_BlockInternalEventStates()
	{
		OrchestrationEvent ev = new()
		{
			EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
			EventType = SlcOrchestrationIds.Enums.EventType.Other,
			Name = "Test Event",
		};

		Action setConfiguring = () => ev.EventState = SlcOrchestrationIds.Enums.EventState.Configuring;
		Action setFailed = () => ev.EventState = SlcOrchestrationIds.Enums.EventState.Failed;
		Action setCompleted= () => ev.EventState = SlcOrchestrationIds.Enums.EventState.Completed;

		Assert.Throws<ArgumentException>(setConfiguring, "Event state Configuring can not be applied.");
		Assert.Throws<ArgumentException>(setFailed, "Event state Failed can not be applied.");
		Assert.Throws<ArgumentException>(setCompleted, "Event state Completed can not be applied.");
	}
}