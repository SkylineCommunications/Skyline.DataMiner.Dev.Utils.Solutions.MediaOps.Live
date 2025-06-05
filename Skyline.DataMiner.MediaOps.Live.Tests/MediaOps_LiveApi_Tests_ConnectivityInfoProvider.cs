namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_ConnectivityInfoProvider
	{
		/*
		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_GetConnectedSource()
		{
			var api = new MediaOpsLiveApiMock();

			using var connectivity = new ConnectivityInfoProvider(api);

			connectivity.GetConnectedSource();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_GetConnectedDestinations()
		{
			var api = new MediaOpsLiveApiMock();

			using var connectivity = new ConnectivityInfoProvider(api);

			connectivity.GetConnectedDestinations();
		}
		*/

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_IsConnected()
		{
			var api = new MediaOpsLiveApiMock();

			using var connectivity = new ConnectivityInfoProvider(api);

			Assert.IsTrue( connectivity.IsConnected());
			Assert.IsFalse( connectivity.IsConnected());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_IsPendingConnected()
		{
			var api = new MediaOpsLiveApiMock();

			using var connectivity = new ConnectivityInfoProvider(api);

			connectivity.IsPendingConnected();
		}
	}
}
