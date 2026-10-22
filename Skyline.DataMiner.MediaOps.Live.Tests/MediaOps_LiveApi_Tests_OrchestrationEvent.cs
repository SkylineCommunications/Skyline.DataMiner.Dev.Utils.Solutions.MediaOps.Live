namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests;

using Skyline.DataMiner.Net.Messages.SLDataGateway;
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

	[TestMethod]
	public void MediaOps_Live_Api_Tests_OrchestrationEvent_ReadEventsPaged()
	{
		MediaOpsLiveApi api = new MediaOpsLiveApiMock();

		const string eventName = "Paged Test Event";

		var job = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
		for (int i = 0; i < 5; i++)
		{
			job.OrchestrationEvents.Add(new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventType = EventType.Other,
				Name = eventName,
			});
		}

		api.Orchestration.SaveOrchestrationJob(job);

		var filter = OrchestrationEventExposers.Name.Equal(eventName);

		var pages = api.Orchestration.ReadEventsPaged(filter, pageSize: 2).ToList();

		Assert.HasCount(3, pages);
		Assert.IsTrue(pages.Take(2).All(p => p.HasNextPage));
		Assert.IsFalse(pages.Last().HasNextPage);

		var pagedIds = pages.SelectMany(p => p).Select(e => e.ID).OrderBy(id => id).ToList();
		var expectedIds = api.Orchestration.ReadEvents(filter).Select(e => e.ID).OrderBy(id => id).ToList();

		Assert.HasCount(5, pagedIds);
		CollectionAssert.AreEqual(expectedIds, pagedIds);
	}

	[TestMethod]
	public void MediaOps_Live_Api_Tests_OrchestrationEvent_ReadEventsPaged_InvalidArguments()
	{
		MediaOpsLiveApi api = new MediaOpsLiveApiMock();

		Assert.Throws<ArgumentNullException>(() => api.Orchestration.ReadEventsPaged(null));
		Assert.Throws<ArgumentOutOfRangeException>(() => api.Orchestration.ReadEventsPaged(OrchestrationEventExposers.Name.Equal("x"), 0));
	}
}