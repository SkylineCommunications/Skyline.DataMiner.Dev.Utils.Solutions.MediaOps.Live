namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_VirtualSignalGroups
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_GetByEndpoints()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Query().First(x => x.Name == "Video Source 1");
			var videoSource2 = api.Endpoints.Query().First(x => x.Name == "Video Source 2");

			var vsgs = api.VirtualSignalGroups.GetByEndpoints([videoSource1, videoSource2]).ToList();

			Assert.HasCount(2, vsgs);
			CollectionAssert.AreEquivalent(
				new[] { "Source 1", "Source 2" },
				vsgs.Select(x => x.Name).ToList());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_GetByEndpointIds()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Query().First(x => x.Name == "Video Source 1");
			var videoSource2 = api.Endpoints.Query().First(x => x.Name == "Video Source 2");

			var vsgs = api.VirtualSignalGroups.GetByEndpointIds([videoSource1.ID, videoSource2.ID]).ToList();

			Assert.HasCount(2, vsgs);
			CollectionAssert.AreEquivalent(
				new[] { "Source 1", "Source 2" },
				vsgs.Select(x => x.Name).ToList());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_AssignEndpoint()
		{
			var api = new MediaOpsLiveApiMock();

			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			var videoLevel = api.Levels.Query().First(x => x.Name == "Video");
			var videoSource1 = api.Endpoints.Query().First(x => x.Name == "Video Source 1");

			vsg.Levels.Remove(vsg.Levels.FirstOrDefault(x => x.Level == videoLevel));
			api.VirtualSignalGroups.Update(vsg);
			vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			Assert.HasCount(1, vsg.Levels);

			vsg.Levels.Add(new LevelEndpoint(videoLevel, videoSource1));
			api.VirtualSignalGroups.Update(vsg);
			vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			Assert.HasCount(2, vsg.Levels);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_JoinEndpoints()
		{
			var api = new MediaOpsLiveApiMock();

			// act
			var result = api.VirtualSignalGroups.ReadAllPaged()
				.JoinEndpoints(api.Endpoints)
				.Flatten()
				.ToList();

			// assert
			Assert.IsNotNull(result);
			Assert.IsGreaterThan(0, result.Count, "Expected at least one joined result.");

			foreach (var (virtualSignalGroup, endpoints) in result)
			{
				var vsgName = virtualSignalGroup.Name;

				Assert.IsNotNull(virtualSignalGroup, "VirtualSignalGroup should not be null.");
				Assert.IsNotNull(endpoints, "Endpoints list should not be null.");

				Assert.IsTrue(endpoints.Any(), "Each VSG should have at least one endpoint.");

				foreach (var endpoint in endpoints)
				{
					Assert.EndsWith(vsgName, endpoint.Name, $"Endpoint '{endpoint.Name}' should end with VSG name '{vsgName}'.");
				}
			}
		}
	}
}
