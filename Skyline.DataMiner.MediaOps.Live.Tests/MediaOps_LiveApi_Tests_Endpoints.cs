namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Endpoints
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_Endpoints_Delete()
		{
			var api = new MediaOpsLiveApiMock(createVsgs: false, createConnections: true);

			// Deleting all endpoints should not throw an exception, even if still in use by connections
			var allConnections = api.Connections.ReadAll().ToList();
			Assert.IsNotEmpty(allConnections);

			var allEndpoints = api.Endpoints.ReadAll().ToList();
			Assert.IsNotEmpty(allEndpoints);

			api.Endpoints.Delete(allEndpoints);
		}
	}
}
