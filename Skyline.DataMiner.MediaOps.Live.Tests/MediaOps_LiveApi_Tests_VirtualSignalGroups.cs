namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_VirtualSignalGroups
    {
		private static readonly MediaOpsLiveApi _api = new MediaOpsLiveApiMock();

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_GetVirtualSignalGroupsContainingEndpoints()
		{
			var videoSource1 = _api.Endpoints.Query().First(x => x.Name == "Video Source 1");
			var videoSource2 = _api.Endpoints.Query().First(x => x.Name == "Video Source 2");

			{
				var vsgs = _api.VirtualSignalGroups.GetVirtualSignalGroupsContainingEndpoints([videoSource1, videoSource2]).ToList();

				Assert.AreEqual(2, vsgs.Count);
				CollectionAssert.AreEquivalent(
					new[] { "Source 1", "Source 2" },
					vsgs.Select(x => x.Name).ToList());
			}

			{
				var vsgs = _api.VirtualSignalGroups.GetVirtualSignalGroupsContainingEndpoints([videoSource1.ID, videoSource2.ID]).ToList();

				Assert.AreEqual(2, vsgs.Count);
				CollectionAssert.AreEquivalent(
					new[] { "Source 1", "Source 2" },
					vsgs.Select(x => x.Name).ToList());
			}
		}
	}
}
