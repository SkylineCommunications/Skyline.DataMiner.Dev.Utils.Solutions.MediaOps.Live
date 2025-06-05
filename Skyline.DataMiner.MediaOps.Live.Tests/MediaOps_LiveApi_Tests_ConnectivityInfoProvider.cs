namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.GQI.Metrics;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

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
			var connection = new DomConnectionMock();
			var interceptedConnection = new ConnectionInterceptor(connection);

			var api = new MediaOpsLiveApiMock(interceptedConnection);

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			api.CreateConnection(audioSource1, audioDestination1);

			var connectionMetrics = new ConnectionMetrics(interceptedConnection);

			using var connectivity = new ConnectivityInfoProvider(api);

			Assert.IsTrue(connectivity.IsConnected(audioSource1));
			Assert.IsTrue(connectivity.IsConnected(audioDestination1));

			Assert.IsFalse(connectivity.IsConnected(audioSource2));
			Assert.IsFalse(connectivity.IsConnected(audioDestination2));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_IsPendingConnected()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			api.CreatePendingConnection(audioSource1, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			Assert.IsTrue(connectivity.IsPendingConnected(audioSource1));
			Assert.IsTrue(connectivity.IsPendingConnected(audioDestination1));

			Assert.IsFalse(connectivity.IsPendingConnected(audioSource2));
			Assert.IsFalse(connectivity.IsPendingConnected(audioDestination2));
		}
	}
}
