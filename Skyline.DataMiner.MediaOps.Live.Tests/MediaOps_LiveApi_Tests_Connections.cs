namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Connections
    {
		private static readonly MediaOpsLiveApi _api = new MediaOpsLiveApiMock();

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Connections_GetConnectionsForDestinations()
		{
			var videoDestination1 = _api.Endpoints.Query().First(x => x.Name == "Video Destination 1");
			var audioDestination1 = _api.Endpoints.Query().First(x => x.Name == "Audio Destination 1");

			{
				var connections = _api.Connections.GetConnectionsForDestinations([videoDestination1, audioDestination1]);

				Assert.AreEqual(2, connections.Count);
				CollectionAssert.AreEquivalent(
					new ApiObjectReference<Endpoint>[] { videoDestination1, audioDestination1 },
					connections.Values.Select(x => x.Destination).ToList());
			}

			{
				var connections = _api.Connections.GetConnectionsForDestinations([videoDestination1.ID, audioDestination1.ID]);

				Assert.AreEqual(2, connections.Count);
				CollectionAssert.AreEquivalent(
					new ApiObjectReference<Endpoint>[] { videoDestination1, audioDestination1 },
					connections.Values.Select(x => x.Destination).ToList());
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Connections_GetConnectionForDestination()
		{
			var videoDestination1 = _api.Endpoints.Query().First(x => x.Name == "Video Destination 1");

			var connection = _api.Connections.GetConnectionForDestination(videoDestination1);

			Assert.IsNotNull(connection);
			Assert.AreEqual(videoDestination1, connection.Destination);
		}
	}
}
