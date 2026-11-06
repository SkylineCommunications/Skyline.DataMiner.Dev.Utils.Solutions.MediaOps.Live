namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests.Automation.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases.Values;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class OrchestrationPhaseContextTests
	{
		private const string TransportId = "node-transport";
		private const string EncoderId = "node-encoder";
		private const string RecorderId = "node-recorder";
		private const int LocalAgentId = 123;

		private readonly FakeClock _clock = new FakeClock();
		private readonly Guid _jobInfoId = Guid.NewGuid();
		private readonly Guid _eventId = Guid.NewGuid();

		[TestMethod]
		public void PhaseContext_JobValues_ArePassedToTheFollowingPhases()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			string configuredWith = null;
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", ctx => { ctx.Job.Values.Set("cloudServiceId", "svc-1"); return PhaseStatus.Completed(); }),
				Phase.Execute("configure", ctx => { configuredWith = ctx.Job.Values.Get("cloudServiceId"); return PhaseStatus.Completed(); }),
			};

			Assert.IsTrue(RunNode(api, TransportId, phases).IsSuccess);
			Assert.AreEqual("svc-1", configuredWith);
		}

		[TestMethod]
		public void PhaseContext_JobValuesOutsideOfANodeOfAnEvent_AreNotAvailable()
		{
			var context = new OrchestrationPhaseContext(null);

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => context.Job.Values.Set("key", "value"));
			Assert.AreEqual(OrchestrationPhaseContext.NoNodeOfEvent, exception.Message);
			Assert.ThrowsExactly<InvalidOperationException>(() => context.Job.Values.ForNode("Transport"));
			Assert.IsNull(context.Node.Id);
		}

		[TestMethod]
		public void PhaseContext_JobValues_SurviveASuspensionAndAreReadAfterTheResume()
		{
			var simulation = new MediaOpsLiveSimulation();
			int polls = 0;
			string configuredWith = null;
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", ctx => { ctx.Job.Values.Set("cloudServiceId", "svc-1"); return PhaseStatus.InProgress(); })
					.WithStatus(ctx => ++polls switch { 1 => PhaseStatus.NotStarted(), 2 => PhaseStatus.InProgress(), _ => PhaseStatus.Completed() }),
				Phase.WaitFor("spin-up").PollEvery(TimeSpan.FromMinutes(1)),
				Phase.Execute("configure", ctx => { configuredWith = ctx.Job.Values.Get("cloudServiceId"); return PhaseStatus.Completed(); }),
			};

			Assert.IsTrue(RunNode(simulation.Api, TransportId, phases, resume: false).IsSuspended);

			_clock.Advance(TimeSpan.FromMinutes(1));
			Assert.IsTrue(RunNode(simulation.Api, TransportId, phases, resume: true).IsSuccess);

			Assert.AreEqual("svc-1", configuredWith);
		}

		[TestMethod]
		public void PhaseContext_JobValues_AreSharedBetweenTheEventsOfTheJob()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			string tornDown = null;

			RunNode(api, TransportId, new OrchestrationPhase[] { Phase.Execute("spin-up", ctx => { ctx.Job.Values.Set("cloudServiceId", "svc-1"); return PhaseStatus.Completed(); }) });
			RunNode(api, TransportId, new OrchestrationPhase[] { Phase.Execute("tear-down", ctx => { tornDown = ctx.Job.Values.Get("cloudServiceId"); return PhaseStatus.Completed(); }) }, eventId: Guid.NewGuid());

			Assert.AreEqual("svc-1", tornDown);
		}

		[TestMethod]
		public void PhaseContext_JobValuesOfAnotherNode_CanBeRead()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			string streamUrl = null;
			bool ownCopy = true;

			RunNode(api, TransportId, new OrchestrationPhase[] { Phase.Execute("spin-up", ctx => { ctx.Job.Values.Set("streamUrl", "srt://transport"); return PhaseStatus.Completed(); }) });
			RunNode(api, EncoderId, new OrchestrationPhase[]
			{
				Phase.Execute("configure", ctx =>
				{
					streamUrl = ctx.Job.Values.ForNode("Transport").Get("streamUrl");
					ownCopy = ctx.Job.Values.TryGet("streamUrl", out _);
					return PhaseStatus.Completed();
				}),
			});

			Assert.AreEqual("srt://transport", streamUrl);
			Assert.IsFalse(ownCopy, "Expected the values of another node not to be visible as values of the current node.");
		}

		[TestMethod]
		public void PhaseContext_JobValuesOfAnotherNode_AreReadOnly()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var context = CreateContext(api, EncoderId);

			Assert.IsNotInstanceOfType<IOrchestrationWritableValues>(context.Job.Values.ForNode("Transport"));
		}

		[TestMethod]
		public void PhaseContext_JobValuesOfANodeThatIsNotPartOfTheJob_CannotBeRead()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var context = CreateContext(api, EncoderId);

			var exception = Assert.ThrowsExactly<ArgumentException>(() => context.Job.Values.ForNode("Unknown"));
			StringAssert.Contains(exception.Message, "node 'Unknown' is not part of the job");
		}

		[TestMethod]
		public void PhaseContext_JobValuesOfANodeOfAnotherEvent_CanBeReadById()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			string recording = null;

			RunNode(api, RecorderId, new OrchestrationPhase[] { Phase.Execute("record", ctx => { ctx.Job.Values.Set("recordingId", "rec-1"); return PhaseStatus.Completed(); }) }, eventId: Guid.NewGuid());
			RunNode(api, EncoderId, new OrchestrationPhase[] { Phase.Execute("configure", ctx => { recording = ctx.Job.Values.ForNode(RecorderId).Get("recordingId"); return PhaseStatus.Completed(); }) });

			Assert.AreEqual("rec-1", recording);
		}

		[TestMethod]
		public void PhaseContext_LabelOfANodeOfAnotherEvent_IsNotAccepted()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var context = CreateContext(api, EncoderId);

			var exception = Assert.ThrowsExactly<ArgumentException>(() => context.Job.Values.ForNode("Recorder"));
			StringAssert.Contains(exception.Message, "node 'Recorder' is not part of the job");
		}

		[TestMethod]
		public void PhaseContext_JobValues_SupportGetTryGetRemoveAndKeys()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			IOrchestrationWritableValues values = CreateContext(api, TransportId).Job.Values;

			values.Set("a", "1");
			values.Set("b", "2");
			values.Set("a", "3");

			CollectionAssert.AreEquivalent(new[] { "a", "b" }, values.Keys.ToList());
			Assert.AreEqual("3", values.Get("a"));
			Assert.IsTrue(values.Remove("a"));
			Assert.IsFalse(values.Remove("a"));
			Assert.IsFalse(values.TryGet("a", out _));
			Assert.ThrowsExactly<KeyNotFoundException>(() => values.Get("a"));
		}

		[TestMethod]
		public void PhaseContext_JobValues_EnforceTheLimits()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			IOrchestrationWritableValues values = CreateContext(api, TransportId).Job.Values;

			var tooLongKey = Assert.ThrowsExactly<ArgumentException>(() => values.Set(new string('k', NodeContextValueRepository.MaxKeyLength + 1), "value"));
			StringAssert.Contains(tooLongKey.Message, $"exceeds {NodeContextValueRepository.MaxKeyLength} characters");

			var tooLongValue = Assert.ThrowsExactly<ArgumentException>(() => values.Set("cloudServiceId", new string('v', NodeContextValueRepository.MaxValueLength + 1)));
			StringAssert.Contains(tooLongValue.Message, "Value of key 'cloudServiceId' exceeds 4096 characters");

			Assert.ThrowsExactly<ArgumentNullException>(() => values.Set("key", null));

			for (int i = 0; i < NodeContextValueRepository.MaxKeysPerNode; i++)
			{
				values.Set($"key{i}", "value");
			}

			Assert.ThrowsExactly<InvalidOperationException>(() => values.Set("oneTooMany", "value"));
		}

		[TestMethod]
		public void PhaseContext_ValueThatExceedsALimit_FailsThePhase()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", ctx => { ctx.Job.Values.Set("cloudServiceId", new string('v', 5000)); return PhaseStatus.Completed(); }),
			};

			var result = RunNode(api, TransportId, phases);

			StringAssert.Contains(result.FailureMessage, "Value of key 'cloudServiceId' exceeds 4096 characters");
		}

		[TestMethod]
		public void PhaseContext_Event_ExposesTheNodesAndConnections()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var context = CreateContext(api, EncoderId);

			Assert.AreNotEqual(Guid.Empty, context.Event.Id);
			Assert.AreEqual("Preroll Start", context.Event.Name);
			Assert.AreEqual(EventType.PrerollStart, context.Event.Type);

			var transport = context.Event.Nodes.Single(node => node.Label == "Transport");
			Assert.AreEqual(TransportId, transport.Id);
			Assert.AreEqual("Transport Script", transport.OrchestrationScriptName);

			var connection = context.Event.Connections.Single();
			Assert.AreEqual(TransportId, connection.SourceNodeId);
			Assert.AreEqual(EncoderId, connection.DestinationNodeId);

			Assert.AreEqual("Transport", context.Event.Nodes.Single(node => node.Id == connection.SourceNodeId).Label);
		}

		[TestMethod]
		public void PhaseContext_Job_ExposesTheJobInfo()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var context = CreateContext(api, EncoderId);

			Assert.AreEqual("JOB-1", context.Job.Reference);
			Assert.AreEqual("Cloud Job", context.Job.Name);
			Assert.AreEqual(default, context.Job.MonitoringService);
		}

		[TestMethod]
		public void PhaseContext_OutsideOfAnEvent_HasNoEventAndNoJob()
		{
			var context = new OrchestrationPhaseContext(null);

			Assert.IsNull(context.Event);
			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => context.Job.Reference);
			Assert.AreEqual(OrchestrationJobContext.NoEvent, exception.Message);
		}

		[TestMethod]
		public void PhaseContext_Node_ExposesTheIdAndLabel()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var context = CreateContext(api, TransportId);

			Assert.AreEqual(TransportId, context.Node.Id);
			Assert.AreEqual("Transport", context.Node.Label);
		}

		private PhaseRunResult RunNode(MediaOpsLiveApi api, string nodeId, IReadOnlyCollection<OrchestrationPhase> phases, bool resume = false, Guid? eventId = null)
		{
			Guid id = eventId ?? _eventId;
			var store = new NodePhasesStatusStore(api.Orchestration.NodePhases, _jobInfoId, id, nodeId, phases, _clock.Now);

			return NodePhasesExecution.Run(
				phases,
				CreateContext(api, nodeId, id),
				store,
				new OrchestrationResumeScheduler(api.Connection, LocalAgentId),
				id,
				resume,
				CreateEventNodes(api, nodeId, id),
				_clock.Advance,
				_clock.Now);
		}

		// Mirrors how the orchestration script builds the context for a node of an event.
		private OrchestrationPhaseContext CreateContext(MediaOpsLiveApi api, string nodeId, Guid? eventId = null)
		{
			var eventNodes = CreateEventNodes(api, nodeId, eventId ?? _eventId);

			return new OrchestrationPhaseContext(
				null,
				new OrchestrationJobContext(
					() => new OrchestrationJobInfo { JobReference = "JOB-1", JobName = "Cloud Job" },
					OrchestrationJobValues.Create(api.Orchestration.NodeContextValues, _jobInfoId, nodeId, eventNodes)),
				new OrchestrationNodeContext(nodeId, nodeId == TransportId ? "Transport" : "Encoder"),
				OrchestrationEventContext.From(CreateEvent()));
		}

		private static OrchestrationEventConfiguration CreateEvent()
		{
			return new OrchestrationEventConfiguration
			{
				Name = "Preroll Start",
				EventType = EventType.PrerollStart,
				Configuration =
				{
					NodeConfigurations =
					{
						new NodeConfiguration { NodeId = TransportId, NodeLabel = "Transport", OrchestrationScriptName = "Transport Script" },
						new NodeConfiguration { NodeId = EncoderId, NodeLabel = "Encoder" },
					},
					Connections =
					{
						new Connection { SourceNodeId = TransportId, DestinationNodeId = EncoderId },
					},
				},
			};
		}

		private static StoredEventNodes CreateEventNodes(MediaOpsLiveApi api, string nodeId, Guid eventId)
		{
			return new StoredEventNodes(
				api.Orchestration.NodePhases,
				eventId,
				nodeId,
				new[]
				{
					new NodeConfiguration { NodeId = TransportId, NodeLabel = "Transport" },
					new NodeConfiguration { NodeId = EncoderId, NodeLabel = "Encoder" },
				},
				() => new[]
				{
					new NodeConfiguration { NodeId = TransportId, NodeLabel = "Transport" },
					new NodeConfiguration { NodeId = EncoderId, NodeLabel = "Encoder" },
					new NodeConfiguration { NodeId = RecorderId, NodeLabel = "Recorder" },
				});
		}

		private sealed class FakeClock
		{
			private DateTimeOffset _now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

			public DateTimeOffset Now() => _now;

			public void Advance(TimeSpan duration) => _now += duration;
		}
	}
}
