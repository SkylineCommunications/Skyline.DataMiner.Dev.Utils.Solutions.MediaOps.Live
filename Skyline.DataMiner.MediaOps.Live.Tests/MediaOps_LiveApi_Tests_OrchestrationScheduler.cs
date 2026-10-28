namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.Advanced;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling;
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());
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

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
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

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ConfirmedEventMovedIntoThePastIsUnscheduled()
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());

			// An event that is moved into the past to be executed immediately must not keep the task it was scheduled with.
			ev.EventTime = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(1);
			ev.EventState = EventState.Draft;

			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
			Assert.IsNull(ev.SchedulerReference);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ConfirmedEventMovedOutOfTheWindowIsUnscheduled()
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());

			// The sliding window only schedules the near future, so an event moved beyond it is scheduled again later.
			ev.EventTime = DateTimeOffset.UtcNow + TimeSpan.FromDays(7);

			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
			Assert.IsNull(ev.SchedulerReference);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteEventsNowInBackground_RecordsTheHandOff()
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

			api.Orchestration.ExecuteEventsNowInBackground([ev]);

			// The launched script is deferred, so the hand-off must be persisted for callers that synchronize in the meantime.
			var storedEvent = api.Orchestration.GetOrchestrationJob(orchestrationJob.JobId).OrchestrationEvents.Single();

			Assert.IsNotNull(storedEvent.ActualStartTime);
			Assert.IsNull(storedEvent.SchedulerReference);
			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());

			ev.EventState = EventState.Cancelled;

			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());

			api.Orchestration.DeleteJob(orchestrationJob);

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());

			var planHelper = api.GetMediaOpsPlanHelper();
			api.Orchestration.ExecuteEventsNow([ev], planHelper);

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());

			var planHelper = api.GetMediaOpsPlanHelper();
			api.Orchestration.ExecuteEventsNow([ev], planHelper);

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());

			var planHelper = api.GetMediaOpsPlanHelper();
			api.Orchestration.ExecuteEventsNow([ev], planHelper);

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
			Assert.AreEqual(EventState.Failed, ev.EventState);
		}

		[TestMethod]
		public async Task MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteNowAsync()
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());

			var planHelper = api.GetMediaOpsPlanHelper();
			await api.Orchestration.ExecuteEventsNowAsync([ev], planHelper);

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
			Assert.AreEqual(EventState.Completed, ev.EventState);
		}

		[TestMethod]
		public async Task MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteNowAsyncWithSuccessfulScripts()
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());

			var planHelper = api.GetMediaOpsPlanHelper();
			await api.Orchestration.ExecuteEventsNowAsync([ev], planHelper);

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
			Assert.AreEqual(EventState.Completed, ev.EventState);
		}

		[TestMethod]
		public async Task MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteNowAsyncWithFailedScripts()
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());

			var planHelper = api.GetMediaOpsPlanHelper();
			await api.Orchestration.ExecuteEventsNowAsync([ev], planHelper);

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());
			Assert.AreEqual(ev.EventTime, utcScheduledTime);

			var twoHoursForNow = DateTimeOffset.UtcNow + TimeSpan.FromHours(2);
			ev.EventTime = twoHoursForNow;
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			utcScheduledTime = simulation.Dms.GetAllDmsSchedulerTasks().First().StartTime.ToUniversalTime();

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());
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

			Assert.HasCount(2, simulation.Dms.GetAllDmsSchedulerTasks());

			List<SimulatedSchedulerTask> tasksInTimeOrder = simulation.Dms.GetAllDmsSchedulerTasks().OrderBy(task => task.StartTime).ToList();

			Assert.Contains(ev.ID, tasksInTimeOrder[0].GetOrchestrationSchedulingInputList());
			Assert.Contains(ev2.ID, tasksInTimeOrder[0].GetOrchestrationSchedulingInputList());
			Assert.Contains(ev3.ID, tasksInTimeOrder[1].GetOrchestrationSchedulingInputList());

			ev2.EventTime = twoHourFromNow;
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.HasCount(2, simulation.Dms.GetAllDmsSchedulerTasks());

			tasksInTimeOrder = simulation.Dms.GetAllDmsSchedulerTasks().OrderBy(task => task.StartTime).ToList();

			Assert.Contains(ev2.ID, tasksInTimeOrder[1].GetOrchestrationSchedulingInputList());
			Assert.DoesNotContain(ev2.ID, tasksInTimeOrder[0].GetOrchestrationSchedulingInputList());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_CleanupPastScheduledEvent()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var jobReference = Guid.NewGuid().ToString();
			var ev = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(30),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event Past Confirmed",
			};

			var orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob(jobReference);
			orchestrationJob.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJob(orchestrationJob);

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());
			Assert.IsNotNull(ev.SchedulerReference);

			// Advance the cleanup window base time 2 hours into the future so that the event's
			// scheduled time (UtcNow + 30 min) falls before the window start (base - 1 h = UtcNow + 1 h).
			api.Orchestration.SyncCurrentSlidingWindow(DateTimeOffset.UtcNow + TimeSpan.FromHours(2));

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());

			var reloadedJob = api.Orchestration.GetOrCreateNewOrchestrationJob(jobReference);
			Assert.IsNull(reloadedJob.OrchestrationEvents.First().SchedulerReference);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteEventsNowInBackground_LaunchesDeferredScript()
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

			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());

			api.Orchestration.ExecuteEventsNowInBackground([ev]);

			var launchedScript = simulation.Dms.ExecutedScripts.SingleOrDefault(script => script.ScriptName == Constants.OrchestrationScriptName);
			Assert.IsNotNull(launchedScript, "Expected the orchestration script to be launched as a deferred task.");

			var options = launchedScript.Options.Sa;

			// The launch must be deferred (fire-and-forget) and carry the event IDs to orchestrate.
			Assert.Contains("DEFER:TRUE", options);
			Assert.Contains(
				option => option.StartsWith($"PARAMETERBYNAME:{Constants.OrchestrationScriptEventIdsParameter}:") && option.Contains(ev.ID.ToString()),
				options,
				"Expected the launched script to receive the event ID as a parameter.");

			// The future scheduler task must be removed so the event is not executed again when the task would trigger.
			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());

			// Fire-and-forget: the deferred script handles execution, so the event is not completed synchronously by the caller.
			Assert.AreEqual(EventState.Confirmed, ev.EventState);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteEventsNowInBackground_EmptyGuidThrows()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			Assert.Throws<ArgumentException>(() => api.Orchestration.ExecuteEventsNowInBackground(new List<Guid> { Guid.Empty }));

			Assert.DoesNotContain(
				script => script.ScriptName == Constants.OrchestrationScriptName,
				simulation.Dms.ExecutedScripts,
				"No orchestration script should be launched when the event IDs are invalid.");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteEventsNowInBackground_EmptyDoesNotLaunchScript()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			api.Orchestration.ExecuteEventsNowInBackground(new List<Guid>());

			Assert.DoesNotContain(
				script => script.ScriptName == Constants.OrchestrationScriptName,
				simulation.Dms.ExecutedScripts,
				"No orchestration script should be launched when there are no events to execute.");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteEventsNowInBackground_EventWhoseTaskIsDue_IsLeftToItsTask()
		{
			var simulation = new MediaOpsLiveSimulation();
			var jobReference = Guid.NewGuid().ToString();
			SaveJobWithConfirmedEvents(simulation.Api, jobReference, 1);

			var dueTask = MakeTaskDue(simulation);

			MakeDueAndExecuteInBackground(simulation, jobReference);

			Assert.AreEqual(0, GetLaunchCount(simulation), "Expected an event that its scheduler task executes not to be launched a second time.");
			Assert.AreSame(dueTask, simulation.Dms.GetAllDmsSchedulerTasks().Single(), "Expected the due task not to be updated or deleted, as that makes the scheduler run it again.");

			var storedEvent = simulation.Api.Orchestration.GetOrchestrationJob(jobReference).OrchestrationEvents.Single();
			Assert.AreEqual(storedEvent.EventTime, storedEvent.ActualStartTime, "Expected the event to be marked as started at its scheduled time, so the event order stays valid.");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteEventsNow_SharedNearlyDueTask_IsNotUpdated()
		{
			var simulation = new MediaOpsLiveSimulation();
			var events = SaveJobWithConfirmedEvents(simulation.Api, Guid.NewGuid().ToString(), 2, atSameTime: true);

			// An update this close to the task's time can reach the scheduler after the task already started.
			var dueTask = MakeTaskDue(simulation, TimeSpan.FromSeconds(2));

			var api = new MediaOpsLiveApi(simulation.Dms.CreateConnection());
			api.Orchestration.ExecuteEventsNow([events[0].ID], api.GetMediaOpsPlanHelper());

			Assert.AreSame(dueTask, simulation.Dms.GetAllDmsSchedulerTasks().Single(), "Expected the nearly due task not to be updated.");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_Scheduling_DueTime_DoesNotUpdateTheDueTask()
		{
			var simulation = new MediaOpsLiveSimulation();
			var jobReference = Guid.NewGuid().ToString();
			SaveJobWithConfirmedEvents(simulation.Api, jobReference, 2, atSameTime: true);

			var dueTask = MakeTaskDue(simulation);

			// Mirrors a sliding window that still includes a due event whose reference was already cleared.
			var ev = simulation.Api.Orchestration.GetOrchestrationJob(jobReference).OrchestrationEvents.First();
			ev.EventTime = new DateTimeOffset(dueTask.StartTime);
			ev.SchedulerReference = null;

			new OrchestrationScheduler(simulation.Dms.CreateConnection()).CreateOrUpdateEventScheduling([ev]);

			Assert.AreSame(dueTask, simulation.Dms.GetAllDmsSchedulerTasks().Single(), "Expected no task to be created or updated for a time that has passed.");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteEventsNowInBackground_TaskCreatedAfterTheTaskListWasLoaded_IsFound()
		{
			var simulation = new MediaOpsLiveSimulation();

			// Loads the task list of this API instance before the event below is scheduled by another instance.
			var api = new MediaOpsLiveApi(simulation.Dms.CreateConnection());
			SaveJobWithConfirmedEvents(api, Guid.NewGuid().ToString(), 1, firstEventTime: DateTimeOffset.UtcNow + TimeSpan.FromHours(2));

			var jobReference = Guid.NewGuid().ToString();
			SaveJobWithConfirmedEvents(simulation.Api, jobReference, 1);

			var job = api.Orchestration.GetOrCreateNewOrchestrationJob(jobReference);
			var ev = job.OrchestrationEvents.Single();
			ev.EventTime = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(1);
			ev.EventState = EventState.Draft;
			api.Orchestration.SaveOrchestrationJob(job);

			Assert.IsNull(simulation.Api.Orchestration.GetOrchestrationJob(jobReference).OrchestrationEvents.Single().ActualStartTime, "Expected the event not to be marked as started by a task that was only missing from the cached task list.");

			api.Orchestration.ExecuteEventsNowInBackground([ev]);

			Assert.AreEqual(1, GetLaunchCount(simulation), "Expected the event to be launched, as its future task was found and removed.");
			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks(), "Expected only the task of the other job to remain.");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteEventsNow_SharedDueTask_IsNotUpdated()
		{
			var simulation = new MediaOpsLiveSimulation();
			var events = SaveJobWithConfirmedEvents(simulation.Api, Guid.NewGuid().ToString(), 2, atSameTime: true);

			var dueTask = MakeTaskDue(simulation);

			// Mirrors the orchestration script that the due task launched.
			var api = new MediaOpsLiveApi(simulation.Dms.CreateConnection());
			api.Orchestration.ExecuteEventsNow([events[0].ID], api.GetMediaOpsPlanHelper());

			Assert.AreSame(dueTask, simulation.Dms.GetAllDmsSchedulerTasks().Single(), "Expected the due task not to be updated, as that makes the scheduler run it again.");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteEventsNowInBackground_EventWhoseTaskWasRemovedByTheOrchestrationScript_IsNotLaunchedAgain()
		{
			var simulation = new MediaOpsLiveSimulation();
			var jobReference = Guid.NewGuid().ToString();
			SaveJobWithConfirmedEvents(simulation.Api, jobReference, 1);

			var task = simulation.Dms.GetAllDmsSchedulerTasks().Single();
			simulation.Dms.Agents[task.HandlingAgentId].Scheduler.Tasks.Remove(task.Id);

			MakeDueAndExecuteInBackground(simulation, jobReference);

			Assert.AreEqual(0, GetLaunchCount(simulation), "Expected an event that the orchestration script is executing not to be launched a second time.");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationScheduler_ExecuteEventsNowInBackground_EventWhoseTaskIsNotDue_IsLaunchedOnce()
		{
			var simulation = new MediaOpsLiveSimulation();
			var jobReference = Guid.NewGuid().ToString();
			SaveJobWithConfirmedEvents(simulation.Api, jobReference, 1);

			var (api, ev) = MakeDueAndExecuteInBackground(simulation, jobReference);
			api.Orchestration.ExecuteEventsNowInBackground([ev]);

			Assert.AreEqual(1, GetLaunchCount(simulation), "Expected an event whose task is not due yet to be launched exactly once.");
			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
		}

		// Moves the single scheduler task to a time around now, like a task that the scheduler is about to run or is running.
		private static SimulatedSchedulerTask MakeTaskDue(MediaOpsLiveSimulation simulation, TimeSpan? offset = null)
		{
			var task = simulation.Dms.GetAllDmsSchedulerTasks().Single();
			var dueTask = new OrchestrationSchedulerTask(DateTimeOffset.UtcNow + (offset ?? TimeSpan.FromSeconds(-1)), task.GetOrchestrationSchedulingInputList())
			{
				ScheduledTaskId = new ScheduledTaskId(task.HandlingAgentId, task.Id),
			};

			simulation.Dms.CreateConnection().HandleMessage(new SetSchedulerInfoMessage
			{
				DataMinerID = task.HandlingAgentId,
				What = 2,
				Info = Int32.MaxValue,
				Ppsa = new PPSA(dueTask.GenerateSchedulerTaskData()),
			});

			return simulation.Dms.GetAllDmsSchedulerTasks().Single();
		}

		// Mirrors a caller that finds the event due, puts it in Draft and executes it immediately, as MediaOps Plan does.
		private static (MediaOpsLiveApi Api, OrchestrationEvent Event) MakeDueAndExecuteInBackground(MediaOpsLiveSimulation simulation, string jobReference)
		{
			// A new API instance has no cached tasks, like the script that performs the synchronization.
			var api = new MediaOpsLiveApi(simulation.Dms.CreateConnection());
			var job = api.Orchestration.GetOrCreateNewOrchestrationJob(jobReference);
			var ev = job.OrchestrationEvents.Single();

			ev.EventTime = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(1);
			ev.EventState = EventState.Draft;
			api.Orchestration.SaveOrchestrationJob(job);

			api.Orchestration.ExecuteEventsNowInBackground([ev]);

			return (api, ev);
		}

		private static int GetLaunchCount(MediaOpsLiveSimulation simulation)
		{
			return simulation.Dms.ExecutedScripts.Count(script => script.ScriptName == Constants.OrchestrationScriptName);
		}

		private static List<OrchestrationEvent> SaveJobWithConfirmedEvents(MediaOpsLiveApi api, string jobReference, int count, bool atSameTime = false, DateTimeOffset? firstEventTime = null)
		{
			var job = api.Orchestration.GetOrCreateNewOrchestrationJob(jobReference);
			var baseTime = firstEventTime ?? DateTimeOffset.UtcNow + TimeSpan.FromHours(1);

			for (int i = 0; i < count; i++)
			{
				job.OrchestrationEvents.Add(new OrchestrationEvent
				{
					EventTime = atSameTime ? baseTime : baseTime + TimeSpan.FromMinutes(i),
					EventState = EventState.Confirmed,
					EventType = EventType.Other,
					Name = $"Test Event {i}",
				});
			}

			api.Orchestration.SaveOrchestrationJob(job);

			return job.OrchestrationEvents.ToList();
		}
	}
}