namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Extensions;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_VirtualSignalGroups
	{
		private static readonly MediaOpsLiveApi _api = new MediaOpsLiveApiMock();

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_GetByEndpoints()
		{
			var videoSource1 = _api.Endpoints.Query().First(x => x.Name == "Video Source 1");
			var videoSource2 = _api.Endpoints.Query().First(x => x.Name == "Video Source 2");

			var vsgs = _api.VirtualSignalGroups.GetByEndpoints([videoSource1, videoSource2]).ToList();

			Assert.AreEqual(2, vsgs.Count);
			CollectionAssert.AreEquivalent(
				new[] { "Source 1", "Source 2" },
				vsgs.Select(x => x.Name).ToList());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_GetByEndpointIds()
		{
			var videoSource1 = _api.Endpoints.Query().First(x => x.Name == "Video Source 1");
			var videoSource2 = _api.Endpoints.Query().First(x => x.Name == "Video Source 2");

			var vsgs = _api.VirtualSignalGroups.GetByEndpointIds([videoSource1.ID, videoSource2.ID]).ToList();

			Assert.AreEqual(2, vsgs.Count);
			CollectionAssert.AreEquivalent(
				new[] { "Source 1", "Source 2" },
				vsgs.Select(x => x.Name).ToList());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_AssignEndpoint()
		{
			var vsg = _api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			var videoLevel = _api.Levels.Query().First(x => x.Name == "Video");
			var videoSource1 = _api.Endpoints.Query().First(x => x.Name == "Video Source 1");

			vsg.Levels.Remove(vsg.Levels.FirstOrDefault(x => x.Level == videoLevel));
			_api.VirtualSignalGroups.Update(vsg);
			vsg = _api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			Assert.AreEqual(1, vsg.Levels.Count);

			vsg.Levels.Add(new LevelEndpoint(videoLevel, videoSource1));
			_api.VirtualSignalGroups.Update(vsg);
			vsg = _api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			Assert.AreEqual(2, vsg.Levels.Count);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_JoinEndpoints()
		{
			// act
			var result = _api.VirtualSignalGroups.ReadAllPaged()
				.JoinEndpoints(_api.Endpoints)
				.Flatten()
				.ToList();

			// assert
			Assert.IsNotNull(result);
			Assert.IsTrue(result.Count > 0, "Expected at least one joined result.");

			foreach (var (virtualSignalGroup, endpoints) in result)
			{
				var vsgName = virtualSignalGroup.Name;

				Assert.IsNotNull(virtualSignalGroup, "VirtualSignalGroup should not be null.");
				Assert.IsNotNull(endpoints, "Endpoints list should not be null.");

				Assert.IsTrue(endpoints.Any(), "Each VSG should have at least one endpoint.");

				foreach (var endpoint in endpoints)
				{
					Assert.IsTrue(
						endpoint.Name.EndsWith(vsgName),
						$"Endpoint '{endpoint.Name}' should end with VSG name '{vsgName}'.");
				}
			}
		}
	}
}
