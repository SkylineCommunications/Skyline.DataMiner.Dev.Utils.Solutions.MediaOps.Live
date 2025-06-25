namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.Tests.Mocking;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Endpoints
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_Endpoints_Delete()
		{
			var simulation = new MediaOpsLiveSimulation(createVsgs: false, createConnections: true);
			var api = simulation.Api;

			// Deleting all endpoints should not throw an exception, even if still in use by connections
			var allConnections = api.Connections.ReadAll().ToList();
			Assert.IsNotEmpty(allConnections);

			var allEndpoints = api.Endpoints.ReadAll().ToList();
			Assert.IsNotEmpty(allEndpoints);

			api.Endpoints.Delete(allEndpoints);
		}
	}
}
