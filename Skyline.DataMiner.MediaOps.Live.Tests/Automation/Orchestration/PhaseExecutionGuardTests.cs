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

		[TestMethod]
		public void PhaseExecutionGuard_SecondExecutionForTheSameNode_IsRefusedUntilTheFirstReleases()
		{
			using (var holder = new GuardHolder(_jobInfoId, _eventId, "node-a"))
			{
				Assert.IsTrue(holder.Acquired);
				Assert.IsFalse(TryAcquireOnOtherThread(_jobInfoId, _eventId, "node-a"));
			}

			Assert.IsTrue(TryAcquireOnOtherThread(_jobInfoId, _eventId, "node-a"));
		}

		[TestMethod]
		public void PhaseExecutionGuard_OtherNodeOrEvent_IsNotBlocked()
		{
			using (new GuardHolder(_jobInfoId, _eventId, "node-a"))
			{
				Assert.IsTrue(TryAcquireOnOtherThread(_jobInfoId, _eventId, "node-b"));
				Assert.IsTrue(TryAcquireOnOtherThread(_jobInfoId, Guid.NewGuid(), "node-a"));
			}
		}

		[TestMethod]
		public void PhaseExecutionGuard_OwnerThatEndsWithoutReleasing_DoesNotBlockTheNextExecution()
		{
			var thread = new Thread(() => PhaseExecutionGuard.TryAcquire(_jobInfoId, _eventId, "node-a", out _));
			thread.Start();
			thread.Join();

			Assert.IsTrue(TryAcquireOnOtherThread(_jobInfoId, _eventId, "node-a"));
		}

		[TestMethod]
		public void PhaseExecutionGuard_NodeIdWithSpecialCharacters_GivesAValidName()
		{
			string name = PhaseExecutionGuard.GetName(_jobInfoId, _eventId, @"Encoder\1 / ""main""");

			Assert.IsFalse(name.Substring(@"Local\".Length).Contains(@"\"));
			Assert.IsTrue(PhaseExecutionGuard.TryAcquire(_jobInfoId, _eventId, @"Encoder\1 / ""main""", out var guard));
			guard.Dispose();
		}

		[TestMethod]
		public void PhaseExecutionGuard_Dispose_CanBeCalledTwice()
		{
			Assert.IsTrue(PhaseExecutionGuard.TryAcquire(_jobInfoId, _eventId, "node-a", out var guard));

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
			using (new GuardHolder(ev.JobInfoReference.Value.ID, ev.ID, "node-a"))
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

		private static bool TryAcquireOnOtherThread(Guid jobInfoId, Guid eventId, string nodeId)
		{
			bool acquired = false;
			var thread = new Thread(() =>
			{
				acquired = PhaseExecutionGuard.TryAcquire(jobInfoId, eventId, nodeId, out var guard);
				guard?.Dispose();
			});

			thread.Start();
			thread.Join();
			return acquired;
		}

		// Holds the guard on a dedicated thread, since a mutex must be released by the thread that owns it.
		private sealed class GuardHolder : IDisposable
		{
			private readonly ManualResetEventSlim _acquired = new ManualResetEventSlim();
			private readonly ManualResetEventSlim _release = new ManualResetEventSlim();
			private readonly Thread _thread;

			public GuardHolder(Guid jobInfoId, Guid eventId, string nodeId)
			{
				_thread = new Thread(() =>
				{
					Acquired = PhaseExecutionGuard.TryAcquire(jobInfoId, eventId, nodeId, out var guard);
					_acquired.Set();
					_release.Wait();
					guard?.Dispose();
				});

				_thread.Start();
				_acquired.Wait();
			}

			public bool Acquired { get; private set; }

			public void Dispose()
			{
				_release.Set();
				_thread.Join();
				_acquired.Dispose();
				_release.Dispose();
			}
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
