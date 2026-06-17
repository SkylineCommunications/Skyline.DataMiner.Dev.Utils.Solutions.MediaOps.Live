namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_VirtualSignalGroupJobInfo
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupJobInfo_SetJobInfoStoresJobInfo()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var destinationVsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Destination 1");

			// Act
			api.VirtualSignalGroups.SetJobInfo(destinationVsg, "JobRef-1", "Job One", "Description of job one");

			// Assert
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			Assert.IsTrue(state.HasJobInfo);
			Assert.AreEqual("JobRef-1", state.JobReference);
			Assert.AreEqual("Job One", state.JobName);
			Assert.AreEqual("Description of job one", state.JobDescription);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupJobInfo_JobInfoPersistsAfterUnlock()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var destinationVsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Destination 1");

			api.VirtualSignalGroups.LockVirtualSignalGroup(destinationVsg, "Orchestration Engine", "Locked for job", "JobRef-2");
			api.VirtualSignalGroups.SetJobInfo(destinationVsg, "JobRef-2", "Job Two", "Description of job two");

			// Act - manually unlock the VSG
			api.VirtualSignalGroups.UnlockVirtualSignalGroup(destinationVsg);

			// Assert - the job info should remain even though the VSG is unlocked
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			Assert.AreEqual(LockState.Unlocked, state.LockState);
			Assert.IsTrue(state.HasJobInfo);
			Assert.AreEqual("JobRef-2", state.JobReference);
			Assert.AreEqual("Job Two", state.JobName);
			Assert.AreEqual("Description of job two", state.JobDescription);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupJobInfo_ClearJobInfoRemovesJobInfo()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var destinationVsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Destination 1");

			api.VirtualSignalGroups.SetJobInfo(destinationVsg, "JobRef-3", "Job Three", "Description of job three");

			// Act
			api.VirtualSignalGroups.ClearJobInfo(new[] { destinationVsg });

			// Assert
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			Assert.IsFalse(state.HasJobInfo);
			Assert.IsNull(state.JobReference);
			Assert.IsNull(state.JobName);
			Assert.IsNull(state.JobDescription);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupJobInfo_SetJobInfoIsIndependentOfLockState()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var destinationVsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Destination 1");

			// Act - set job info without ever locking the VSG
			api.VirtualSignalGroups.SetJobInfo(destinationVsg, "JobRef-4", "Job Four", "Description of job four");

			// Assert - job info is stored and the VSG remains unlocked
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			Assert.AreEqual(LockState.Unlocked, state.LockState);
			Assert.IsTrue(state.HasJobInfo);
		}
	}
}
