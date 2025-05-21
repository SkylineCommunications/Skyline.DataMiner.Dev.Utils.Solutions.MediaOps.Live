namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationJob
	{
		private static readonly MediaOpsLiveApi _api = new MediaOpsLiveApiMock();

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_CheckDeleteBeforeUpdateWithoutNodes()
		{
			var events = NoNodes_CreateEventInstance(2);

			var job = _api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			job.OrchestrationEvents.AddRange(events);

			var guids = job.OrchestrationEvents.Select(ev => ev.ID).ToList();
			var toRemove = guids[0];
			var eventToRemove = job.OrchestrationEvents.FirstOrDefault(ev => ev.ID == toRemove);

			// Check events created is 2
			Assert.AreEqual(2, job.OrchestrationEvents.Count);

			// Check events is 1 after removal
			job.OrchestrationEvents.Remove(eventToRemove);
			Assert.AreEqual(1, job.OrchestrationEvents.Count);

			// Check 1 event identified as deleted
			// Assert.AreEqual(job.RemovedIds.Count(), 1);
			// Assert.AreEqual(job.RemovedIds.FirstOrDefault(), toRemove);
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_CheckDeleteBeforeUpdateWithNodes()
		{
			var events = WithNodes_CreateEventConfigurationInstances(2, 5);

			var job = _api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.AddRange(events);

			var guids = job.OrchestrationEvents.Select(ev => ev.ID).ToList();
			var toRemove = guids[0];
			var eventToRemove = job.OrchestrationEvents.FirstOrDefault(ev => ev.ID == toRemove);

			// Check events created is 2
			Assert.AreEqual(2, job.OrchestrationEvents.Count);

			// Check events is 1 after removal
			job.OrchestrationEvents.Remove(eventToRemove);
			Assert.AreEqual(1, job.OrchestrationEvents.Count);

			// Check 1 event identified as deleted
			// Assert.AreEqual(job.RemovedIds.Count(), 1);
			// Assert.AreEqual(job.RemovedIds.FirstOrDefault(), toRemove);
		}

		/*[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationJob_CheckApi()
		{
			string testGuid = "dd2cd5f2-ee7d-42b8-9b96-1e562d472b63";

			var domEvents = _api.Orchestration.GetEventsByJobReference(testGuid);
			OrchestrationJobConfiguration job = _api.Orchestration.GetOrchestrationJobConfiguration(testGuid);

			Assert.AreEqual(10, job.OrchestrationEvents.Count());
			Assert.AreEqual(10, domEvents.Count());

			job.OrchestrationEvents.RemoveAt(0);
			_api.Orchestration.CreateOrUpdateOrchestrationJobConfiguration(job);

			domEvents = _api.Orchestration.GetEventsByJobReference(testGuid);
			Assert.AreEqual(9, domEvents.Count());

			//var nameEvents = _api.Orchestration.Query().Where(x => x.Name == "Test Event 1");
			//Console.WriteLine(nameEvents.First().JobReference);

			//Assert.AreEqual(10, nameEvents.Count());
		}*/

		private IEnumerable<OrchestrationEventConfiguration> WithNodes_CreateEventConfigurationInstances(int count, int nodes)
		{
			List<Connection> connections = [];
			List<NodeConfiguration> nodeConfigs = [];
			List<LevelMapping> levelMapping =
			[
				new()
				{
					Destination = new Level
					{
						Name = "Destination",
						Number = 1,
					},
					Source = new Level
					{
						Name = "Source",
						Number = 1,
					},
				},
			];

			List<OrchestrationScriptArgument> scriptArguments =
			[
				new(OrchestrationScriptArgumentType.Element, "Name", "Value"),
				new(OrchestrationScriptArgumentType.Parameter, "Name", "Value"),
				new(OrchestrationScriptArgumentType.Parameter, "Name", "Value"),
			];

			for (int i = 1; i <= nodes; i++)
			{
				connections.Add(new Connection
				{
					DestinationNodeId = "1",
					DestinationVsg = Guid.NewGuid(),
					SourceVsg = Guid.NewGuid(),
					SourceNodeId = "1",
					LevelMappings = levelMapping,
				});

				nodeConfigs.Add(new NodeConfiguration
				{
					NodeId = "1",
					NodeLabel = "Node Label",
					OrchestrationScriptName = "OrchestrationScript",
					OrchestrationScriptArguments = scriptArguments,
				});
			}

			List<OrchestrationEventConfiguration> orchestrationEventConfigurations = [];

			for (int i = 1; i <= count; i++)
			{
				orchestrationEventConfigurations.Add(new OrchestrationEventConfiguration
				{
					Name = $"Test Event {i}",
					EventTime = DateTime.UtcNow,
					EventType = SlcOrchestrationIds.Enums.EventType.Other,
					EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
					GlobalOrchestrationScript = "Test Script",
					GlobalOrchestrationScriptArguments = scriptArguments,
					Configuration =
					{
						Connections = connections,
						NodeConfigurations = nodeConfigs,
					},
				});
			}

			return orchestrationEventConfigurations;
		}

		private IEnumerable<OrchestrationEvent> NoNodes_CreateEventInstance(int count)
		{
			List<OrchestrationEvent> events = [];
			for (int i = 1; i <= count; i++)
			{
				events.Add(new OrchestrationEvent
				{
					Name = $"Test Event {i}",
					EventTime = DateTime.UtcNow,
					EventType = SlcOrchestrationIds.Enums.EventType.Other,
					EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
				});
			}

			return events;
		}
	}
}
