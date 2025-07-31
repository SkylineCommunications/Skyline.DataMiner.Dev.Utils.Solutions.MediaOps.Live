namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

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
				EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
				Name = "Test Event Confirmed",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			var utcScheduledTime = simulation.Dms.GetAllDmsSchedulerTasks().First().StartTime.ToUniversalTime();
			var trimmedEventTime = new DateTimeOffset(
				new DateTime(ev.EventTime.Value.Ticks - ev.EventTime.Value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc));

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.Contains(ev.ID, simulation.Dms.GetAllDmsSchedulerTasks().First().GetOrchestrationSchedulingInputList());
			Assert.AreEqual(trimmedEventTime, utcScheduledTime);

			Assert.Contains(orchestrationJob.OrchestrationEvents.First().ReservationInstance.DmaId, simulation.Dms.Agents.Keys);
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
				EventState = SlcOrchestrationIds.Enums.EventState.Draft,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
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
				EventState = SlcOrchestrationIds.Enums.EventState.Cancelled,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
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
				EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
				Name = "Test Event Confirmed",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());

			ev.EventState = SlcOrchestrationIds.Enums.EventState.Cancelled;

			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.AreEqual(0, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.IsNull(ev.ReservationInstance);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_DeleteConfirmedEvent()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var ev = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
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
				EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
				Name = "Test Event Confirmed",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());

			api.Orchestration.ExecuteEventsNow(new List<OrchestrationEvent> { ev });

			Assert.AreEqual(0, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.AreEqual(SlcOrchestrationIds.Enums.EventState.Completed, ev.EventState);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteNowWithSuccessfulScripts()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var ev = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
				Name = "Test Event Confirmed",
				GlobalOrchestrationScript = "Script_Success",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJobConfiguration(orchestrationJob);

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());

			api.Orchestration.ExecuteEventsNow(new List<OrchestrationEvent> { ev });

			Assert.AreEqual(0, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.AreEqual(SlcOrchestrationIds.Enums.EventState.Completed, ev.EventState);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteNowWithFailedScripts()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var ev = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
				Name = "Test Event Confirmed",
				GlobalOrchestrationScript = "Script_Fail",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJobConfiguration(orchestrationJob);

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());

			api.Orchestration.ExecuteEventsNow(new List<OrchestrationEvent> { ev });

			Assert.AreEqual(0, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.AreEqual(SlcOrchestrationIds.Enums.EventState.Failed, ev.EventState);
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
				EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
				Name = "Test Event Confirmed",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			var utcScheduledTime = simulation.Dms.GetAllDmsSchedulerTasks().First().StartTime.ToUniversalTime();
			var trimmedEventTime = new DateTimeOffset(
				new DateTime(ev.EventTime.Value.Ticks - ev.EventTime.Value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc));

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.AreEqual(trimmedEventTime, utcScheduledTime);

			var twoHoursForNow = DateTimeOffset.UtcNow + TimeSpan.FromHours(2);
			ev.EventTime = twoHoursForNow;
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			utcScheduledTime = simulation.Dms.GetAllDmsSchedulerTasks().First().StartTime.ToUniversalTime();
			trimmedEventTime = new DateTimeOffset(
				new DateTime(ev.EventTime.Value.Ticks - ev.EventTime.Value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc));

			Assert.AreEqual(1, simulation.Dms.GetAllDmsSchedulerTasks().Count());
			Assert.AreEqual(trimmedEventTime, utcScheduledTime);
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
				EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
				Name = "Test Event Confirmed",
			};
			var ev2 = new OrchestrationEvent
			{
				EventTime = hourFromNow,
				EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
				Name = "Test Event Confirmed",
			};
			var ev3 = new OrchestrationEvent
			{
				EventTime = twoHourFromNow,
				EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
				EventType = SlcOrchestrationIds.Enums.EventType.Other,
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