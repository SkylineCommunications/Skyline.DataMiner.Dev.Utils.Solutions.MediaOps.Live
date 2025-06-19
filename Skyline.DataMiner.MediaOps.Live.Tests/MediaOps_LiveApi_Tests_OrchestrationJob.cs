namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationJob
	{
		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_CheckDeleteBeforeUpdate()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var job = api.Orchestration.GetOrCreateNewOrchestrationJob("dd2cd5f2-ee7d-42b8-9b96-1e562d472b63");

			var guids = job.OrchestrationEvents.Select(ev => ev.ID).ToList();
			var toRemove = guids[0];
			var eventToRemove = job.OrchestrationEvents.FirstOrDefault(ev => ev.ID == toRemove);

			// Check events created is 2
			Assert.AreEqual(10, job.OrchestrationEvents.Count);

			// Check events is 1 after removal
			job.OrchestrationEvents.Remove(eventToRemove);
			Assert.AreEqual(9, job.OrchestrationEvents.Count);

			// Check 1 event identified as deleted
			Assert.AreEqual(1, job.RemovedIds.Count());
			Assert.AreEqual(toRemove, job.RemovedIds.FirstOrDefault());
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJobConfiguration_CheckDeleteBeforeUpdate()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration("dd2cd5f2-ee7d-42b8-9b96-1e562d472b63");

			var guids = job.OrchestrationEvents.Select(ev => ev.ID).ToList();
			var toRemove = guids[0];
			var eventToRemove = job.OrchestrationEvents.FirstOrDefault(ev => ev.ID == toRemove);

			// Check events created is 2
			Assert.AreEqual(10, job.OrchestrationEvents.Count);

			// Check events is 1 after removal
			job.OrchestrationEvents.Remove(eventToRemove);
			Assert.AreEqual(9, job.OrchestrationEvents.Count);

			// Check 1 event identified as deleted
			Assert.AreEqual(1, job.RemovedIds.Count());
			Assert.AreEqual(toRemove, job.RemovedIds.FirstOrDefault());
		}
	}
}
