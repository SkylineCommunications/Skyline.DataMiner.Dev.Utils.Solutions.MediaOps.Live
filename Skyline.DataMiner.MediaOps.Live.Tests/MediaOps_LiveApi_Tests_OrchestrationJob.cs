namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	using ParameterValue = Skyline.DataMiner.Net.Profiles.ParameterValue;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationJob
	{
		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_CheckDeleteBeforeUpdate()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var job = api.Orchestration.GetOrCreateNewOrchestrationJob("dd2cd5f2-ee7d-42b8-9b96-1e562d472b63");

			var guids = job.OrchestrationEvents.Select(ev => ev.ID).ToList();
			var toRemove = guids[0];
			var eventToRemove = job.OrchestrationEvents.FirstOrDefault(ev => ev.ID == toRemove);

			// Check events created is 2
			Assert.HasCount(10, job.OrchestrationEvents);

			// Check events is 1 after removal
			job.OrchestrationEvents.Remove(eventToRemove);
			Assert.HasCount(9, job.OrchestrationEvents);

			// Check 1 event identified as deleted
			Assert.AreEqual(1, job.RemovedIds.Count());
			Assert.AreEqual(toRemove, job.RemovedIds.FirstOrDefault());
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_SaveEventsNoConfiguration()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

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

			var job = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(event1);
			job.OrchestrationEvents.Add(event2);
			api.Orchestration.SaveOrchestrationJob(job);

			var orchestrationEventHelper = new OrchestrationEventRepository(api);
			Assert.AreEqual(12, orchestrationEventHelper.CountAll());

			var configurationHelper = new ConfigurationRepository(api);
			Assert.AreEqual(10, configurationHelper.CountAll());
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_SaveEventsWithEmptyConfiguration()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var event1 = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var event2 = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(event1);
			job.OrchestrationEvents.Add(event2);
			api.Orchestration.SaveOrchestrationJobConfiguration(job);

			var orchestrationEventHelper = new OrchestrationEventRepository(api);
			Assert.AreEqual(12, orchestrationEventHelper.CountAll());

			var configurationHelper = new ConfigurationRepository(api);
			Assert.AreEqual(10, configurationHelper.CountAll());
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_ValidateMultipleStartEvents()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var event1 = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.PrerollStart,
				Name = "Test Event Confirmed",
			};

			var event2 = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(2),
				EventState = EventState.Confirmed,
				EventType = EventType.Start,
				Name = "Test Event Confirmed",
			};

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(event1);
			job.OrchestrationEvents.Add(event2);

			Assert.Throws<InvalidOperationException>(
				() => api.Orchestration.SaveOrchestrationJobConfiguration(job),
				"Job can have only a single starting event (Start, PrerollStart) and a single ending event (Stop, PostrollStop).");
			var orchestrationEventHelper = new OrchestrationEventRepository(api);

			Assert.AreEqual(10, orchestrationEventHelper.CountAll());

			var configurationHelper = new ConfigurationRepository(api);
			Assert.AreEqual(10, configurationHelper.CountAll());
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_ValidateStopBeforeStart()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var event1 = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Stop,
				Name = "Test Event Confirmed",
			};

			var event2 = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(2),
				EventState = EventState.Confirmed,
				EventType = EventType.Start,
				Name = "Test Event Confirmed",
			};

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(event1);
			job.OrchestrationEvents.Add(event2);

			Assert.Throws<InvalidOperationException>(
				() => api.Orchestration.SaveOrchestrationJobConfiguration(job),
				"Event of type Stop can not be scheduled before an event of type Start");

			var orchestrationEventHelper = new OrchestrationEventRepository(api);
			Assert.AreEqual(10, orchestrationEventHelper.CountAll());

			var configurationHelper = new ConfigurationRepository(api);
			Assert.AreEqual(10, configurationHelper.CountAll());
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_ValidateStartWithoutStop()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var event1 = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Start,
				Name = "Test Event Confirmed",
			};

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(event1);

			Assert.Throws<InvalidOperationException>(
				() => api.Orchestration.SaveOrchestrationJobConfiguration(job),
				"Job must have a starting event (Start, PrerollStart) and an ending event (Stop, PostrollStop).");

			var orchestrationEventHelper = new OrchestrationEventRepository(api);
			Assert.AreEqual(10, orchestrationEventHelper.CountAll());

			var configurationHelper = new ConfigurationRepository(api);
			Assert.AreEqual(10, configurationHelper.CountAll());
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_ValidateProfileValueType_StringToNumericParameter()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var eventConfig = CreateEventWithOrchestrationScript(new List<OrchestrationProfileValue>
			{
				new OrchestrationProfileValue
				{
					Name = "IndividualProfileParam_Int",
					Value = new ParameterValue { Type = ParameterValue.ValueType.String, StringValue = "invalid" },
				},
				new OrchestrationProfileValue
				{
					Name = "IndividualProfileParam_String",
					Value = new ParameterValue { Type = ParameterValue.ValueType.String, StringValue = "valid" },
				},
				new OrchestrationProfileValue
				{
					Name = "DefinitionProfileParam_Int",
					Value = new ParameterValue { Type = ParameterValue.ValueType.Double, DoubleValue = 42 },
				},
				new OrchestrationProfileValue
				{
					Name = "DefinitionProfileParam_String",
					Value = new ParameterValue { Type = ParameterValue.ValueType.String, StringValue = "valid" },
				},
			});

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(eventConfig);

			var ex = Assert.Throws<InvalidOperationException>(
				() => api.Orchestration.SaveOrchestrationJobConfiguration(job));

			Assert.IsTrue(ex.Message.Contains("IndividualProfileParam_Int"));
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_ValidateProfileValueType_NumericToStringParameter()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var eventConfig = CreateEventWithOrchestrationScript(new List<OrchestrationProfileValue>
			{
				new OrchestrationProfileValue
				{
					Name = "IndividualProfileParam_Int",
					Value = new ParameterValue { Type = ParameterValue.ValueType.Double, DoubleValue = 42 },
				},
				new OrchestrationProfileValue
				{
					Name = "IndividualProfileParam_String",
					Value = new ParameterValue { Type = ParameterValue.ValueType.Double, DoubleValue = 123 },
				},
				new OrchestrationProfileValue
				{
					Name = "DefinitionProfileParam_Int",
					Value = new ParameterValue { Type = ParameterValue.ValueType.Double, DoubleValue = 42 },
				},
				new OrchestrationProfileValue
				{
					Name = "DefinitionProfileParam_String",
					Value = new ParameterValue { Type = ParameterValue.ValueType.String, StringValue = "valid" },
				},
			});

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(eventConfig);

			var ex = Assert.Throws<InvalidOperationException>(
				() => api.Orchestration.SaveOrchestrationJobConfiguration(job));

			Assert.IsTrue(ex.Message.Contains("IndividualProfileParam_String"));
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_ValidateProfileValueType_CorrectTypes()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var eventConfig = CreateEventWithOrchestrationScript(new List<OrchestrationProfileValue>
			{
				new OrchestrationProfileValue
				{
					Name = "IndividualProfileParam_Int",
					Value = new ParameterValue { Type = ParameterValue.ValueType.Double, DoubleValue = 42 },
				},
				new OrchestrationProfileValue
				{
					Name = "IndividualProfileParam_String",
					Value = new ParameterValue { Type = ParameterValue.ValueType.String, StringValue = "valid" },
				},
				new OrchestrationProfileValue
				{
					Name = "DefinitionProfileParam_Int",
					Value = new ParameterValue { Type = ParameterValue.ValueType.Double, DoubleValue = 100 },
				},
				new OrchestrationProfileValue
				{
					Name = "DefinitionProfileParam_String",
					Value = new ParameterValue { Type = ParameterValue.ValueType.String, StringValue = "valid" },
				},
			});

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(eventConfig);

			// Should not throw
			api.Orchestration.SaveOrchestrationJobConfiguration(job);

			// Check that events and configuration are saved
			var orchestrationEventHelper = new OrchestrationEventRepository(api);
			Assert.AreEqual(11, orchestrationEventHelper.CountAll());

			var configurationHelper = new ConfigurationRepository(api);
			Assert.AreEqual(10, configurationHelper.CountAll());
		}

		private static OrchestrationEventConfiguration CreateEventWithOrchestrationScript(List<OrchestrationProfileValue> profileValues)
		{
			var eventConfig = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event",
				GlobalOrchestrationScript = "OrchestrationScript",
			};

			eventConfig.GlobalOrchestrationScriptArguments = new List<OrchestrationScriptArgument>
			{
				new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, "InputParam", "value"),
				new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Element, "InputDummy", "123/1000"),
			};

			eventConfig.Profile = new OrchestrationProfile
			{
				Values = new List<OrchestrationProfileValue>(profileValues),
			};

			return eventConfig;
		}
	}
}
