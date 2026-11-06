namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationEventPhases
	{
		private const string NodeA = "node-a";
		private const string NodeB = "node-b";

		private readonly Guid _jobInfoId = Guid.NewGuid();

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationEventPhases_GetEventPhases_ReadsSeveralEventsAtOnce()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var eventA = Guid.NewGuid();
			var eventB = Guid.NewGuid();
			var withoutPhases = Guid.NewGuid();
			SavePhases(api, eventA, NodeB, ("configure", 1, PhaseState.NotStarted), ("spin-up", 0, PhaseState.Completed));
			SavePhases(api, eventA, NodeA, ("go-live", 0, PhaseState.InProgress));
			SavePhases(api, eventB, NodeA, ("go-live", 0, PhaseState.Failed));

			var phases = api.Orchestration.GetEventPhases([eventA, eventB, withoutPhases]);

			CollectionAssert.AreEqual(new[] { "node-a/go-live", "node-b/spin-up", "node-b/configure" }, phases[eventA].Select(x => $"{x.NodeId}/{x.Key}").ToArray());
			Assert.IsTrue(phases[eventA].All(x => x.EventId == eventA));
			Assert.AreEqual(PhaseState.Failed, phases[eventB].Single().State);
			Assert.IsEmpty(phases[withoutPhases]);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationEventPhases_Subscription_ReportsTheCurrentPhasesOfTheChangedNode()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var eventId = Guid.NewGuid();
			var changes = new List<OrchestrationNodePhasesChange>();

			using var subscription = api.Orchestration.SubscribeOnEventPhases();
			subscription.Changed += (s, e) => changes.AddRange(e);

			SavePhases(api, eventId, NodeA, ("spin-up", 0, PhaseState.InProgress));
			SavePhases(api, eventId, NodeA, ("spin-up", 0, PhaseState.Completed), ("configure", 1, PhaseState.NotStarted));

			Assert.HasCount(2, changes);
			var last = changes.Last();
			Assert.AreEqual(eventId, last.EventId);
			Assert.AreEqual(NodeA, last.NodeId);
			Assert.IsFalse(last.IsRemoved);
			CollectionAssert.AreEqual(new[] { "spin-up:Completed", "configure:NotStarted" }, last.Phases.Select(x => $"{x.Key}:{x.State}").ToArray());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationEventPhases_Subscription_ReportsRemovedNodes()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var eventId = Guid.NewGuid();
			SavePhases(api, eventId, NodeA, ("spin-up", 0, PhaseState.Completed));
			var changes = new List<OrchestrationNodePhasesChange>();

			using var subscription = api.Orchestration.SubscribeOnEventPhases();
			subscription.Changed += (s, e) => changes.AddRange(e);

			api.Orchestration.NodePhases.DeleteForEvents([eventId]);

			Assert.IsTrue(changes.Single().IsRemoved);
			Assert.IsEmpty(changes.Single().Phases);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationEventPhases_SubscriptionForOneEvent_IgnoresOtherEvents()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var eventId = Guid.NewGuid();
			var changes = new List<OrchestrationNodePhasesChange>();

			using var subscription = api.Orchestration.SubscribeOnEventPhases(eventId);
			subscription.Changed += (s, e) => changes.AddRange(e);

			SavePhases(api, Guid.NewGuid(), NodeA, ("spin-up", 0, PhaseState.InProgress));
			SavePhases(api, eventId, NodeB, ("spin-up", 0, PhaseState.InProgress));

			Assert.AreEqual(NodeB, changes.Single().NodeId);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationEventPhases_Subscription_StopsReportingWhenDisposed()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			var changes = new List<OrchestrationNodePhasesChange>();

			var subscription = api.Orchestration.SubscribeOnEventPhases();
			subscription.Changed += (s, e) => changes.AddRange(e);
			subscription.Dispose();

			SavePhases(api, Guid.NewGuid(), NodeA, ("spin-up", 0, PhaseState.InProgress));

			Assert.IsEmpty(changes);
		}

		private void SavePhases(MediaOpsLiveApi api, Guid eventId, string nodeId, params (string Key, int Order, PhaseState State)[] phases)
		{
			var repository = api.Orchestration.NodePhases;
			var nodePhases = repository.GetOrCreateNodePhases(_jobInfoId, eventId, nodeId);

			nodePhases.Phases.Clear();
			foreach (var (key, order, state) in phases)
			{
				nodePhases.Phases.Add(new OrchestrationPhaseStatus { Key = key, Order = order, State = state, LastUpdated = DateTimeOffset.UtcNow });
			}

			repository.CreateOrUpdate(nodePhases);
		}
	}
}
