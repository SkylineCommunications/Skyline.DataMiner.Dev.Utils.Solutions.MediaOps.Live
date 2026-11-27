namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationPhaseStorage
	{
		private const string NodeA = "node-a";
		private const string NodeB = "node-b";

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_ContextValue_SetAndGet()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodeContextValues;
			var jobInfoId = Guid.NewGuid();

			repository.SetValue(jobInfoId, NodeA, "cloudServiceId", "svc-1");

			Assert.IsTrue(repository.TryGetValue(jobInfoId, NodeA, "cloudServiceId", out string value));
			Assert.AreEqual("svc-1", value);
			Assert.IsFalse(repository.TryGetValue(jobInfoId, NodeA, "unknown", out _));

			var values = repository.GetValues(jobInfoId, NodeA);
			Assert.HasCount(1, values);
			Assert.AreEqual("svc-1", values["cloudServiceId"]);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_ContextValue_OverwriteKeepsSingleInstance()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodeContextValues;
			var jobInfoId = Guid.NewGuid();

			repository.SetValue(jobInfoId, NodeA, "cloudServiceId", "svc-1");
			repository.SetValue(jobInfoId, NodeA, "cloudServiceId", "svc-2");

			Assert.AreEqual(1, repository.CountAll());
			Assert.IsTrue(repository.TryGetValue(jobInfoId, NodeA, "cloudServiceId", out string value));
			Assert.AreEqual("svc-2", value);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_ContextValue_IsolatedPerJobAndNode()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodeContextValues;
			var jobInfoId = Guid.NewGuid();
			var otherJobInfoId = Guid.NewGuid();

			repository.SetValue(jobInfoId, NodeA, "key", "a");
			repository.SetValue(jobInfoId, NodeB, "key", "b");
			repository.SetValue(otherJobInfoId, NodeA, "key", "other");

			Assert.AreEqual(3, repository.CountAll());
			Assert.AreEqual("a", repository.GetValues(jobInfoId, NodeA)["key"]);
			Assert.AreEqual("b", repository.GetValues(jobInfoId, NodeB)["key"]);
			Assert.AreEqual("other", repository.GetValues(otherJobInfoId, NodeA)["key"]);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_ContextValue_SetRemovesDuplicates()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodeContextValues;
			var jobInfoId = Guid.NewGuid();

			// Simulates two concurrent first writes of the same key.
			repository.CreateOrUpdate(
			[
				new OrchestrationNodeContextValue { JobInfoReference = jobInfoId, NodeId = NodeA, Key = "key", Value = "first" },
				new OrchestrationNodeContextValue { JobInfoReference = jobInfoId, NodeId = NodeA, Key = "key", Value = "second" },
			]);

			Assert.AreEqual(2, repository.CountAll());
			Assert.HasCount(1, repository.GetValues(jobInfoId, NodeA));

			repository.SetValue(jobInfoId, NodeA, "key", "third");

			Assert.AreEqual(1, repository.CountAll());
			Assert.AreEqual("third", repository.GetValues(jobInfoId, NodeA)["key"]);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_ContextValue_Remove()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodeContextValues;
			var jobInfoId = Guid.NewGuid();

			repository.SetValue(jobInfoId, NodeA, "key", "value");
			repository.RemoveValue(jobInfoId, NodeA, "key");

			Assert.AreEqual(0, repository.CountAll());
			Assert.IsFalse(repository.TryGetValue(jobInfoId, NodeA, "key", out _));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_ContextValue_Limits()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodeContextValues;
			var jobInfoId = Guid.NewGuid();

			var keyException = Assert.ThrowsExactly<ArgumentException>(
				() => repository.SetValue(jobInfoId, NodeA, new string('k', NodeContextValueRepository.MaxKeyLength + 1), "value"));
			StringAssert.Contains(keyException.Message, $"exceeds {NodeContextValueRepository.MaxKeyLength} characters");

			var valueException = Assert.ThrowsExactly<ArgumentException>(
				() => repository.SetValue(jobInfoId, NodeA, "cloudServiceId", new string('v', NodeContextValueRepository.MaxValueLength + 1)));
			StringAssert.Contains(valueException.Message, $"Value of key 'cloudServiceId' exceeds {NodeContextValueRepository.MaxValueLength} characters");

			for (int i = 0; i < NodeContextValueRepository.MaxKeysPerNode; i++)
			{
				repository.SetValue(jobInfoId, NodeA, $"key{i}", "value");
			}

			Assert.ThrowsExactly<InvalidOperationException>(() => repository.SetValue(jobInfoId, NodeA, "oneTooMany", "value"));

			// Overwriting an existing key and writing for another node remain possible.
			repository.SetValue(jobInfoId, NodeA, "key0", "updated");
			repository.SetValue(jobInfoId, NodeB, "key0", "value");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_NodePhases_SaveAndRead()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodePhases;
			var jobInfoId = Guid.NewGuid();
			var eventId = Guid.NewGuid();
			var now = DateTimeOffset.UtcNow;

			var nodePhases = repository.GetOrCreateNodePhases(jobInfoId, eventId, NodeA);
			nodePhases.PhaseLayoutFingerprint = "spin-up|wait:spin-up|configure";
			nodePhases.WaitStarted = now;
			nodePhases.Phases.Add(new OrchestrationPhaseStatus { Key = "spin-up", Order = 0, State = PhaseState.InProgress, Message = "Cloud service requested.", LastUpdated = now, Started = now });
			nodePhases.Phases.Add(new OrchestrationPhaseStatus { Key = "configure", Order = 1, State = PhaseState.NotStarted, Message = new string('m', 300), LastUpdated = now });
			repository.CreateOrUpdate(nodePhases);

			var stored = repository.GetNodePhases(eventId, NodeA);

			Assert.IsNotNull(stored);
			Assert.AreEqual(nodePhases.ID, stored.ID);
			Assert.AreEqual(jobInfoId, stored.JobInfoReference.ID);
			Assert.AreEqual("spin-up|wait:spin-up|configure", stored.PhaseLayoutFingerprint);
			Assert.HasCount(2, stored.Phases);

			var spinUp = stored.Phases.Single(x => x.Key == "spin-up");
			Assert.AreEqual(PhaseState.InProgress, spinUp.State);
			Assert.AreEqual("Cloud service requested.", spinUp.Message);

			var configure = stored.Phases.Single(x => x.Key == "configure");
			Assert.AreEqual(1, configure.Order);
			Assert.AreEqual(OrchestrationPhaseStatus.MaxMessageLength, configure.Message.Length);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_NodePhases_OverlappingEventsOfSameNodeAreIsolated()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodePhases;
			var jobInfoId = Guid.NewGuid();
			var prerollEventId = Guid.NewGuid();
			var startEventId = Guid.NewGuid();

			var preroll = repository.GetOrCreateNodePhases(jobInfoId, prerollEventId, NodeA);
			var start = repository.GetOrCreateNodePhases(jobInfoId, startEventId, NodeA);

			preroll.Phases.Add(new OrchestrationPhaseStatus { Key = "spin-up", State = PhaseState.InProgress, LastUpdated = DateTimeOffset.UtcNow });
			start.Phases.Add(new OrchestrationPhaseStatus { Key = "go-live", State = PhaseState.Completed, LastUpdated = DateTimeOffset.UtcNow });

			repository.CreateOrUpdate(preroll);
			repository.CreateOrUpdate(start);

			Assert.AreEqual(2, repository.CountAll());
			Assert.AreEqual("spin-up", repository.GetNodePhases(prerollEventId, NodeA).Phases.Single().Key);
			Assert.AreEqual("go-live", repository.GetNodePhases(startEventId, NodeA).Phases.Single().Key);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_NodePhases_GetForEvent()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodePhases;
			var jobInfoId = Guid.NewGuid();
			var eventId = Guid.NewGuid();

			repository.CreateOrUpdate(repository.GetOrCreateNodePhases(jobInfoId, eventId, NodeA));
			repository.CreateOrUpdate(repository.GetOrCreateNodePhases(jobInfoId, eventId, NodeB));
			repository.CreateOrUpdate(repository.GetOrCreateNodePhases(jobInfoId, Guid.NewGuid(), NodeA));

			var forEvent = repository.GetNodePhasesForEvent(eventId);

			Assert.HasCount(2, forEvent);
			Assert.IsTrue(forEvent.ContainsKey(NodeA));
			Assert.IsTrue(forEvent.ContainsKey(NodeB));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_NodePhases_GetOrCreateRemovesDuplicates()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodePhases;
			var jobInfoId = Guid.NewGuid();
			var eventId = Guid.NewGuid();

			repository.CreateOrUpdate(
			[
				new OrchestrationNodePhases { JobInfoReference = jobInfoId, EventReference = eventId, NodeId = NodeA },
				new OrchestrationNodePhases { JobInfoReference = jobInfoId, EventReference = eventId, NodeId = NodeA },
			]);

			Assert.AreEqual(2, repository.CountAll());

			var nodePhases = repository.GetOrCreateNodePhases(jobInfoId, eventId, NodeA);

			Assert.AreEqual(1, repository.CountAll());
			Assert.AreEqual(nodePhases.ID, repository.GetNodePhases(eventId, NodeA).ID);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_NodePhases_DuplicatePhaseKeyIsRejected()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var repository = api.Orchestration.NodePhases;

			var nodePhases = repository.GetOrCreateNodePhases(Guid.NewGuid(), Guid.NewGuid(), NodeA);
			nodePhases.Phases.Add(new OrchestrationPhaseStatus { Key = "spin-up", LastUpdated = DateTimeOffset.UtcNow });
			nodePhases.Phases.Add(new OrchestrationPhaseStatus { Key = "spin-up", LastUpdated = DateTimeOffset.UtcNow });

			var exception = Assert.ThrowsExactly<ArgumentException>(() => repository.CreateOrUpdate(nodePhases));
			StringAssert.Contains(exception.Message, "'spin-up'");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_DeleteJobRemovesPhasesAndContext()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var ev = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Test Event",
			};

			var job = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(ev);
			api.Orchestration.SaveOrchestrationJob(job);

			api.Orchestration.NodePhases.CreateOrUpdate(api.Orchestration.NodePhases.GetOrCreateNodePhases(job.JobInfo.ID, ev.ID, NodeA));
			api.Orchestration.NodeContextValues.SetValue(job.JobInfo.ID, NodeA, "key", "value");

			// Data of another job must survive.
			var otherJobInfoId = Guid.NewGuid();
			api.Orchestration.NodeContextValues.SetValue(otherJobInfoId, NodeA, "key", "value");

			api.Orchestration.DeleteJob(job);

			Assert.AreEqual(0, api.Orchestration.NodePhases.CountAll());
			Assert.AreEqual(1, api.Orchestration.NodeContextValues.CountAll());
			Assert.IsTrue(api.Orchestration.NodeContextValues.TryGetValue(otherJobInfoId, NodeA, "key", out _));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationPhaseStorage_RemovingEventRemovesItsPhases()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var keptEvent = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Kept Event",
			};

			var removedEvent = new OrchestrationEvent
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(2),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Removed Event",
			};

			var job = api.Orchestration.GetOrCreateNewOrchestrationJob(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(keptEvent);
			job.OrchestrationEvents.Add(removedEvent);
			api.Orchestration.SaveOrchestrationJob(job);

			var repository = api.Orchestration.NodePhases;
			repository.CreateOrUpdate(repository.GetOrCreateNodePhases(job.JobInfo.ID, keptEvent.ID, NodeA));
			repository.CreateOrUpdate(repository.GetOrCreateNodePhases(job.JobInfo.ID, removedEvent.ID, NodeA));

			job = api.Orchestration.GetOrCreateNewOrchestrationJob(job.JobId);
			job.OrchestrationEvents.RemoveAll(x => x.ID == removedEvent.ID);
			api.Orchestration.SaveOrchestrationJob(job);

			Assert.AreEqual(1, repository.CountAll());
			Assert.IsNotNull(repository.GetNodePhases(keptEvent.ID, NodeA));
			Assert.IsNull(repository.GetNodePhases(removedEvent.ID, NodeA));
		}
	}
}
