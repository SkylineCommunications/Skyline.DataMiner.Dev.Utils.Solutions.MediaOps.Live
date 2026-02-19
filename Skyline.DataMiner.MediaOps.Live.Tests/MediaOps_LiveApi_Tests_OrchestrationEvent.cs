namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests;

using Skyline.DataMiner.Solutions.MediaOps.Live.API;
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

[TestClass]
public class MediaOps_LiveApi_Tests_OrchestrationEvent
{
	[TestMethod]
	public void MediaOps_Live_Api_Tests_OrchestrationEvent_BlockInternalEventStates()
	{
		OrchestrationEvent ev = new()
		{
			EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
			EventType = EventType.Other,
			Name = "Test Event",
		};

		Action setConfiguring = () => ev.EventState = EventState.Configuring;
		Action setFailed = () => ev.EventState = EventState.Failed;
		Action setCompleted= () => ev.EventState = EventState.Completed;

		Assert.Throws<ArgumentException>(setConfiguring, "Event state Configuring can not be applied.");
		Assert.Throws<ArgumentException>(setFailed, "Event state Failed can not be applied.");
		Assert.Throws<ArgumentException>(setCompleted, "Event state Completed can not be applied.");
	}

	[TestMethod]
	public void MediaOps_Live_Api_Tests_OrchestrationEvent_EventToEventConfiguration()
	{
		MediaOpsLiveApi api = new MediaOpsLiveApiMock();

		var job = api.Orchestration.GetOrCreateNewOrchestrationJob("dd2cd5f2-ee7d-42b8-9b96-1e562d472b63");

		var ev = job.OrchestrationEvents.First();
		var convertedEvents = api.Orchestration.GetEventsAsEventConfigurations(new List<OrchestrationEvent> { ev });
		var eventConfiguration = convertedEvents[ev.ID];

		Assert.AreEqual(ev.ConfigurationReference, eventConfiguration.Configuration.ID);
	}
}