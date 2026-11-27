namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests.Automation.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Threading;

	using Moq;
	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	using OrchestrationScriptInput = Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects.OrchestrationScriptInput;

	[TestClass]
	public sealed class PhaseExecutionGuardTests
	{
		private readonly Guid _jobInfoId = Guid.NewGuid();
		private readonly Guid _eventId = Guid.NewGuid();
		private readonly IEngine _engine = new Mock<IEngine>().Object;

		[TestMethod]
		public void PhaseExecutionGuard_SecondExecutionForTheSameNode_IsRefusedUntilTheFirstReleases()
		{
			Assert.IsTrue(PhaseExecutionGuard.TryAcquire(_engine, _jobInfoId, _eventId, "node-a", out var guard));
			Assert.IsFalse(PhaseExecutionGuard.TryAcquire(_engine, _jobInfoId, _eventId, "node-a", out var refused));
			Assert.IsNull(refused);

			guard.Dispose();

			Assert.IsTrue(PhaseExecutionGuard.TryAcquire(_engine, _jobInfoId, _eventId, "node-a", out var next));
			next.Dispose();
		}

		[TestMethod]
		public void PhaseExecutionGuard_OtherNodeOrEvent_IsNotBlocked()
		{
			using (Acquire(_jobInfoId, _eventId, "node-a"))
			using (Acquire(_jobInfoId, _eventId, "node-b"))
			using (Acquire(_jobInfoId, Guid.NewGuid(), "node-a"))
			{
			}
		}

		[TestMethod]
		public void PhaseExecutionGuard_OwnerThatDoesNotRelease_IsReleasedWhenTheLeaseExpires()
		{
			var locks = new PhaseExecutionGuard.InMemoryPhaseLocks();
			string lockId = PhaseExecutionGuard.GetName(_jobInfoId, _eventId, "node-a");

			Assert.IsTrue(PhaseExecutionGuard.TryAcquire(locks, lockId, TimeSpan.FromMilliseconds(50), out _));
			Assert.IsFalse(PhaseExecutionGuard.TryAcquire(locks, lockId, TimeSpan.FromMilliseconds(50), out _));

			Thread.Sleep(100);

			Assert.IsTrue(PhaseExecutionGuard.TryAcquire(locks, lockId, TimeSpan.FromMilliseconds(50), out var guard));
			guard.Dispose();
		}

		[TestMethod]
		public void PhaseExecutionGuard_ReleasedOnAnotherThread_DoesNotBlockTheNextExecution()
		{
			var guard = Acquire(_jobInfoId, _eventId, "node-a");

			var thread = new Thread(() => guard.Dispose());
			thread.Start();
			thread.Join();

			Acquire(_jobInfoId, _eventId, "node-a").Dispose();
		}

		[TestMethod]
		public void PhaseExecutionGuard_NodeIdWithSpecialCharacters_GivesAValidName()
		{
			string name = PhaseExecutionGuard.GetName(_jobInfoId, _eventId, @"Encoder\1 / ""main""");

			StringAssert.Matches(name, new System.Text.RegularExpressions.Regex(@"^[A-Za-z0-9.]+$"));
			Acquire(_jobInfoId, _eventId, @"Encoder\1 / ""main""").Dispose();
		}

		[TestMethod]
		public void PhaseExecutionGuard_Dispose_CanBeCalledTwice()
		{
			var guard = Acquire(_jobInfoId, _eventId, "node-a");

			guard.Dispose();
			guard.Dispose();
		}

		[TestMethod]
		public void PhaseExecutionGuard_NodeScriptWhosePhasesAreAlreadyRunning_ReportsSkippedInsteadOfAnError()
		{
			var simulation = new MediaOpsLiveSimulation();
			var ev = SaveEvent(simulation.Api);
			var engine = new Mock<IEngine>();
			engine.Setup(e => e.GetUserConnection()).Returns(simulation.Dms.CreateConnection());
			var script = new PhasedScript();

			RequestScriptInfoOutput output;
			using (Acquire(ev.JobInfoReference.Value.ID, ev.ID, "node-a"))
			{
				output = script.OnRequestScriptInfoRequest(engine.Object, CreateNodeRequest(ev.ID, "node-a"));
			}

			Assert.IsFalse(output.Data.ContainsKey(OrchestrationScriptConstants.ScriptOutputError));
			StringAssert.Contains(output.Data[OrchestrationScriptConstants.ScriptOutputSkipped], "already being executed");
			Assert.IsFalse(script.Executed);
		}

		private static OrchestrationEventConfiguration SaveEvent(MediaOpsLiveApi api)
		{
			var ev = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Guarded Event",
				Configuration =
				{
					NodeConfigurations =
					{
						new NodeConfiguration { NodeId = "node-a", NodeLabel = "Transport", OrchestrationScriptName = "Script_Success" },
					},
				},
			};

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJobConfiguration(job);

			return ev;
		}

		private static RequestScriptInfoInput CreateNodeRequest(Guid eventId, string nodeId)
		{
			var input = new OrchestrationScriptInput();
			input.Metadata[OrchestrationScriptInternalInput.OrchestrationLevelMetadataKey] = nameof(OrchestrationLevel.Node);
			input.Metadata[OrchestrationScriptInternalInput.EventIdMetadataKey] = eventId.ToString();
			input.Metadata[OrchestrationScriptInternalInput.NodeIdMetadataKey] = nodeId;

			return new RequestScriptInfoInput
			{
				Data = new Dictionary<string, string>
				{
					[OrchestrationScriptConstants.OrchestrationScriptActionRequestScriptInfoKey] = nameof(OrchestrationScriptAction.PerformOrchestration),
					[OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey] = JsonConvert.SerializeObject(input),
				},
			};
		}

		private PhaseExecutionGuard Acquire(Guid jobInfoId, Guid eventId, string nodeId)
		{
			Assert.IsTrue(PhaseExecutionGuard.TryAcquire(_engine, jobInfoId, eventId, nodeId, out var guard));
			return guard;
		}

		private sealed class PhasedScript : OrchestrationScript
		{
			public bool Executed { get; private set; }

			public override IEnumerable<OrchestrationPhase> GetPhases()
			{
				yield return Phase.Execute("spin-up", _ =>
				{
					Executed = true;
					return PhaseStatus.Completed();
				});
			}

			public override IEnumerable<IOrchestrationParameters> GetParameters() => Array.Empty<IOrchestrationParameters>();
		}
	}
}
