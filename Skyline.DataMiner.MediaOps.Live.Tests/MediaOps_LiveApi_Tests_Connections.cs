namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Connections
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_Connections_GetByDestinations()
		{
			var simulation = new MediaOpsLiveSimulation(createConnections: true);
			var api = simulation.Api;

			var videoDestination1 = api.Endpoints.Query().First(x => x.Name == "Video Destination 1");
			var audioDestination1 = api.Endpoints.Query().First(x => x.Name == "Audio Destination 1");

			var connections = api.Connections.GetByDestinations([videoDestination1, audioDestination1]);

			Assert.AreEqual(2, connections.Count);
			CollectionAssert.AreEquivalent(
				new ApiObjectReference<Endpoint>[] { videoDestination1, audioDestination1 },
				connections.Values.Select(x => x.Destination).ToList());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Connections_GetByDestinationIds()
		{
			var simulation = new MediaOpsLiveSimulation(createConnections: true);
			var api = simulation.Api;

			var videoDestination1 = api.Endpoints.Query().First(x => x.Name == "Video Destination 1");
			var audioDestination1 = api.Endpoints.Query().First(x => x.Name == "Audio Destination 1");

			var connections = api.Connections.GetByDestinationIds([videoDestination1.ID, audioDestination1.ID]);

			Assert.AreEqual(2, connections.Count);
			CollectionAssert.AreEquivalent(
				new ApiObjectReference<Endpoint>[] { videoDestination1, audioDestination1 },
				connections.Values.Select(x => x.Destination).ToList());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Connections_GetByDestination()
		{
			var simulation = new MediaOpsLiveSimulation(createConnections: true);
			var api = simulation.Api;

			var videoDestination1 = api.Endpoints.Query().First(x => x.Name == "Video Destination 1");

			var connection = api.Connections.GetByDestination(videoDestination1);

			Assert.IsNotNull(connection);
			Assert.AreEqual(videoDestination1, connection.Destination);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Connections_GetByDestinationId()
		{
			var simulation = new MediaOpsLiveSimulation(createConnections: true);
			var api = simulation.Api;

			var videoDestination1 = api.Endpoints.Query().First(x => x.Name == "Video Destination 1");

			var connection = api.Connections.GetByDestinationId(videoDestination1.ID);

			Assert.IsNotNull(connection);
			Assert.AreEqual(videoDestination1, connection.Destination);
		}
	}
}
