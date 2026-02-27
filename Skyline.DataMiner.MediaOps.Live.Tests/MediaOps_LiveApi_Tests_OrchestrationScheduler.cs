namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Plan;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting.Simulation;

	[TestClass]
	public class MediaOps_LiveApi_Tests_OrchestrationScheduler
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ConfirmedEventIsScheduled()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var ev = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			var utcScheduledTime = simulation.Dms.GetAllDmsSchedulerTasks().First().StartTime.ToUniversalTime();

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.Contains(ev.ID, simulation.Dms.GetAllDmsSchedulerTasks().First().GetOrchestrationSchedulingInputList());
			Assert.AreEqual(ev.EventTime, utcScheduledTime);

			Assert.Contains(orchestrationJob.OrchestrationEvents.First().SchedulerReference.DmaId, simulation.Dms.Agents.Keys);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_DraftEventIsNotScheduled()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Draft,
				EventType = EventType.Other,
				Name = "Test Event Draft",
			});
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.AreEqual(0, simulation.Dms.GetAllDmsSchedulerTasks().Count());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_CancelledEventIsNotScheduled()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Cancelled,
				EventType = EventType.Other,
				Name = "Test Event Cancelled",
			});
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.AreEqual(0, simulation.Dms.GetAllDmsSchedulerTasks().Count());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_CancelConfirmedEvent()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var ev = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());

			ev.EventState = EventState.Cancelled;

			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.AreEqual(0, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.IsNull(ev.SchedulerReference);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_DeleteConfirmedEvent()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var ev = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());

			api.Orchestration.DeleteJob(orchestrationJob);

			Assert.AreEqual(0, simulation.Dms.GetAllDmsSchedulerTasks().Count());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteNow()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var ev = new OrchestrationEvent()
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());

			var planHelper = api.GetMediaOpsPlanHelper();
			api.Orchestration.ExecuteEventsNow([ev], planHelper);

			Assert.AreEqual(0, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.AreEqual(EventState.Completed, ev.EventState);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteNowWithSuccessfulScripts()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var ev = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
				GlobalOrchestrationScript = "Script_Success",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJobConfiguration(orchestrationJob);

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());

			var planHelper = api.GetMediaOpsPlanHelper();
			api.Orchestration.ExecuteEventsNow([ev], planHelper);

			Assert.AreEqual(0, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.AreEqual(EventState.Completed, ev.EventState);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteNowWithFailedScripts()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var ev = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
				GlobalOrchestrationScript = "Script_Fail",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJobConfiguration(orchestrationJob);

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());

			var planHelper = api.GetMediaOpsPlanHelper();
			api.Orchestration.ExecuteEventsNow([ev], planHelper);

			Assert.AreEqual(0, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.AreEqual(EventState.Failed, ev.EventState);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_Reschedule()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var hourFromNow = DateTimeOffset.UtcNow + TimeSpan.FromHours(1);
			var ev = new OrchestrationEvent
			{
				EventTime = hourFromNow,
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			var utcScheduledTime = simulation.Dms.GetAllDmsSchedulerTasks().First().StartTime.ToUniversalTime();

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.AreEqual(ev.EventTime, utcScheduledTime);

			var twoHoursForNow = DateTimeOffset.UtcNow + TimeSpan.FromHours(2);
			ev.EventTime = twoHoursForNow;
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			utcScheduledTime = simulation.Dms.GetAllDmsSchedulerTasks().First().StartTime.ToUniversalTime();
			
			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.AreEqual(ev.EventTime, utcScheduledTime);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_RescheduleBetweenExistingTimeStamps()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var hourFromNow = DateTimeOffset.UtcNow + TimeSpan.FromHours(1);
			var twoHourFromNow = hourFromNow + TimeSpan.FromHours(1);

			var ev = new OrchestrationEvent
			{
				EventTime = hourFromNow,
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};
			var ev2 = new OrchestrationEvent
			{
				EventTime = hourFromNow,
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};
			var ev3 = new OrchestrationEvent
			{
				EventTime = twoHourFromNow,
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Confirmed",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			orchestrationJob.OrchestrationEvents.Add(ev2);
			orchestrationJob.OrchestrationEvents.Add(ev3);
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.AreEqual(2, simulation.Dms.GetAllDmsSchedulerTasks().Count());

			List<SimulatedSchedulerTask> tasksInTimeOrder = simulation.Dms.GetAllDmsSchedulerTasks().OrderBy(task => task.StartTime).ToList();

			Assert.Contains(ev.ID, tasksInTimeOrder[0].GetOrchestrationSchedulingInputList());
			Assert.Contains(ev2.ID, tasksInTimeOrder[0].GetOrchestrationSchedulingInputList());
			Assert.Contains(ev3.ID, tasksInTimeOrder[1].GetOrchestrationSchedulingInputList());

			ev2.EventTime = twoHourFromNow;
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.AreEqual(2, simulation.Dms.GetAllDmsSchedulerTasks().Count());

			tasksInTimeOrder = simulation.Dms.GetAllDmsSchedulerTasks().OrderBy(task => task.StartTime).ToList();

			Assert.Contains(ev2.ID, tasksInTimeOrder[1].GetOrchestrationSchedulingInputList());
			Assert.DoesNotContain(ev2.ID, tasksInTimeOrder[0].GetOrchestrationSchedulingInputList());
		}
	}
}