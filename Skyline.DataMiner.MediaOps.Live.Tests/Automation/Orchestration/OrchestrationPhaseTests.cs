namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests.Automation.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Moq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	[TestClass]
	public sealed class OrchestrationPhaseTests
	{
		[TestMethod]
		public void OrchestrationPhases_ScriptWithoutPhases_CallsOrchestrate()
		{
			var script = new LegacyScript();

			script.ExecuteOrchestration(null, false);

			Assert.IsTrue(script.WasOrchestrated);
		}

		[TestMethod]
		public void OrchestrationPhases_ScriptWithPhases_RunsPhasesInsteadOfOrchestrate()
		{
			var script = new PhasedScript();

			script.ExecuteOrchestration(null, false);

			CollectionAssert.AreEqual(new[] { "spin-up", "configure" }, script.Executed);
			Assert.IsFalse(script.WasOrchestrated);
		}

		[TestMethod]
		public void OrchestrationPhases_ScriptWithoutPhasesOrOrchestrate_Fails()
		{
			var script = new EmptyScript();

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => script.ExecuteOrchestration(null, false));

			StringAssert.Contains(exception.Message, "must either override Orchestrate or define phases");
		}

		[TestMethod]
		public void OrchestrationPhases_DynamicScriptWithPhases_RunsPhasesWithTheInputValues()
		{
			var script = new PhasedDynamicScript();
			script.EvaluateInputs(Mock.Of<IEngine>(), new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue> { ["General/Region"] = "EU" }));

			script.ExecuteOrchestration(null, false);

			Assert.AreEqual("EU", script.Region);
		}

		[TestMethod]
		public void OrchestrationPhases_FailingPhase_FailsTheScriptAndStopsTheFollowingPhases()
		{
			var executed = new List<string>();
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => { executed.Add("spin-up"); return PhaseStatus.Failed("Region rejected."); }),
				Phase.Execute("configure", _ => { executed.Add("configure"); return PhaseStatus.Completed(); }),
			};

			var runner = CreateRunner(phases);
			var result = runner.Run();

			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual("Phase 'spin-up' failed: Region rejected.", result.FailureMessage);
			CollectionAssert.AreEqual(new[] { "spin-up" }, executed);
			Assert.AreEqual(PhaseState.NotStarted, runner.Statuses["configure"].State);
		}

		[TestMethod]
		public void OrchestrationPhases_ThrowingPhase_IsMarkedFailedWithTheExceptionMessage()
		{
			var runner = CreateRunner(Phase.Execute("spin-up", _ => throw new InvalidOperationException("Cloud unreachable.")));

			var result = runner.Run();

			Assert.AreEqual("Phase 'spin-up' failed: Cloud unreachable.", result.FailureMessage);
			Assert.AreEqual(PhaseState.Failed, runner.Statuses["spin-up"].State);
		}

		[TestMethod]
		public void OrchestrationPhases_CompletedStatus_SkipsThePhase()
		{
			bool executed = false;
			var runner = CreateRunner(
				Phase.Execute("spin-up", _ => { executed = true; return PhaseStatus.Completed(); })
					.WithStatus(_ => PhaseStatus.Completed("Already running.")));

			var result = runner.Run();

			Assert.IsTrue(result.IsSuccess);
			Assert.IsFalse(executed);
			Assert.AreEqual("Already running.", runner.Statuses["spin-up"].Message);
		}

		[TestMethod]
		public void OrchestrationPhases_RecordedCompletionWithoutStatusMethod_SkipsThePhase()
		{
			bool executed = false;
			var recorded = new Dictionary<string, PhaseStatus> { ["spin-up"] = PhaseStatus.Completed() };
			var runner = CreateRunner(recorded, Phase.Execute("spin-up", _ => { executed = true; return PhaseStatus.Completed(); }));

			runner.Run();

			Assert.IsFalse(executed);
		}

		[TestMethod]
		public void OrchestrationPhases_RecordedCompletionThatNoLongerHolds_IsExecutedAgain()
		{
			bool executed = false;
			var recorded = new Dictionary<string, PhaseStatus> { ["configure"] = PhaseStatus.Completed() };
			var runner = CreateRunner(
				recorded,
				Phase.Execute("configure", _ => { executed = true; return PhaseStatus.Completed(); })
					.WithStatus(_ => PhaseStatus.NotStarted("Configuration was changed.")));

			runner.Run();

			Assert.IsTrue(executed);
		}

		[TestMethod]
		public void OrchestrationPhases_StatusMethodThatKeepsFailing_FailsThePhaseWithoutExecutingIt()
		{
			int statusCalls = 0;
			bool executed = false;
			var runner = CreateRunner(
				Phase.Execute("spin-up", _ => { executed = true; return PhaseStatus.Completed(); })
					.WithStatus(_ => { statusCalls++; throw new TimeoutException("API timeout."); }));

			var result = runner.Run();

			Assert.IsFalse(result.IsSuccess);
			Assert.IsFalse(executed);
			Assert.AreEqual(PhaseRunner.StatusAttempts, statusCalls);
			StringAssert.Contains(result.FailureMessage, "Unable to retrieve status of 'spin-up' phase");
		}

		[TestMethod]
		public void OrchestrationPhases_TransientStatusError_IsRetried()
		{
			int statusCalls = 0;
			var runner = CreateRunner(
				Phase.Execute("spin-up", _ => PhaseStatus.Completed())
					.WithStatus(_ => ++statusCalls == 1 ? throw new TimeoutException() : PhaseStatus.Completed()));

			Assert.IsTrue(runner.Run().IsSuccess);
			Assert.AreEqual(2, statusCalls);
		}

		[TestMethod]
		public void OrchestrationPhases_Wait_PollsUntilTheTargetCompletes()
		{
			int polls = 0;
			var sleeps = new List<TimeSpan>();
			var clock = new FakeClock();
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => PhaseStatus.InProgress()).WithStatus(_ => ++polls < 4 ? (polls == 1 ? PhaseStatus.NotStarted() : PhaseStatus.InProgress()) : PhaseStatus.Completed()),
				Phase.WaitFor("spin-up").PollEvery(TimeSpan.FromSeconds(5)).Timeout(TimeSpan.FromMinutes(1)),
				Phase.Execute("configure", _ => PhaseStatus.Completed()),
			};

			var runner = new PhaseRunner(phases, CreateContext(), sleep: d => { sleeps.Add(d); clock.Advance(d); }, now: clock.Now);
			var result = runner.Run();

			Assert.IsTrue(result.IsSuccess, result.FailureMessage);
			Assert.HasCount(2, sleeps);
			Assert.IsTrue(sleeps.All(x => x == TimeSpan.FromSeconds(5)));
			Assert.AreEqual(PhaseState.Completed, runner.Statuses["spin-up"].State);
			Assert.AreEqual(PhaseState.Completed, runner.Statuses["configure"].State);
		}

		[TestMethod]
		public void OrchestrationPhases_WaitTimeout_FailsThePhaseAfterTheWait()
		{
			bool configured = false;
			var clock = new FakeClock();
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => PhaseStatus.InProgress()).WithStatus(_ => PhaseStatus.InProgress()),
				Phase.WaitFor("spin-up").PollEvery(TimeSpan.FromSeconds(15)).Timeout(TimeSpan.FromMinutes(30)),
				Phase.Execute("configure", _ => { configured = true; return PhaseStatus.Completed(); }),
			};

			var runner = new PhaseRunner(phases, CreateContext(), sleep: clock.Advance, now: clock.Now);
			var result = runner.Run();

			Assert.IsFalse(result.IsSuccess);
			Assert.IsFalse(configured);
			Assert.AreEqual("Phase 'spin-up' did not complete within '30 minutes'.", result.FailureMessage);
			Assert.AreEqual(PhaseState.Failed, runner.Statuses["configure"].State);
		}

		[TestMethod]
		public void OrchestrationPhases_WaitedPhaseFails_FailsTheScript()
		{
			var clock = new FakeClock();
			int polls = 0;
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => PhaseStatus.InProgress()).WithStatus(_ => ++polls == 1 ? PhaseStatus.NotStarted() : PhaseStatus.Failed("Quota exceeded.")),
				Phase.WaitFor("spin-up"),
				Phase.Execute("configure", _ => PhaseStatus.Completed()),
			};

			var result = new PhaseRunner(phases, CreateContext(), sleep: clock.Advance, now: clock.Now).Run();

			Assert.AreEqual("Phase 'spin-up' failed: Quota exceeded.", result.FailureMessage);
		}

		[TestMethod]
		public void OrchestrationPhases_DuplicateKey_IsRejectedBeforeAnythingIsExecuted()
		{
			bool executed = false;
			var runner = CreateRunner(
				Phase.Execute("spin-up", _ => { executed = true; return PhaseStatus.Completed(); }),
				Phase.Execute("spin-up", _ => PhaseStatus.Completed()));

			var result = runner.Run();

			Assert.AreEqual("Phase 'spin-up' is defined more than once.", result.FailureMessage);
			Assert.IsFalse(executed);
		}

		[TestMethod]
		public void OrchestrationPhases_WaitOnUnknownPhase_FailsThePhaseAfterTheWaitBeforeAnythingIsExecuted()
		{
			bool executed = false;
			var runner = CreateRunner(
				Phase.Execute("spin-up", _ => { executed = true; return PhaseStatus.Completed(); }).WithStatus(_ => PhaseStatus.NotStarted()),
				Phase.WaitFor("spin-upp"),
				Phase.Execute("configure", _ => PhaseStatus.Completed()));

			var result = runner.Run();

			Assert.IsFalse(executed);
			StringAssert.Contains(result.FailureMessage, "Unable to resolve the 'spin-upp' phase");
			Assert.AreEqual(PhaseState.Failed, runner.Statuses["configure"].State);
			Assert.AreEqual(PhaseState.NotStarted, runner.Statuses["spin-up"].State);
		}

		[TestMethod]
		public void OrchestrationPhases_WaitOnPhaseWithoutStatusMethod_IsRejected()
		{
			var runner = CreateRunner(
				Phase.Execute("spin-up", _ => PhaseStatus.InProgress()),
				Phase.WaitFor("spin-up"));

			StringAssert.Contains(runner.Run().FailureMessage, "has no status method");
		}

		[TestMethod]
		public void OrchestrationPhases_Declaration_ValidatesArguments()
		{
			Assert.ThrowsExactly<ArgumentException>(() => Phase.Execute(" ", _ => PhaseStatus.Completed()));
			Assert.ThrowsExactly<ArgumentNullException>(() => Phase.Execute("key", null));
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Phase.WaitFor("key").PollEvery(TimeSpan.Zero));
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Phase.WaitFor("key").Timeout(TimeSpan.FromSeconds(-1)));
		}

		[TestMethod]
		public void OrchestrationPhases_PhaseStatus_TreatsAnEmptyMessageAsNoMessage()
		{
			Assert.IsNull(PhaseStatus.Completed(" ").Message);
			Assert.AreEqual(PhaseState.InProgress, PhaseStatus.InProgress("busy").State);
		}

		[TestMethod]
		public void OrchestrationPhases_ScriptTimeoutInStatusMethod_FailsThePhaseAndEndsTheScript()
		{
			int calls = 0;
			var runner = CreateRunner(
				Phase.Execute("spin-up", _ => PhaseStatus.Completed()).WithStatus(_ => { calls++; throw new ScriptTimeoutException(); }));

			Assert.ThrowsExactly<ScriptTimeoutException>(() => runner.Run());

			Assert.AreEqual(1, calls, "Expected a script timeout not to be retried.");
			Assert.AreEqual(PhaseState.Failed, runner.Statuses["spin-up"].State);
			Assert.AreEqual("The script timed out while retrieving the status of phase 'spin-up'.", runner.Statuses["spin-up"].FailureReason);
		}

		[TestMethod]
		public void OrchestrationPhases_ScriptTimeoutInExecution_FailsThePhaseAndEndsTheScript()
		{
			var runner = CreateRunner(Phase.Execute("spin-up", _ => throw new ScriptTimeoutException()));

			Assert.ThrowsExactly<ScriptTimeoutException>(() => runner.Run());

			Assert.AreEqual("The script timed out while executing phase 'spin-up'.", runner.Statuses["spin-up"].FailureReason);
		}

		[TestMethod]
		public void OrchestrationPhases_ScriptTimeoutWhileWaiting_FailsThePhaseAfterTheWait()
		{
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => PhaseStatus.InProgress()).WithStatus(_ => PhaseStatus.InProgress()),
				Phase.WaitFor("spin-up"),
				Phase.Execute("configure", _ => PhaseStatus.Completed()),
			};
			var runner = new PhaseRunner(phases, CreateContext(), sleep: _ => throw new ScriptTimeoutException());

			Assert.ThrowsExactly<ScriptTimeoutException>(() => runner.Run());

			Assert.AreEqual("The script timed out while waiting on phase 'spin-up'.", runner.Statuses["configure"].FailureReason);
		}

		[TestMethod]
		public void OrchestrationPhases_ScriptTimeoutThatCannotBeRecorded_StillEndsTheScript()
		{
			var runner = new PhaseRunner(new[] { Phase.Execute("spin-up", _ => throw new ScriptTimeoutException()) }, CreateContext(), new UnavailableStore());

			Assert.ThrowsExactly<ScriptTimeoutException>(() => runner.Run());
		}

		private sealed class UnavailableStore : IPhaseStatusStore
		{
			public IReadOnlyDictionary<string, PhaseStatus> Load() => new Dictionary<string, PhaseStatus>();

			public void Record(string key, PhaseStatus status, DateTimeOffset? executionStarted) => throw new InvalidOperationException("Storage unavailable.");
		}

		private static PhaseRunner CreateRunner(params OrchestrationPhase[] phases)
		{
			return CreateRunner(null, phases);
		}

		private static PhaseRunner CreateRunner(IReadOnlyDictionary<string, PhaseStatus> recorded, params OrchestrationPhase[] phases)
		{
			var clock = new FakeClock();
			return new PhaseRunner(phases, CreateContext(), new InMemoryPhaseStatusStore(recorded), clock.Advance, clock.Now);
		}

		private static OrchestrationPhaseContext CreateContext()
		{
			return new OrchestrationPhaseContext(null);
		}

		private sealed class FakeClock
		{
			private DateTimeOffset _now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

			public DateTimeOffset Now() => _now;

			public void Advance(TimeSpan duration) => _now += duration;
		}

		private sealed class LegacyScript : OrchestrationScript
		{
			public bool WasOrchestrated { get; private set; }

			public override void Orchestrate(IEngine engine)
			{
				WasOrchestrated = true;
			}

			public override IEnumerable<IOrchestrationParameters> GetParameters()
			{
				return Enumerable.Empty<IOrchestrationParameters>();
			}
		}

		private sealed class PhasedScript : OrchestrationScript
		{
			public List<string> Executed { get; } = new List<string>();

			public bool WasOrchestrated { get; private set; }

			public override void Orchestrate(IEngine engine)
			{
				WasOrchestrated = true;
			}

			public override IEnumerable<IOrchestrationParameters> GetParameters()
			{
				return Enumerable.Empty<IOrchestrationParameters>();
			}

			public override IEnumerable<OrchestrationPhase> GetPhases()
			{
				return new OrchestrationPhase[]
				{
					Phase.Execute("spin-up", SpinUp),
					Phase.Execute("configure", Configure),
				};
			}

			private PhaseStatus SpinUp(OrchestrationPhaseContext context)
			{
				Executed.Add("spin-up");
				return PhaseStatus.Completed();
			}

			private PhaseStatus Configure(OrchestrationPhaseContext context)
			{
				Executed.Add("configure");
				return PhaseStatus.Completed();
			}
		}

		private sealed class EmptyScript : OrchestrationScript
		{
			public override IEnumerable<IOrchestrationParameters> GetParameters()
			{
				return Enumerable.Empty<IOrchestrationParameters>();
			}
		}

		private sealed class PhasedDynamicScript : DynamicOrchestrationScript
		{
			public string Region { get; private set; }

			public override OrchestrationInputDefinition GetInputs(IEngine engine, OrchestrationInputValues providedValues)
			{
				return new OrchestrationInputBuilder()
					.AddGroup("General", general => general.AddText("Region", field => field.IsRequired = true))
					.Build();
			}

			public override IEnumerable<OrchestrationPhase> GetPhases()
			{
				return new OrchestrationPhase[]
				{
					Phase.Execute("spin-up", _ =>
					{
						Region = InputValues.GetString("General/Region");
						return PhaseStatus.Completed();
					}),
				};
			}
		}
	}
}
