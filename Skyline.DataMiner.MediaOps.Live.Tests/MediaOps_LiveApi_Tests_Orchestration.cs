namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Orchestration
	{
		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_GetAllJobs()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			Guid newJobGuid = Guid.NewGuid();

			var event1 = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var event2 = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var job = api.Orchestration.GetOrCreateNewOrchestrationJob(newJobGuid.ToString());
			job.OrchestrationEvents.Add(event1);
			job.OrchestrationEvents.Add(event2);
			api.Orchestration.SaveOrchestrationJob(job);

			var allJobs = api.Orchestration.GetAllJobs();
			Assert.HasCount(2, allJobs);

			Assert.HasCount(2, allJobs.FirstOrDefault(job => job.JobId == newJobGuid.ToString()).OrchestrationEvents);

			Assert.HasCount(10, allJobs.FirstOrDefault(job => job.JobId != newJobGuid.ToString()).OrchestrationEvents);
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_GetOrCreateNewOrchestrationJobs()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			// Create first Job
			Guid newJobGuid1 = Guid.NewGuid();

			var event1_1 = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var event1_2 = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var job1 = api.Orchestration.GetOrCreateNewOrchestrationJob(newJobGuid1.ToString());
			job1.OrchestrationEvents.Add(event1_1);
			job1.OrchestrationEvents.Add(event1_2);
			api.Orchestration.SaveOrchestrationJob(job1);

			// Create second job
			Guid newJobGuid2 = Guid.NewGuid();

			var event2_1 = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var job2 = api.Orchestration.GetOrCreateNewOrchestrationJob(newJobGuid2.ToString());
			job2.OrchestrationEvents.Add(event2_1);
			api.Orchestration.SaveOrchestrationJob(job2);

			var allJobs = api.Orchestration.GetOrCreateNewOrchestrationJobs(new List<string> { newJobGuid1.ToString(), newJobGuid2.ToString() });
			Assert.HasCount(2, allJobs);

			Assert.HasCount(2, allJobs.FirstOrDefault(job => job.JobId == newJobGuid1.ToString()).OrchestrationEvents);

			Assert.HasCount(1, allJobs.FirstOrDefault(job => job.JobId == newJobGuid2.ToString()).OrchestrationEvents);
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_GetOrCreateNewOrchestrationJobConfigurations()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			// Create first Job
			Guid newJobGuid1 = Guid.NewGuid();

			var event1_1 = new OrchestrationEventConfiguration()
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			event1_1.Configuration.NodeConfigurations.Add(new NodeConfiguration { NodeId = "1" });

			var event1_2 = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var job1 = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(newJobGuid1.ToString());
			job1.OrchestrationEvents.Add(event1_1);
			job1.OrchestrationEvents.Add(event1_2);
			api.Orchestration.SaveOrchestrationJobConfiguration(job1);

			// Create second job
			Guid newJobGuid2 = Guid.NewGuid();

			var event2_1 = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			event2_1.Configuration.NodeConfigurations.Add(new NodeConfiguration { NodeId = "1"});

			var job2 = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(newJobGuid2.ToString());
			job2.OrchestrationEvents.Add(event2_1);
			api.Orchestration.SaveOrchestrationJobConfiguration(job2);

			var retrievedJobs = api.Orchestration.GetOrCreateNewOrchestrationJobConfigurations(new List<string> { newJobGuid1.ToString(), newJobGuid2.ToString() });
			Assert.HasCount(2, retrievedJobs);

			Assert.HasCount(2, retrievedJobs.FirstOrDefault(job => job.JobId == newJobGuid1.ToString()).OrchestrationEvents);
			Assert.HasCount(1, retrievedJobs.FirstOrDefault(job => job.JobId == newJobGuid2.ToString()).OrchestrationEvents);

			var configurationHelper = new ConfigurationRepository(api);
			Assert.AreEqual(12, configurationHelper.CountAll());
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_DeleteJobs()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			Guid newJobGuid = Guid.NewGuid();

			var event1 = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var event2 = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var job = api.Orchestration.GetOrCreateNewOrchestrationJob(newJobGuid.ToString());
			job.OrchestrationEvents.Add(event1);
			job.OrchestrationEvents.Add(event2);
			api.Orchestration.SaveOrchestrationJob(job);

			var allJobs = api.Orchestration.GetAllJobs();
			api.Orchestration.DeleteJobs(allJobs);

			var eventHelper = new OrchestrationEventRepository(api);
			Assert.AreEqual(0, eventHelper.CountAll());

			var jobInfoHelper = new JobInfoRepository(api);
			Assert.AreEqual(0, jobInfoHelper.CountAll());
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_DeleteJobConfigurations()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			Guid newJobGuid = Guid.NewGuid();

			var event1 = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			event1.Configuration.NodeConfigurations.Add(new NodeConfiguration { NodeId = "1" });

			var event2 = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(newJobGuid.ToString());
			job.OrchestrationEvents.Add(event1);
			job.OrchestrationEvents.Add(event2);
			api.Orchestration.SaveOrchestrationJobConfiguration(job);

			var allJobs = api.Orchestration.GetAllJobConfigurations();
			api.Orchestration.DeleteJobConfigurations(allJobs);

			var eventHelper = new OrchestrationEventRepository(api);
			Assert.AreEqual(0, eventHelper.CountAll());

			var configurationHelper = new ConfigurationRepository(api);
			Assert.AreEqual(0, configurationHelper.CountAll());

			var jobInfoHelper = new JobInfoRepository(api);
			Assert.AreEqual(0, jobInfoHelper.CountAll());
		}
	}
}
