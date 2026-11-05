namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests.Automation.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class OrchestrationPhasePersistenceTests
	{
		private const string NodeId = "node-a";

		private readonly Guid _jobInfoId = Guid.NewGuid();
		private readonly Guid _eventId = Guid.NewGuid();
		private readonly FakeClock _clock = new FakeClock();

		[TestMethod]
		public void OrchestrationPhasePersistence_NodeScriptMetadata_ContainsTheNodeId()
		{
			var arguments = new OrchestrationScriptInternalInput(_eventId, OrchestrationLevel.Node, NodeId).ToMetadataArguments().ToList();

			Assert.IsTrue(arguments.Any(x => x.Name == OrchestrationScriptInternalInput.NodeIdMetadataKey && x.Value == NodeId));
			Assert.IsTrue(arguments.Any(x => x.Name == OrchestrationScriptInternalInput.EventIdMetadataKey && x.Value == _eventId.ToString()));
		}

		[TestMethod]
		public void OrchestrationPhasePersistence_GlobalScriptMetadata_HasNoNodeId()
		{
			var arguments = new OrchestrationScriptInternalInput(_eventId, OrchestrationLevel.Global).ToMetadataArguments();

			Assert.IsFalse(arguments.Any(x => x.Name == OrchestrationScriptInternalInput.NodeIdMetadataKey));
		}

		[TestMethod]
		public void OrchestrationPhasePersistence_Run_StoresTheStatusOfEveryPhase()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => { _clock.Advance(TimeSpan.FromSeconds(3)); return PhaseStatus.Completed("Service svc-1 running."); }),
				Phase.Execute("configure", _ => PhaseStatus.InProgress("Applying configuration.")),
			};

			var start = _clock.Now();
			Run(api, phases);

			var stored = api.Orchestration.NodePhases.GetNodePhases(_eventId, NodeId);
			Assert.IsNotNull(stored);
			Assert.AreEqual(_jobInfoId, stored.JobInfoReference.ID);
			Assert.AreEqual("spin-up|configure", stored.PhaseLayoutFingerprint);

			var spinUp = stored.Phases.Single(x => x.Key == "spin-up");
			Assert.AreEqual(0, spinUp.Order);
			Assert.AreEqual(PhaseState.Completed, spinUp.State);
			Assert.AreEqual("Service svc-1 running.", spinUp.Message);
			Assert.AreEqual(start, spinUp.Started);
			Assert.AreEqual(start.AddSeconds(3), spinUp.Ended);

			var configure = stored.Phases.Single(x => x.Key == "configure");
			Assert.AreEqual(1, configure.Order);
			Assert.AreEqual(PhaseState.InProgress, configure.State);
			Assert.IsNotNull(configure.Started);
			Assert.IsNull(configure.Ended);
		}

		[TestMethod]
		public void OrchestrationPhasePersistence_Retrigger_SkipsAPhaseRecordedAsCompleted()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			int spinUps = 0;
			int configureAttempts = 0;
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => { spinUps++; return PhaseStatus.Completed(); }),
				Phase.Execute("configure", _ => ++configureAttempts == 1 ? PhaseStatus.Failed("Device unreachable.") : PhaseStatus.Completed()),
			};

			Assert.IsFalse(Run(api, phases).IsSuccess);
			Assert.IsTrue(Run(api, phases).IsSuccess);

			Assert.AreEqual(1, spinUps);
			Assert.AreEqual(2, configureAttempts);

			var configure = api.Orchestration.NodePhases.GetNodePhases(_eventId, NodeId).Phases.Single(x => x.Key == "configure");
			Assert.AreEqual(PhaseState.Completed, configure.State);
			Assert.IsNull(configure.Message);
			Assert.AreEqual(1, api.Orchestration.NodePhases.CountAll());
		}

		[TestMethod]
		public void OrchestrationPhasePersistence_UnchangedStatus_IsNotWrittenAgain()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => PhaseStatus.Completed()).WithStatus(_ => PhaseStatus.Completed("Running.")),
			};

			Run(api, phases);
			var firstUpdate = api.Orchestration.NodePhases.GetNodePhases(_eventId, NodeId).Phases.Single().LastUpdated;

			_clock.Advance(TimeSpan.FromMinutes(5));
			Run(api, phases);

			Assert.AreEqual(firstUpdate, api.Orchestration.NodePhases.GetNodePhases(_eventId, NodeId).Phases.Single().LastUpdated);
		}

		[TestMethod]
		public void OrchestrationPhasePersistence_RemovedPhase_DisappearsFromTheStoredStatuses()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			Run(api, Phase.Execute("spin-up", _ => PhaseStatus.Completed()), Phase.Execute("legacy", _ => PhaseStatus.Completed()));
			Run(api, Phase.Execute("spin-up", _ => PhaseStatus.Completed()), Phase.Execute("configure", _ => PhaseStatus.Completed()));

			var stored = api.Orchestration.NodePhases.GetNodePhases(_eventId, NodeId);
			CollectionAssert.AreEquivalent(new[] { "spin-up", "configure" }, stored.Phases.Select(x => x.Key).ToList());
			Assert.AreEqual("spin-up|configure", stored.PhaseLayoutFingerprint);
		}

		[TestMethod]
		public void OrchestrationPhasePersistence_Wait_StoresTheWaitingMessageOnThePhaseAfterTheWait()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			int polls = 0;
			string messageWhileWaiting = null;
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => PhaseStatus.InProgress()).WithStatus(_ => ++polls switch
				{
					1 => PhaseStatus.NotStarted(),
					2 => PhaseStatus.InProgress(),
					_ => PhaseStatus.Completed(),
				}),
				Phase.WaitFor("spin-up").PollEvery(TimeSpan.FromSeconds(5)),
				Phase.Execute("configure", _ => PhaseStatus.Completed()),
			};

			var store = CreateStore(api, phases);
			var runner = new PhaseRunner(
				phases,
				new OrchestrationPhaseContext(null),
				store,
				sleep: d =>
				{
					messageWhileWaiting ??= api.Orchestration.NodePhases.GetNodePhases(_eventId, NodeId).Phases.Single(x => x.Key == "configure").Message;
					_clock.Advance(d);
				},
				now: _clock.Now);

			Assert.IsTrue(runner.Run().IsSuccess);
			Assert.AreEqual("Waiting on phase 'spin-up'.", messageWhileWaiting);
			Assert.IsNull(api.Orchestration.NodePhases.GetNodePhases(_eventId, NodeId).Phases.Single(x => x.Key == "configure").Message);
		}

		[TestMethod]
		public void OrchestrationPhasePersistence_OverlappingEventsOfTheSameNode_KeepTheirOwnStatuses()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var otherEventId = Guid.NewGuid();

			Run(api, Phase.Execute("spin-up", _ => PhaseStatus.InProgress()));
			new PhaseRunner(
				new[] { Phase.Execute("go-live", _ => PhaseStatus.Completed()) },
				new OrchestrationPhaseContext(null),
				new NodePhasesStatusStore(api.Orchestration.NodePhases, _jobInfoId, otherEventId, NodeId, new[] { Phase.Execute("go-live", _ => PhaseStatus.Completed()) }, _clock.Now),
				_clock.Advance,
				_clock.Now).Run();

			Assert.AreEqual("spin-up", api.Orchestration.NodePhases.GetNodePhases(_eventId, NodeId).Phases.Single().Key);
			Assert.AreEqual("go-live", api.Orchestration.NodePhases.GetNodePhases(otherEventId, NodeId).Phases.Single().Key);
		}

		private PhaseRunResult Run(MediaOpsLiveApi api, params OrchestrationPhase[] phases)
		{
			return new PhaseRunner(phases, new OrchestrationPhaseContext(null), CreateStore(api, phases), _clock.Advance, _clock.Now).Run();
		}

		private NodePhasesStatusStore CreateStore(MediaOpsLiveApi api, IEnumerable<OrchestrationPhase> phases)
		{
			return new NodePhasesStatusStore(api.Orchestration.NodePhases, _jobInfoId, _eventId, NodeId, phases, _clock.Now);
		}

		private sealed class FakeClock
		{
			private DateTimeOffset _now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

			public DateTimeOffset Now() => _now;

			public void Advance(TimeSpan duration) => _now += duration;
		}
	}
}
