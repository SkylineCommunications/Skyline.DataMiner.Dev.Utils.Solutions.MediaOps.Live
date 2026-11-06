namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests.Automation.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

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
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	using OrchestrationScriptInput = Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects.OrchestrationScriptInput;

	[TestClass]
	public sealed class OrchestrationGlobalNodeTests
	{
		private const string NodeId = "node-a";

		[TestMethod]
		public void OrchestrateNode_NodeFails_FailsTheGlobalScriptWithAReadableMessage()
		{
			var simulation = new MediaOpsLiveSimulation();
			var ev = SaveEvent(simulation.Api, "Script_Fail");
			var script = new GlobalScript(useTry: false);

			var output = script.OnRequestScriptInfoRequest(CreateEngine(simulation), CreateGlobalRequest(ev.ID));

			Assert.IsTrue(output.Data.TryGetValue(OrchestrationScriptConstants.ScriptOutputError, out string error));
			StringAssert.StartsWith(error, "Node 'Transport' failed: ");
			Assert.DoesNotContain(" at ", error, "Expected no stack trace.");
			Assert.IsFalse(script.ContinuedAfterNode);
		}

		[TestMethod]
		public void OrchestrateNode_NodeSucceeds_ContinuesTheGlobalScript()
		{
			var simulation = new MediaOpsLiveSimulation();
			var ev = SaveEvent(simulation.Api, "Script_Success");
			var script = new GlobalScript(useTry: false);

			var output = script.OnRequestScriptInfoRequest(CreateEngine(simulation), CreateGlobalRequest(ev.ID));

			Assert.IsFalse(output.Data.TryGetValue(OrchestrationScriptConstants.ScriptOutputError, out string error), error);
			Assert.IsTrue(script.ContinuedAfterNode);
		}

		[TestMethod]
		public void TryOrchestrateNode_NodeFails_ReportsTheFailureWithoutFailingTheGlobalScript()
		{
			var simulation = new MediaOpsLiveSimulation();
			var ev = SaveEvent(simulation.Api, "Script_Fail");
			var script = new GlobalScript(useTry: true);

			var output = script.OnRequestScriptInfoRequest(CreateEngine(simulation), CreateGlobalRequest(ev.ID));

			Assert.IsFalse(output.Data.TryGetValue(OrchestrationScriptConstants.ScriptOutputError, out string error), error);
			Assert.IsTrue(script.ContinuedAfterNode);
			Assert.IsTrue(script.Result.IsExecuted);
			Assert.IsFalse(script.Result.IsSuccess);
			Assert.AreEqual(NodeId, script.Result.NodeId);
			Assert.IsFalse(String.IsNullOrEmpty(script.Result.FailureReason));
		}

		[TestMethod]
		public void TryOrchestrateNode_RunsTheNodeScriptWithoutSuspending()
		{
			var simulation = new MediaOpsLiveSimulation();
			var ev = SaveEvent(simulation.Api, "Script_Success");
			var script = new GlobalScript(useTry: true);

			script.OnRequestScriptInfoRequest(CreateEngine(simulation), CreateGlobalRequest(ev.ID));

			Assert.IsTrue(script.Result.IsSuccess);
			var metadata = GetNodeScriptMetadata(simulation);
			Assert.AreEqual(Boolean.TrueString, metadata[OrchestrationScriptInternalInput.HoldWaitsMetadataKey]);
			Assert.AreEqual(NodeId, metadata[OrchestrationScriptInternalInput.NodeIdMetadataKey]);
			Assert.AreEqual(nameof(OrchestrationLevel.Node), metadata[OrchestrationScriptInternalInput.OrchestrationLevelMetadataKey]);
		}

		[TestMethod]
		public void TryOrchestrateNode_OutsideTheGlobalScriptOfAnEvent_DoesNotRunTheNode()
		{
			var simulation = new MediaOpsLiveSimulation();
			var ev = SaveEvent(simulation.Api, "Script_Success");
			var script = new GlobalScript(useTry: true) { Node = ev.Configuration.NodeConfigurations.Single() };
			int executedBefore = simulation.Dms.ExecutedScripts.Count;

			script.ExecuteOrchestration(CreateEngine(simulation), false);

			Assert.IsFalse(script.Result.IsExecuted);
			Assert.IsTrue(script.Result.IsSuccess);
			Assert.HasCount(executedBefore, simulation.Dms.ExecutedScripts);
		}

		[TestMethod]
		public void NodePhasesExecution_HoldingWaits_WaitsInProcessInsteadOfSchedulingAResume()
		{
			var simulation = new MediaOpsLiveSimulation();
			var jobInfoId = Guid.NewGuid();
			var eventId = Guid.NewGuid();
			var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
			var sleeps = new List<TimeSpan>();
			int polls = 0;
			var phases = new OrchestrationPhase[]
			{
				Phase.Execute("spin-up", _ => PhaseStatus.InProgress()).WithStatus(_ => polls++ < 2 ? PhaseStatus.InProgress() : PhaseStatus.Completed()),
				Phase.WaitFor("spin-up").PollEvery(TimeSpan.FromMinutes(1)),
				Phase.Execute("configure", _ => PhaseStatus.Completed()),
			};
			var store = new NodePhasesStatusStore(simulation.Api.Orchestration.NodePhases, jobInfoId, eventId, NodeId, phases, () => now);
			var scheduler = new OrchestrationResumeScheduler(simulation.Api.Connection, 123);

			var result = NodePhasesExecution.Run(
				phases,
				new OrchestrationPhaseContext(null),
				store,
				scheduler,
				eventId,
				resume: false,
				eventNodes: null,
				sleep: d => { sleeps.Add(d); now += d; },
				now: () => now,
				canSuspend: false);

			Assert.IsTrue(result.IsSuccess);
			Assert.IsFalse(result.IsSuspended);
			Assert.IsNotEmpty(sleeps);
			Assert.IsEmpty(simulation.Dms.GetAllDmsSchedulerTasks());
			Assert.IsNull(simulation.Api.Orchestration.NodePhases.GetNodePhases(eventId, NodeId).ResumeSchedulerReference);
		}

		private static IEngine CreateEngine(MediaOpsLiveSimulation simulation)
		{
			var engine = new Mock<IEngine>();
			engine.Setup(e => e.GetUserConnection()).Returns(simulation.Dms.CreateConnection());

			return engine.Object;
		}

		private static RequestScriptInfoInput CreateGlobalRequest(Guid eventId)
		{
			var input = new OrchestrationScriptInput();
			input.Metadata[OrchestrationScriptInternalInput.OrchestrationLevelMetadataKey] = nameof(OrchestrationLevel.Global);
			input.Metadata[OrchestrationScriptInternalInput.EventIdMetadataKey] = eventId.ToString();

			return new RequestScriptInfoInput
			{
				Data = new Dictionary<string, string>
				{
					[OrchestrationScriptConstants.OrchestrationScriptActionRequestScriptInfoKey] = nameof(OrchestrationScriptAction.PerformOrchestration),
					[OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey] = JsonConvert.SerializeObject(input),
				},
			};
		}

		private static Dictionary<string, string> GetNodeScriptMetadata(MediaOpsLiveSimulation simulation)
		{
			var data = simulation.Dms.ExecutedScripts
				.Where(x => x.ScriptName == "Script_Success")
				.Select(x => x.CustomEntryPoint?.Parameters?.OfType<RequestScriptInfoInput>().FirstOrDefault()?.Data)
				.Single(x => x != null && x[OrchestrationScriptConstants.OrchestrationScriptActionRequestScriptInfoKey] == nameof(OrchestrationScriptAction.PerformOrchestration));

			return JsonConvert.DeserializeObject<OrchestrationScriptInput>(data[OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey]).Metadata;
		}

		private static OrchestrationEventConfiguration SaveEvent(MediaOpsLiveApi api, string nodeScript)
		{
			var ev = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Global Event",
				GlobalOrchestrationScript = "Script_Success",
				Configuration =
				{
					NodeConfigurations =
					{
						new NodeConfiguration { NodeId = NodeId, NodeLabel = "Transport", OrchestrationScriptName = nodeScript },
					},
				},
			};

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJobConfiguration(job);

			return ev;
		}

		private sealed class GlobalScript : OrchestrationScript
		{
			private readonly bool _useTry;

			public GlobalScript(bool useTry)
			{
				_useTry = useTry;
			}

			public NodeConfiguration Node { get; set; }

			public NodeOrchestrationResult Result { get; private set; }

			public bool ContinuedAfterNode { get; private set; }

			public override void Orchestrate(IEngine engine)
			{
				NodeConfiguration node = Node ?? EventConfiguration.Configuration.NodeConfigurations.Single();

				if (_useTry)
				{
					Result = TryOrchestrateNode(node);
				}
				else
				{
					OrchestrateNode(node);
				}

				ContinuedAfterNode = true;
			}

			public override IEnumerable<IOrchestrationParameters> GetParameters() => Array.Empty<IOrchestrationParameters>();
		}
	}
}
