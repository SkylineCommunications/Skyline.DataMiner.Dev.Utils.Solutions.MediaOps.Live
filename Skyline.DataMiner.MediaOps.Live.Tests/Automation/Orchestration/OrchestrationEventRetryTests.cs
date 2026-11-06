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
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Plan;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	using OrchestrationScriptInput = Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects.OrchestrationScriptInput;

	[TestClass]
	public sealed class OrchestrationEventRetryTests
	{
		private const string NodeId = "node-a";

		[TestMethod]
		public void OrchestrationEventRetry_FailedEvent_RunsAgainAndCompletes()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEvent(api, out _);
			api.Orchestration.ExecuteEventsNow([ev.ID], api.GetMediaOpsPlanHelper());
			MarkFailed(api, ev.ID, "Phase 'spin-up' failed: Encoder offline.");
			int executedBefore = CountNodeScriptExecutions(simulation, resume: false);

			api.Orchestration.RetryEvents([ev.ID], api.GetMediaOpsPlanHelper());

			var stored = GetEvent(api, ev.ID);
			Assert.AreEqual(EventState.Completed, stored.EventState);
			Assert.IsTrue(String.IsNullOrEmpty(stored.FailureInfo), stored.FailureInfo);
			Assert.AreEqual(executedBefore + 1, CountNodeScriptExecutions(simulation, resume: false));
			Assert.AreEqual(0, CountNodeScriptExecutions(simulation, resume: true));
		}

		[TestMethod]
		public void OrchestrationEventRetry_KeepsTheFirstActualStartTime()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEvent(api, out _);
			api.Orchestration.ExecuteEventsNow([ev.ID], api.GetMediaOpsPlanHelper());
			var firstStart = GetEvent(api, ev.ID).ActualStartTime;

			api.Orchestration.RetryEvents([ev.ID], api.GetMediaOpsPlanHelper());

			Assert.IsNotNull(firstStart);
			Assert.AreEqual(firstStart, GetEvent(api, ev.ID).ActualStartTime);
		}

		[TestMethod]
		public void OrchestrationEventRetry_NodeScriptThatFailsAgain_FailsTheEvent()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEvent(api, out _, script: "Script_Fail");
			api.Orchestration.ExecuteEventsNow([ev.ID], api.GetMediaOpsPlanHelper());
			Assert.AreEqual(EventState.Failed, GetEvent(api, ev.ID).EventState);

			api.Orchestration.RetryEvents([ev.ID], api.GetMediaOpsPlanHelper());

			var stored = GetEvent(api, ev.ID);
			Assert.AreEqual(EventState.Failed, stored.EventState);
			Assert.IsFalse(String.IsNullOrEmpty(stored.FailureInfo));
		}

		[TestMethod]
		[DataRow(EventState.Draft)]
		[DataRow(EventState.Confirmed)]
		[DataRow(EventState.Cancelled)]
		public void OrchestrationEventRetry_EventThatDidNotRun_Throws(EventState state)
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEvent(api, out _);
			SetEventState(api, ev.ID, state);

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => api.Orchestration.RetryEvents([ev.ID], api.GetMediaOpsPlanHelper()));

			StringAssert.Contains(exception.Message, $"is {state} and cannot be retried");
			Assert.AreEqual(0, CountNodeScriptExecutions(simulation, resume: false));
		}

		[TestMethod]
		public void OrchestrationEventPhases_ReadingAnEvent_ExposesThePhasesOfAllNodes()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEvent(api, out string jobReference);
			var now = DateTimeOffset.UtcNow;

			var transport = api.Orchestration.NodePhases.GetOrCreateNodePhases(ev.JobInfoReference.Value.ID, ev.ID, "node-b");
			transport.Phases.Add(new OrchestrationPhaseStatus { Key = "configure", Order = 1, State = PhaseState.Failed, Message = "Encoder offline.", LastUpdated = now, Started = now, Ended = now });
			transport.Phases.Add(new OrchestrationPhaseStatus { Key = "spin-up", Order = 0, State = PhaseState.Completed, LastUpdated = now, Started = now, Ended = now });
			api.Orchestration.NodePhases.CreateOrUpdate(transport);

			var encoder = api.Orchestration.NodePhases.GetOrCreateNodePhases(ev.JobInfoReference.Value.ID, ev.ID, NodeId);
			encoder.Phases.Add(new OrchestrationPhaseStatus { Key = "go-live", Order = 0, State = PhaseState.InProgress, Message = "Starting.", LastUpdated = now, Started = now });
			api.Orchestration.NodePhases.CreateOrUpdate(encoder);

			var fromEvent = GetEvent(api, ev.ID).Phases;
			var fromJob = api.Orchestration.GetOrCreateNewOrchestrationJob(jobReference).OrchestrationEvents.Single().Phases;

			foreach (var phases in new[] { fromEvent, fromJob })
			{
				CollectionAssert.AreEqual(new[] { "node-a/go-live", "node-b/spin-up", "node-b/configure" }, phases.Select(x => $"{x.NodeId}/{x.Key}").ToArray());

				var failed = phases.Single(x => x.Key == "configure");
				Assert.AreEqual(PhaseState.Failed, failed.State);
				Assert.AreEqual("Encoder offline.", failed.FailureReason);
				Assert.AreEqual(1, failed.Order);

				var inProgress = phases.Single(x => x.Key == "go-live");
				Assert.AreEqual(PhaseState.InProgress, inProgress.State);
				Assert.AreEqual("Starting.", inProgress.Message);
				Assert.IsNull(inProgress.FailureReason);
				Assert.IsNull(inProgress.Ended);
			}
		}

		[TestMethod]
		public void OrchestrationEventPhases_EventWithoutPhases_IsEmpty()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var ev = SaveEvent(api, out _);

			Assert.IsEmpty(GetEvent(api, ev.ID).Phases);
			Assert.IsEmpty(new OrchestrationEventConfiguration().Phases);
		}

		[TestMethod]
		public void PhaseStatus_Failed_ExposesTheReason()
		{
			Assert.AreEqual("Encoder offline.", PhaseStatus.Failed("Encoder offline.").FailureReason);
			Assert.IsNull(PhaseStatus.InProgress("Encoder offline.").FailureReason);
		}

		[TestMethod]
		public void OrchestrationScript_FailedPhase_ReportsTheReasonWithoutAStackTrace()
		{
			var script = new FailingScript();

			var output = script.OnRequestScriptInfoRequest(Mock.Of<IEngine>(), CreatePerformOrchestrationRequest());

			Assert.AreEqual("Phase 'spin-up' failed: Encoder offline.", output.Data[OrchestrationScriptConstants.ScriptOutputError]);
		}

		[TestMethod]
		public void OrchestrationScript_UnexpectedException_KeepsTheDetails()
		{
			var script = new CrashingScript();

			var output = script.OnRequestScriptInfoRequest(Mock.Of<IEngine>(), CreatePerformOrchestrationRequest());

			StringAssert.Contains(output.Data[OrchestrationScriptConstants.ScriptOutputError], typeof(NotSupportedException).FullName);
		}

		private static RequestScriptInfoInput CreatePerformOrchestrationRequest()
		{
			return new RequestScriptInfoInput
			{
				Data = new Dictionary<string, string>
				{
					[OrchestrationScriptConstants.OrchestrationScriptActionRequestScriptInfoKey] = nameof(OrchestrationScriptAction.PerformOrchestration),
					[OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey] = JsonConvert.SerializeObject(new OrchestrationScriptInput()),
				},
			};
		}

		private static OrchestrationEventConfiguration SaveEvent(MediaOpsLiveApi api, out string jobReference, string script = "Script_Success")
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
						new NodeConfiguration { NodeId = NodeId, NodeLabel = "Encoder", OrchestrationScriptName = script },
					},
				},
			};

			jobReference = Guid.NewGuid().ToString();
			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(jobReference);
			job.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJobConfiguration(job);

			return ev;
		}

		private static void MarkFailed(MediaOpsLiveApi api, Guid eventId, string failureInfo)
		{
			var ev = GetEvent(api, eventId);
			ev.InternalSetState(EventState.Failed);
			ev.FailureInfo = failureInfo;
			new OrchestrationEventRepository(api).CreateOrUpdate(ev);
		}

		private static void SetEventState(MediaOpsLiveApi api, Guid eventId, EventState state)
		{
			var ev = GetEvent(api, eventId);
			ev.InternalSetState(state);
			new OrchestrationEventRepository(api).CreateOrUpdate(ev);
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
					&& data.TryGetValue(OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey, out var input)
					&& input.Contains(OrchestrationScriptInternalInput.ResumeMetadataKey) == resume);
		}

		private sealed class FailingScript : OrchestrationScript
		{
			public override IEnumerable<OrchestrationPhase> GetPhases()
			{
				yield return Phase.Execute("spin-up", _ => PhaseStatus.Failed("Encoder offline."));
			}

			public override IEnumerable<IOrchestrationParameters> GetParameters() => Array.Empty<IOrchestrationParameters>();
		}

		private sealed class CrashingScript : OrchestrationScript
		{
			public override void Orchestrate(IEngine engine) => throw new NotSupportedException("Unexpected.");

			public override IEnumerable<IOrchestrationParameters> GetParameters() => Array.Empty<IOrchestrationParameters>();
		}
	}
}
