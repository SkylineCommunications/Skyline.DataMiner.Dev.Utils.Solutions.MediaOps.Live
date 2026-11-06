namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests.Automation.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class OrchestrationCrossNodeWaitTests
	{
		private const string EncoderId = "node-encoder";
		private const string TransportId = "node-transport";

		private readonly FakeClock _clock = new FakeClock();
		private readonly Guid _jobInfoId = Guid.NewGuid();
		private readonly Guid _eventId = Guid.NewGuid();

		[TestMethod]
		public void CrossNodeWait_WaitsUntilTheOtherNodeStoresCompleted()
		{
			var nodes = new FakeEventNodes();
			nodes.Store(TransportId, "spin-up", PhaseStatus.InProgress());
			bool configured = false;

			var runner = CreateRunner(
				nodes,
				onSleep: () => nodes.Store(TransportId, "spin-up", PhaseStatus.Completed()),
				Phase.WaitFor("Transport", "spin-up").PollEvery(TimeSpan.FromSeconds(5)),
				Phase.Execute("configure", _ => { configured = true; return PhaseStatus.Completed(); }));

			var result = runner.Run();

			Assert.IsTrue(result.IsSuccess, result.FailureMessage);
			Assert.IsTrue(configured);
			Assert.IsFalse(runner.Statuses.ContainsKey("spin-up"), "Expected the status of another node never to be recorded for the current node.");
		}

		[TestMethod]
		public void CrossNodeWait_WhileWaiting_ReportsTheNodeOnThePhaseAfterTheWait()
		{
			var nodes = new FakeEventNodes();
			string messageWhileWaiting = null;
			PhaseRunner runner = null;

			runner = CreateRunner(
				nodes,
				onSleep: () =>
				{
					messageWhileWaiting = runner.Statuses["configure"].Message;
					nodes.Store(TransportId, "spin-up", PhaseStatus.Completed());
				},
				Phase.WaitFor("Transport", "spin-up").PollEvery(TimeSpan.FromSeconds(5)),
				Phase.Execute("configure", _ => PhaseStatus.Completed()));

			runner.Run();

			Assert.AreEqual("Waiting on phase 'spin-up' of node 'Transport'.", messageWhileWaiting);
		}

		[TestMethod]
		public void CrossNodeWait_PhaseThatNeverReported_TimesOutWithAnExplicitMessage()
		{
			var runner = CreateRunner(
				new FakeEventNodes(),
				onSleep: null,
				Phase.WaitFor("Transport", "spin-up").PollEvery(TimeSpan.FromSeconds(5)).Timeout(TimeSpan.FromSeconds(30)),
				Phase.Execute("configure", _ => PhaseStatus.Completed()));

			var result = runner.Run();

			Assert.AreEqual("Phase 'spin-up' of node 'Transport' never reported a status within '30 seconds'.", result.FailureMessage);
			Assert.AreEqual(PhaseState.Failed, runner.Statuses["configure"].State);
		}

		[TestMethod]
		public void CrossNodeWait_PhaseThatStaysInProgress_TimesOut()
		{
			var nodes = new FakeEventNodes();
			nodes.Store(TransportId, "spin-up", PhaseStatus.InProgress());

			var result = CreateRunner(
				nodes,
				onSleep: null,
				Phase.WaitFor("Transport", "spin-up").PollEvery(TimeSpan.FromSeconds(5)).Timeout(TimeSpan.FromSeconds(30))).Run();

			Assert.AreEqual("Phase 'spin-up' of node 'Transport' did not complete within '30 seconds'.", result.FailureMessage);
		}

		[TestMethod]
		public void CrossNodeWait_PhaseOfTheOtherNodeFails_FailsThePhaseAfterTheWait()
		{
			var nodes = new FakeEventNodes();
			nodes.Store(TransportId, "spin-up", PhaseStatus.Failed("Quota exceeded."));

			var runner = CreateRunner(
				nodes,
				onSleep: null,
				Phase.WaitFor("Transport", "spin-up"),
				Phase.Execute("configure", _ => PhaseStatus.Completed()));

			var result = runner.Run();

			Assert.AreEqual("Phase 'spin-up' of node 'Transport' failed: Quota exceeded.", result.FailureMessage);
			Assert.AreEqual(result.FailureMessage, runner.Statuses["configure"].Message);
		}

		[TestMethod]
		public void CrossNodeWait_StatusThatCannotBeRead_IsRetriedAndReportedOnThePhaseAfterTheWait()
		{
			var nodes = new FakeEventNodes { ReadError = new TimeoutException("DOM unavailable.") };

			var runner = CreateRunner(
				nodes,
				onSleep: null,
				Phase.WaitFor("Transport", "spin-up"),
				Phase.Execute("configure", _ => PhaseStatus.Completed()));

			var result = runner.Run();

			Assert.AreEqual(PhaseRunner.StatusAttempts, nodes.Reads);
			Assert.AreEqual("Unable to retrieve status of 'spin-up' phase of node 'Transport': DOM unavailable.", result.FailureMessage);
			Assert.AreEqual(PhaseState.Failed, runner.Statuses["configure"].State);
		}

		[TestMethod]
		public void CrossNodeWait_NodeThatIsNotPartOfTheEvent_FailsBeforeAnythingIsExecuted()
		{
			bool executed = false;
			var runner = CreateRunner(
				new FakeEventNodes(),
				onSleep: null,
				Phase.Execute("prepare", _ => { executed = true; return PhaseStatus.Completed(); }),
				Phase.WaitFor("Unknown", "spin-up"),
				Phase.Execute("configure", _ => PhaseStatus.Completed()));

			var result = runner.Run();

			Assert.IsFalse(executed);
			Assert.AreEqual("Unable to resolve the 'spin-up' phase of node 'Unknown': node 'Unknown' is not part of the event.", result.FailureMessage);
			Assert.AreEqual(PhaseState.Failed, runner.Statuses["configure"].State);
		}

		[TestMethod]
		public void CrossNodeWait_OutsideOfAnEvent_CannotBeResolved()
		{
			var runner = new PhaseRunner(
				new OrchestrationPhase[] { Phase.WaitFor("Transport", "spin-up") },
				new OrchestrationPhaseContext(null),
				sleep: _clock.Advance,
				now: _clock.Now);

			StringAssert.Contains(runner.Run().FailureMessage, "only possible when the script runs for a node of an event");
		}

		[TestMethod]
		public void CrossNodeWait_OnTheCurrentNode_UsesTheStatusMethodOfThePhase()
		{
			int statusCalls = 0;
			var runner = CreateRunner(
				new FakeEventNodes(),
				onSleep: null,
				Phase.Execute("spin-up", _ => PhaseStatus.InProgress()).WithStatus(_ => ++statusCalls == 1 ? PhaseStatus.NotStarted() : PhaseStatus.Completed()),
				Phase.WaitFor("Encoder", "spin-up"));

			Assert.IsTrue(runner.Run().IsSuccess);
			Assert.AreEqual(2, statusCalls);
		}

		[TestMethod]
		public void CrossNodeWait_LongPollingInterval_Suspends()
		{
			var runner = new PhaseRunner(
				new OrchestrationPhase[] { Phase.WaitFor("Transport", "spin-up").PollEvery(TimeSpan.FromMinutes(1)) },
				new OrchestrationPhaseContext(null),
				sleep: _clock.Advance,
				now: _clock.Now,
				canSuspend: true,
				eventNodes: new FakeEventNodes());

			Assert.IsTrue(runner.Run().IsSuspended);
		}

		[TestMethod]
		public void CrossNodeWait_Declaration_ValidatesArguments()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => Phase.WaitFor(null, "spin-up"));
			Assert.ThrowsExactly<ArgumentException>(() => Phase.WaitFor(" ", "spin-up"));
			Assert.ThrowsExactly<ArgumentException>(() => Phase.WaitFor("Transport", " "));
			Assert.AreEqual("Transport", Phase.WaitFor("Transport", "spin-up").Node);
			Assert.IsNull(Phase.WaitFor("spin-up").Node);
		}

		[TestMethod]
		public void CrossNodeWait_LayoutFingerprint_IncludesTheNode()
		{
			string fingerprint = NodePhasesStatusStore.GetLayoutFingerprint(new OrchestrationPhase[]
			{
				Phase.WaitFor("Transport", "spin-up"),
				Phase.Execute("configure", _ => PhaseStatus.Completed()),
			});

			Assert.AreEqual("wait:Transport/spin-up|configure", fingerprint);
		}

		[TestMethod]
		public void CrossNodeWait_StoredEventNodes_ResolvesByIdAndLabel()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var nodes = CreateStoredEventNodes(api, EncoderId);

			Assert.IsTrue(nodes.TryResolveNode("Transport", out var byLabel, out _));
			Assert.AreEqual(TransportId, byLabel.NodeId);
			Assert.IsFalse(byLabel.IsCurrentNode);

			Assert.IsTrue(nodes.TryResolveNode(EncoderId, out var byId, out _));
			Assert.IsTrue(byId.IsCurrentNode);

			Assert.IsFalse(nodes.TryResolveNode("Shared", out _, out string ambiguous));
			StringAssert.Contains(ambiguous, "more than one node");
		}

		[TestMethod]
		public void CrossNodeWait_EndToEnd_EncoderWaitsOnTheStoredStatusOfTheTransport()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodePhases;
			PhaseStatus transportStatus = PhaseStatus.InProgress("Provisioning.");

			var transportPhases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => PhaseStatus.InProgress()).WithStatus(_ => transportStatus),
			};

			RunNode(api, TransportId, transportPhases);
			Assert.AreEqual(PhaseState.InProgress, CreateStoredEventNodes(api, EncoderId).GetStoredStatus(TransportId, "spin-up").State);

			bool configured = false;
			var encoderPhases = new OrchestrationPhase[]
			{
				Phase.WaitFor("Transport", "spin-up").PollEvery(TimeSpan.FromSeconds(5)),
				Phase.Execute("configure", _ => { configured = true; return PhaseStatus.Completed(); }),
			};

			var encoderRunner = new PhaseRunner(
				encoderPhases,
				new OrchestrationPhaseContext(null),
				new NodePhasesStatusStore(repository, _jobInfoId, _eventId, EncoderId, encoderPhases, _clock.Now),
				sleep: d =>
				{
					_clock.Advance(d);
					transportStatus = PhaseStatus.Completed();
					RunNode(api, TransportId, transportPhases);
				},
				now: _clock.Now,
				eventNodes: CreateStoredEventNodes(api, EncoderId));

			Assert.IsTrue(encoderRunner.Run().IsSuccess);
			Assert.IsTrue(configured);

			var encoder = repository.GetNodePhases(_eventId, EncoderId);
			CollectionAssert.AreEqual(new[] { "configure" }, encoder.Phases.Select(x => x.Key).ToList(), "Expected the encoder not to store a status for the phase of the transport.");
		}

		private void RunNode(MediaOpsLiveApi api, string nodeId, IReadOnlyCollection<OrchestrationPhase> phases)
		{
			var store = new NodePhasesStatusStore(api.Orchestration.NodePhases, _jobInfoId, _eventId, nodeId, phases, _clock.Now);
			new PhaseRunner(phases, new OrchestrationPhaseContext(null), store, _clock.Advance, _clock.Now, eventNodes: CreateStoredEventNodes(api, nodeId)).Run();
		}

		private StoredEventNodes CreateStoredEventNodes(MediaOpsLiveApi api, string currentNodeId)
		{
			return new StoredEventNodes(
				api.Orchestration.NodePhases,
				_eventId,
				currentNodeId,
				new[]
				{
					new NodeConfiguration { NodeId = EncoderId, NodeLabel = "Encoder" },
					new NodeConfiguration { NodeId = TransportId, NodeLabel = "Transport" },
					new NodeConfiguration { NodeId = "node-shared-1", NodeLabel = "Shared" },
					new NodeConfiguration { NodeId = "node-shared-2", NodeLabel = "Shared" },
				});
		}

		private PhaseRunner CreateRunner(FakeEventNodes nodes, Action onSleep, params OrchestrationPhase[] phases)
		{
			return new PhaseRunner(
				phases,
				new OrchestrationPhaseContext(null),
				sleep: d =>
				{
					_clock.Advance(d);
					onSleep?.Invoke();
				},
				now: _clock.Now,
				eventNodes: nodes);
		}

		private sealed class FakeEventNodes : IEventNodes
		{
			private readonly Dictionary<(string, string), PhaseStatus> _stored = new Dictionary<(string, string), PhaseStatus>();

			public Exception ReadError { get; set; }

			public int Reads { get; private set; }

			public void Store(string nodeId, string phaseKey, PhaseStatus status) => _stored[(nodeId, phaseKey)] = status;

			public bool TryResolveNode(string reference, out EventNode node, out string failure)
			{
				switch (reference)
				{
					case "Transport":
						node = new EventNode(TransportId, "Transport", isCurrentNode: false);
						break;
					case "Encoder":
						node = new EventNode(EncoderId, "Encoder", isCurrentNode: true);
						break;
					default:
						node = null;
						failure = $"node '{reference}' is not part of the event";
						return false;
				}

				failure = null;
				return true;
			}

			public bool TryResolveJobNode(string reference, out EventNode node, out string failure) => TryResolveNode(reference, out node, out failure);

			public PhaseStatus GetStoredStatus(string nodeId, string phaseKey)
			{
				Reads++;

				if (ReadError != null)
				{
					throw ReadError;
				}

				return _stored.TryGetValue((nodeId, phaseKey), out var status) ? status : null;
			}
		}

		private sealed class FakeClock
		{
			private DateTimeOffset _now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

			public DateTimeOffset Now() => _now;

			public void Advance(TimeSpan duration) => _now += duration;
		}
	}
}
