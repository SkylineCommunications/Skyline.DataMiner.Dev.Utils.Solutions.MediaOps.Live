namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests.Automation.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Plan;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class OrchestrationPhaseResumeTests
	{
		private const string NodeId = "node-a";
		private const int LocalAgentId = 123;

		private readonly FakeClock _clock = new FakeClock();
		private readonly Guid _jobInfoId = Guid.NewGuid();
		private readonly Guid _eventId = Guid.NewGuid();

		[TestMethod]
		public void OrchestrationPhaseResume_LongPollingInterval_SuspendsInsteadOfHoldingTheThread()
		{
			var sleeps = new List<TimeSpan>();
			var phases = CreateCloudPhases(spinUpStatus: () => PhaseStatus.InProgress(), pollEvery: TimeSpan.FromSeconds(15));
			var runner = new PhaseRunner(phases, Context(), sleep: sleeps.Add, now: _clock.Now, canSuspend: true);
			var start = _clock.Now();

			var result = runner.Run();

			Assert.IsTrue(result.IsSuccess);
			Assert.IsTrue(result.IsSuspended);
			Assert.IsEmpty(sleeps);
			Assert.AreEqual(1, result.SuspendedAt.PhaseIndex);
			Assert.AreEqual(start, result.SuspendedAt.WaitStarted);
			Assert.AreEqual(start.AddSeconds(15), result.ResumeTime);
			Assert.AreEqual("configure", result.WaitingPhaseKey);
			Assert.AreEqual("Waiting on phase 'spin-up'.", runner.Statuses["configure"].Message);
		}

		[TestMethod]
		public void OrchestrationPhaseResume_ShortPollingInterval_HoldsTheThread()
		{
			int polls = 0;
			var sleeps = new List<TimeSpan>();
			var phases = CreateCloudPhases(spinUpStatus: () => ++polls < 3 ? PhaseStatus.InProgress() : PhaseStatus.Completed(), pollEvery: TimeSpan.FromSeconds(5));
			var runner = new PhaseRunner(phases, Context(), sleep: d => { sleeps.Add(d); _clock.Advance(d); }, now: _clock.Now, canSuspend: true);

			var result = runner.Run();

			Assert.IsFalse(result.IsSuspended);
			Assert.IsTrue(result.IsSuccess);
			Assert.IsNotEmpty(sleeps);
		}

		[TestMethod]
		public void OrchestrationPhaseResume_RemainingTimeoutBelowThreshold_HoldsTheThread()
		{
			var sleeps = new List<TimeSpan>();
			var phases = CreateCloudPhases(spinUpStatus: () => PhaseStatus.InProgress(), pollEvery: TimeSpan.FromSeconds(30), timeout: TimeSpan.FromMinutes(1));
			var runner = new PhaseRunner(phases, Context(), sleep: d => { sleeps.Add(d); _clock.Advance(d); }, now: _clock.Now, canSuspend: true);

			// The wait started 52 seconds ago, so only 8 seconds remain until the timeout.
			var result = runner.Run(new ResumePoint(1, _clock.Now().AddSeconds(-52)));

			Assert.IsFalse(result.IsSuspended);
			Assert.AreEqual(TimeSpan.FromSeconds(8), sleeps.Single());
			StringAssert.Contains(result.FailureMessage, "did not complete within '1 minute'");
		}

		[TestMethod]
		public void OrchestrationPhaseResume_ResumePoint_ContinuesAtTheWaitWithoutRevisitingEarlierPhases()
		{
			int spinUps = 0;
			int initStatusCalls = 0;
			bool configured = false;
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("init", _ => PhaseStatus.InProgress()).WithStatus(_ => { initStatusCalls++; return PhaseStatus.InProgress(); }),
				Phase.Execute("spin-up", _ => { spinUps++; return PhaseStatus.InProgress(); }).WithStatus(_ => PhaseStatus.Completed()),
				Phase.WaitFor("spin-up").PollEvery(TimeSpan.FromSeconds(15)),
				Phase.Execute("configure", _ => { configured = true; return PhaseStatus.Completed(); }),
			};

			var result = new PhaseRunner(phases, Context(), sleep: _clock.Advance, now: _clock.Now, canSuspend: true).Run(new ResumePoint(2, _clock.Now()));

			Assert.IsTrue(result.IsSuccess);
			Assert.AreEqual(0, spinUps);
			Assert.AreEqual(0, initStatusCalls);
			Assert.IsTrue(configured);
		}

		[TestMethod]
		public void OrchestrationPhaseResume_TimeoutSpansExecutions()
		{
			var phases = CreateCloudPhases(spinUpStatus: () => PhaseStatus.InProgress(), pollEvery: TimeSpan.FromSeconds(15), timeout: TimeSpan.FromMinutes(30));

			var result = new PhaseRunner(phases, Context(), sleep: _clock.Advance, now: _clock.Now, canSuspend: true)
				.Run(new ResumePoint(1, _clock.Now().AddMinutes(-31)));

			Assert.AreEqual("Phase 'spin-up' did not complete within '30 minutes'.", result.FailureMessage);
		}

		[TestMethod]
		public void OrchestrationPhaseResume_ResumePointThatIsNoWait_WalksAllPhases()
		{
			bool spunUp = false;
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => { spunUp = true; return PhaseStatus.Completed(); }),
			};

			new PhaseRunner(phases, Context(), sleep: _clock.Advance, now: _clock.Now, canSuspend: true).Run(new ResumePoint(0, _clock.Now()));

			Assert.IsTrue(spunUp);
		}

		[TestMethod]
		public void OrchestrationPhaseResume_Suspension_SchedulesAResumeTaskAndStoresTheResumePoint()
		{
			var simulation = new MediaOpsLiveSimulation();
			var phases = CreateCloudPhases(spinUpStatus: () => PhaseStatus.InProgress(), pollEvery: TimeSpan.FromMinutes(1));

			var result = Run(simulation, phases, resume: false);

			Assert.IsTrue(result.IsSuspended);

			var task = simulation.Dms.GetAllDmsSchedulerTasks().Single();
			Assert.AreEqual(OrchestrationResumeScheduler.ResumeTaskNaming, task.Description);
			Assert.AreEqual(LocalAgentId, task.HandlingAgentId);

			var stored = simulation.Api.Orchestration.NodePhases.GetNodePhases(_eventId, NodeId);
			Assert.AreEqual(new ScheduledTaskId(LocalAgentId, task.Id), stored.ResumeSchedulerReference);
			Assert.AreEqual(1, stored.ResumePhaseIndex);
			Assert.AreEqual(_clock.Now(), stored.WaitStarted);
			Assert.AreEqual(_clock.Now().AddMinutes(1), stored.ResumeTime);
			Assert.HasCount(1, simulation.Api.Orchestration.NodePhases.GetPendingResumes(_eventId));
		}

		[TestMethod]
		public void OrchestrationPhaseResume_Resume_ContinuesAtTheWaitAndClearsTheSuspension()
		{
			var simulation = new MediaOpsLiveSimulation();
			int spinUps = 0;
			PhaseStatus spinUpStatus = PhaseStatus.InProgress();
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => { spinUps++; return PhaseStatus.InProgress(); }).WithStatus(_ => spinUps == 0 ? PhaseStatus.NotStarted() : spinUpStatus),
				Phase.WaitFor("spin-up").PollEvery(TimeSpan.FromMinutes(1)),
				Phase.Execute("configure", _ => PhaseStatus.Completed()),
			};

			Assert.IsTrue(Run(simulation, phases, resume: false).IsSuspended);

			_clock.Advance(TimeSpan.FromMinutes(1));
			spinUpStatus = PhaseStatus.Completed();
			var result = Run(simulation, phases, resume: true);

			Assert.IsTrue(result.IsSuccess);
			Assert.IsFalse(result.IsSuspended);
			Assert.AreEqual(1, spinUps);
			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());

			var stored = simulation.Api.Orchestration.NodePhases.GetNodePhases(_eventId, NodeId);
			Assert.IsNull(stored.ResumeSchedulerReference);
			Assert.IsNull(stored.ResumePhaseIndex);
			Assert.AreEqual(PhaseState.Completed, stored.Phases.Single(x => x.Key == "configure").State);
		}

		[TestMethod]
		public void OrchestrationPhaseResume_SuspendAgain_ReplacesTheResumeTask()
		{
			var simulation = new MediaOpsLiveSimulation();
			var phases = CreateCloudPhases(spinUpStatus: () => PhaseStatus.InProgress(), pollEvery: TimeSpan.FromMinutes(1));

			Run(simulation, phases, resume: false);
			var firstTask = simulation.Dms.GetAllDmsSchedulerTasks().Single();

			_clock.Advance(TimeSpan.FromMinutes(1));
			Run(simulation, phases, resume: true);

			var secondTask = simulation.Dms.GetAllDmsSchedulerTasks().Single();
			Assert.AreEqual(firstTask.StartTime.AddMinutes(1), secondTask.StartTime);
			Assert.AreEqual(_clock.Now().AddMinutes(-1), simulation.Api.Orchestration.NodePhases.GetNodePhases(_eventId, NodeId).WaitStarted, "Expected the wait to keep its original start, so the timeout spans executions.");
		}

		[TestMethod]
		public void OrchestrationPhaseResume_RetriggerThatIsNoResume_StartsFromTheFirstPhase()
		{
			var simulation = new MediaOpsLiveSimulation();
			int spinUpStatusCalls = 0;
			var phases = CreateCloudPhases(spinUpStatus: () => { spinUpStatusCalls++; return PhaseStatus.InProgress(); }, pollEvery: TimeSpan.FromMinutes(1));

			Run(simulation, phases, resume: false);
			int callsAfterFirstRun = spinUpStatusCalls;

			Run(simulation, phases, resume: false);

			Assert.IsTrue(spinUpStatusCalls > callsAfterFirstRun + 1, "Expected the status of 'spin-up' to be evaluated as a phase again, not only by the wait.");
			Assert.HasCount(1, simulation.Dms.GetAllDmsSchedulerTasks());
		}

		[TestMethod]
		public void OrchestrationPhaseResume_ScriptChangedWhileSuspended_WalksAllPhasesFromTheFirstOne()
		{
			var simulation = new MediaOpsLiveSimulation();
			Run(simulation, CreateCloudPhases(spinUpStatus: () => PhaseStatus.InProgress(), pollEvery: TimeSpan.FromMinutes(1)), resume: false);

			bool validated = false;
			var changedPhases = new List<OrchestrationPhase>(CreateCloudPhases(spinUpStatus: () => PhaseStatus.Completed(), pollEvery: TimeSpan.FromMinutes(1)));
			changedPhases.Insert(0, Phase.Execute("validate", _ => { validated = true; return PhaseStatus.Completed(); }));

			var result = Run(simulation, changedPhases, resume: true);

			Assert.IsTrue(result.IsSuccess);
			Assert.IsTrue(validated);
		}

		[TestMethod]
		public void OrchestrationPhaseResume_ResumeTaskData_RunsTheOrchestrationScriptForTheEvent()
		{
			var simulation = new MediaOpsLiveSimulation();
			var scheduler = new OrchestrationResumeScheduler(simulation.Api.Connection, LocalAgentId);

			scheduler.ScheduleResume(_eventId, DateTimeOffset.Now.AddMinutes(5));

			var task = simulation.Dms.GetAllDmsSchedulerTasks().Single();
			var action = task.Actions.Single();
			Assert.AreEqual(Constants.OrchestrationScriptName, action.ScriptInstance.ScriptName);
			StringAssert.Contains(((AutomationScriptInstanceInfo)action.ScriptInstance.ParameterIdToValue[0]).Value, _eventId.ToString());
			Assert.IsEmpty(task.GetOrchestrationSchedulingInputList(), "Expected the sliding window scheduler not to treat a resume task as an event task.");
		}

		[TestMethod]
		public void OrchestrationPhaseResume_InitialExecution_KeepsTheEventConfiguringWhileANodeWaits()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEventWithNodeScript(api);
			SuspendNode(simulation, ev, DateTimeOffset.UtcNow.AddMinutes(5));

			api.Orchestration.ExecuteEventsNow([ev.ID], api.GetMediaOpsPlanHelper());

			Assert.AreEqual(EventState.Configuring, GetEvent(api, ev.ID).EventState);
		}

		[TestMethod]
		public void OrchestrationPhaseResume_InitialExecutionWithoutWaitingNodes_CompletesTheEvent()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEventWithNodeScript(api);

			api.Orchestration.ExecuteEventsNow([ev.ID], api.GetMediaOpsPlanHelper());

			Assert.AreEqual(EventState.Completed, GetEvent(api, ev.ID).EventState);
		}

		[TestMethod]
		public void OrchestrationPhaseResume_DueResume_RunsTheNodeScriptWithTheResumeIntent()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEventWithNodeScript(api);
			SuspendNode(simulation, ev, DateTimeOffset.UtcNow.AddMinutes(5));
			api.Orchestration.ExecuteEventsNow([ev.ID], api.GetMediaOpsPlanHelper());

			MakeResumeDue(api, ev.ID);
			int executedBefore = CountNodeScriptExecutions(simulation, resume: true);
			api.Orchestration.ExecuteEventsNow([ev.ID], api.GetMediaOpsPlanHelper());

			Assert.AreEqual(executedBefore + 1, CountNodeScriptExecutions(simulation, resume: true));
		}

		[TestMethod]
		public void OrchestrationPhaseResume_ResumeThatIsNotDue_DoesNotRunTheNodeScript()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEventWithNodeScript(api);
			SuspendNode(simulation, ev, DateTimeOffset.UtcNow.AddMinutes(5));
			api.Orchestration.ExecuteEventsNow([ev.ID], api.GetMediaOpsPlanHelper());

			api.Orchestration.ExecuteEventsNow([ev.ID], api.GetMediaOpsPlanHelper());

			Assert.AreEqual(0, CountNodeScriptExecutions(simulation, resume: true));
			Assert.AreEqual(EventState.Configuring, GetEvent(api, ev.ID).EventState);
		}

		[TestMethod]
		public void OrchestrationPhaseResume_ResumeOfANodeThatLeftTheEvent_FailsTheEventAndRemovesTheResume()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEventWithNodeScript(api);
			api.Orchestration.ExecuteEventsNow([ev.ID], api.GetMediaOpsPlanHelper());
			SetEventState(api, ev.ID, EventState.Configuring);
			SuspendNode(simulation, ev, DateTimeOffset.UtcNow.AddMinutes(5), nodeId: "removed-node");
			MakeResumeDue(api, ev.ID, "removed-node");

			api.Orchestration.ExecuteEventsNow([ev.ID], api.GetMediaOpsPlanHelper());

			var stored = GetEvent(api, ev.ID);
			Assert.AreEqual(EventState.Failed, stored.EventState);
			StringAssert.Contains(stored.FailureInfo, "'removed-node' could not be resumed");
			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks().Where(x => x.Description == OrchestrationResumeScheduler.ResumeTaskNaming));
			Assert.IsEmpty(api.Orchestration.NodePhases.GetPendingResumes(ev.ID));
		}

		[TestMethod]
		public void OrchestrationPhaseResume_DeletingTheJob_RemovesThePendingResumeTask()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEventWithNodeScript(api, out string jobReference);
			SuspendNode(simulation, ev, DateTimeOffset.UtcNow.AddMinutes(5));

			api.Orchestration.DeleteJob(api.Orchestration.GetOrCreateNewOrchestrationJob(jobReference));

			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks().Where(x => x.Description == OrchestrationResumeScheduler.ResumeTaskNaming));
		}

		private PhaseRunResult Run(MediaOpsLiveSimulation simulation, IReadOnlyCollection<OrchestrationPhase> phases, bool resume)
		{
			var store = new NodePhasesStatusStore(simulation.Api.Orchestration.NodePhases, _jobInfoId, _eventId, NodeId, phases, _clock.Now);
			var scheduler = new OrchestrationResumeScheduler(simulation.Api.Connection, LocalAgentId);

			return NodePhasesExecution.Run(phases, Context(), store, scheduler, _eventId, resume, null, _clock.Advance, _clock.Now);
		}

		private static OrchestrationPhase[] CreateCloudPhases(Func<PhaseStatus> spinUpStatus, TimeSpan pollEvery, TimeSpan? timeout = null)
		{
			var wait = Phase.WaitFor("spin-up").PollEvery(pollEvery);
			if (timeout.HasValue)
			{
				wait.Timeout(timeout.Value);
			}

			return new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => PhaseStatus.InProgress()).WithStatus(_ => spinUpStatus()),
				wait,
				Phase.Execute("configure", _ => PhaseStatus.Completed()),
			};
		}

		private static OrchestrationEventConfiguration SaveEventWithNodeScript(MediaOpsLiveApi api)
		{
			return SaveEventWithNodeScript(api, out _);
		}

		private static OrchestrationEventConfiguration SaveEventWithNodeScript(MediaOpsLiveApi api, out string jobReference)
		{
			var ev = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Cloud Event",
				Configuration =
				{
					NodeConfigurations =
					{
						new NodeConfiguration { NodeId = NodeId, NodeLabel = "Transport", OrchestrationScriptName = "Script_Success" },
					},
				},
			};

			jobReference = Guid.NewGuid().ToString();
			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(jobReference);
			job.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJobConfiguration(job);

			return ev;
		}

		// Stands in for a node script that suspended on a long wait.
		private static void SuspendNode(MediaOpsLiveSimulation simulation, OrchestrationEvent ev, DateTimeOffset resumeTime, string nodeId = NodeId)
		{
			var repository = simulation.Api.Orchestration.NodePhases;
			var nodePhases = repository.GetOrCreateNodePhases(ev.JobInfoReference.Value.ID, ev.ID, nodeId);

			nodePhases.ResumePhaseIndex = 1;
			nodePhases.WaitStarted = DateTimeOffset.UtcNow;
			nodePhases.ResumeTime = resumeTime;
			nodePhases.ResumeSchedulerReference = new OrchestrationResumeScheduler(simulation.Api.Connection, LocalAgentId).ScheduleResume(ev.ID, resumeTime);
			repository.CreateOrUpdate(nodePhases);
		}

		private static void MakeResumeDue(MediaOpsLiveApi api, Guid eventId, string nodeId = NodeId)
		{
			var nodePhases = api.Orchestration.NodePhases.GetNodePhases(eventId, nodeId);
			nodePhases.ResumeTime = DateTimeOffset.UtcNow;
			api.Orchestration.NodePhases.CreateOrUpdate(nodePhases);
		}

		private static void SetEventState(MediaOpsLiveApi api, Guid eventId, EventState state)
		{
			var ev = GetEvent(api, eventId);
			ev.InternalSetState(state);
			new Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration.OrchestrationEventRepository(api).CreateOrUpdate(ev);
		}

		private static OrchestrationEventConfiguration GetEvent(MediaOpsLiveApi api, Guid eventId)
		{
			return api.Orchestration.GetEventConfigurationsById([eventId]).Single();
		}

		private static int CountNodeScriptExecutions(MediaOpsLiveSimulation simulation, bool resume)
		{
			return simulation.Dms.ExecutedScripts
				.Where(x => x.ScriptName == "Script_Success")
				.Select(x => x.CustomEntryPoint?.Parameters?.OfType<RequestScriptInfoInput>().FirstOrDefault()?.Data)
				.Count(data => data != null
					&& data.TryGetValue(Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey, out var input)
					&& input.Contains(OrchestrationScriptInternalInput.ResumeMetadataKey) == resume);
		}

		private static OrchestrationPhaseContext Context() => new OrchestrationPhaseContext(null);

		private sealed class FakeClock
		{
			private DateTimeOffset _now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

			public DateTimeOffset Now() => _now;

			public void Advance(TimeSpan duration) => _now += duration;
		}
	}
}
