namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
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

			var destinationVsg = api.VirtualSignalGroups.ReadSingle("Destination 1");

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

			var destinationVsg = api.VirtualSignalGroups.ReadSingle("Destination 1");

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

			var destinationVsg = api.VirtualSignalGroups.ReadSingle("Destination 1");

			api.VirtualSignalGroups.SetJobInfo(destinationVsg, "JobRef-3", "Job Three", "Description of job three");

			// Act
			api.VirtualSignalGroups.ClearJobInfo([destinationVsg]);

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

			var destinationVsg = api.VirtualSignalGroups.ReadSingle("Destination 1");

			// Act - set job info without ever locking the VSG
			api.VirtualSignalGroups.SetJobInfo(destinationVsg, "JobRef-4", "Job Four", "Description of job four");

			// Assert - job info is stored and the VSG remains unlocked
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			Assert.AreEqual(LockState.Unlocked, state.LockState);
			Assert.IsTrue(state.HasJobInfo);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupJobInfo_ControlSurfaceTemplateBuildsJobDetailsUrl()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			// Store the Control Surface configuration (enabled + URL template).
			var settings = new ControlSurfaceSettings
			{
				JobDetailsEnabled = true,
				JobDetailsUrlTemplate = $"https://control-surface.example.com/jobs/{ControlSurfaceSettings.JobReferencePlaceholder}/details",
			};
			api.ControlSurfaceSettings.CreateOrUpdate(settings);

			var destinationVsg = api.VirtualSignalGroups.ReadSingle("Destination 1");
			api.VirtualSignalGroups.SetJobInfo(destinationVsg, "JobRef-5", "Job Five", "Description of job five");

			// Act - read back the stored settings and build the URL from the VSG's stored job reference.
			var storedSettings = api.ControlSurfaceSettings.GetOrCreate();
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			var resolvedUrl = storedSettings.ResolveJobDetailsUrl(state.JobReference);

			// Assert
			Assert.IsTrue(storedSettings.JobDetailsEnabled);
			Assert.AreEqual("https://control-surface.example.com/jobs/JobRef-5/details", resolvedUrl);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupJobInfo_ControlSurfaceDisabledDoesNotBuildJobDetailsUrl()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			// Store the Control Surface configuration with a template but the feature disabled.
			var settings = new ControlSurfaceSettings
			{
				JobDetailsEnabled = false,
				JobDetailsUrlTemplate = $"https://control-surface.example.com/jobs/{ControlSurfaceSettings.JobReferencePlaceholder}/details",
			};
			api.ControlSurfaceSettings.CreateOrUpdate(settings);

			var destinationVsg = api.VirtualSignalGroups.ReadSingle("Destination 1");
			api.VirtualSignalGroups.SetJobInfo(destinationVsg, "JobRef-6", "Job Six", "Description of job six");

			// Act - read back the stored settings and attempt to build the URL.
			var storedSettings = api.ControlSurfaceSettings.GetOrCreate();
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			var resolvedUrl = storedSettings.ResolveJobDetailsUrl(state.JobReference);

			// Assert - no URL is built while the feature is disabled.
			Assert.IsFalse(storedSettings.JobDetailsEnabled);
			Assert.IsNull(resolvedUrl);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupJobInfo_ControlSurfaceDoesNotBuildUrlWhenVsgHasNoJobInfo()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			// Store the Control Surface configuration (enabled + URL template).
			var settings = new ControlSurfaceSettings
			{
				JobDetailsEnabled = true,
				JobDetailsUrlTemplate = $"https://control-surface.example.com/jobs/{ControlSurfaceSettings.JobReferencePlaceholder}/details",
			};
			api.ControlSurfaceSettings.CreateOrUpdate(settings);

			// VSG without any job info set.
			var destinationVsg = api.VirtualSignalGroups.ReadSingle("Destination 1");

			// Act - attempt to build the URL from a VSG that has no stored job reference.
			var storedSettings = api.ControlSurfaceSettings.GetOrCreate();
			api.VirtualSignalGroupStates.TryGetByVirtualSignalGroup(destinationVsg, out var state);
			var jobReference = state?.JobReference;
			var resolvedUrl = storedSettings.ResolveJobDetailsUrl(jobReference);

			// Assert - no job reference means no URL is built.
			Assert.IsFalse(state?.HasJobInfo ?? false);
			Assert.IsNull(jobReference);
			Assert.IsNull(resolvedUrl);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupJobInfo_ControlSurfaceDoesNotBuildUrlAfterJobInfoCleared()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			// Store the Control Surface configuration (enabled + URL template).
			var settings = new ControlSurfaceSettings
			{
				JobDetailsEnabled = true,
				JobDetailsUrlTemplate = $"https://control-surface.example.com/jobs/{ControlSurfaceSettings.JobReferencePlaceholder}/details",
			};
			api.ControlSurfaceSettings.CreateOrUpdate(settings);

			var destinationVsg = api.VirtualSignalGroups.ReadSingle("Destination 1");
			api.VirtualSignalGroups.SetJobInfo(destinationVsg, "JobRef-7", "Job Seven", "Description of job seven");

			// Act - clear the job info and then attempt to build the URL.
			api.VirtualSignalGroups.ClearJobInfo([destinationVsg]);

			var storedSettings = api.ControlSurfaceSettings.GetOrCreate();
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			var resolvedUrl = storedSettings.ResolveJobDetailsUrl(state.JobReference);

			// Assert - the cleared job reference means no URL is built.
			Assert.IsFalse(state.HasJobInfo);
			Assert.IsNull(state.JobReference);
			Assert.IsNull(resolvedUrl);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupJobInfo_ControlSurfaceTemplateReplacesAllPlaceholderOccurrences()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			// Store the Control Surface configuration with the placeholder appearing more than once.
			var settings = new ControlSurfaceSettings
			{
				JobDetailsEnabled = true,
				JobDetailsUrlTemplate = $"https://control-surface.example.com/{ControlSurfaceSettings.JobReferencePlaceholder}/jobs/{ControlSurfaceSettings.JobReferencePlaceholder}/details",
			};
			api.ControlSurfaceSettings.CreateOrUpdate(settings);

			var destinationVsg = api.VirtualSignalGroups.ReadSingle("Destination 1");
			api.VirtualSignalGroups.SetJobInfo(destinationVsg, "JobRef-8", "Job Eight", "Description of job eight");

			// Act - read back the stored settings and build the URL from the VSG's stored job reference.
			var storedSettings = api.ControlSurfaceSettings.GetOrCreate();
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			var resolvedUrl = storedSettings.ResolveJobDetailsUrl(state.JobReference);

			// Assert - every placeholder occurrence is replaced.
			Assert.AreEqual("https://control-surface.example.com/JobRef-8/jobs/JobRef-8/details", resolvedUrl);
		}
	}
}
