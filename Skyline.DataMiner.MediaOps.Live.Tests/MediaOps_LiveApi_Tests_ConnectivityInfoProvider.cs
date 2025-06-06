namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Shouldly;

	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.GQI.Metrics;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_ConnectivityInfoProvider
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Performance()
		{
			var connection = new DomConnectionMock();
			var interceptedConnection = new ConnectionInterceptor(connection);

			var api = new MediaOpsLiveApiMock(interceptedConnection);

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");

			api.CreateConnection(audioSource1, audioDestination1);

			var connectionMetrics = new ConnectionMetrics(interceptedConnection);

			using (var connectivity = new ConnectivityInfoProvider(api))
			{
				Assert.IsTrue(connectivity.IsConnected(audioSource1));
			}

			Assert.IsTrue(connectionMetrics.NumberOfRequests < 100);
			Assert.IsTrue(connectionMetrics.NumberOfDomInstancesRetrieved < 100);
		}

		#region Endpoints

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_GetConnectedSource()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			api.CreateConnection(audioSource1, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			Assert.AreEqual(audioSource1, connectivity.GetConnectedSource(audioDestination1));
			Assert.IsNull(connectivity.GetConnectedSource(audioDestination2));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_GetConnectedSource_Bulk()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			api.CreateConnection(audioSource1, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.GetConnectedSource([audioDestination1, audioDestination2]);

			result.ShouldBe(new Dictionary<Endpoint, Endpoint?>
			{
				{ audioDestination1, audioSource1 },
				{ audioDestination2, null },
			});
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_GetConnectedDestinations()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			api.CreateConnection(audioSource1, audioDestination1);
			api.CreateConnection(audioSource1, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			connectivity.GetConnectedDestinations(audioSource1).ShouldBe([audioDestination1, audioDestination2], ignoreOrder: true);
			connectivity.GetConnectedDestinations(audioSource2).ShouldBeEmpty();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_GetConnectedDestinations_Bulk()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			api.CreateConnection(audioSource1, audioDestination1);
			api.CreateConnection(audioSource1, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.GetConnectedDestinations([audioSource1, audioSource2]);

			result.Keys.ShouldBe([audioSource1, audioSource2], ignoreOrder: true);
			result[audioSource1].ShouldBe([audioDestination1, audioDestination2], ignoreOrder: true);
			result[audioSource2].ShouldBeEmpty();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_IsConnected()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			api.CreateConnection(audioSource1, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			Assert.IsTrue(connectivity.IsConnected(audioSource1));
			Assert.IsTrue(connectivity.IsConnected(audioDestination1));

			Assert.IsFalse(connectivity.IsConnected(audioSource2));
			Assert.IsFalse(connectivity.IsConnected(audioDestination2));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_IsConnected_Bulk()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			api.CreateConnection(audioSource1, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.IsConnected([audioSource1, audioSource2, audioDestination1, audioDestination2]);

			result.ShouldBe(new Dictionary<Endpoint, bool>
			{
				{ audioSource1, true },
				{ audioSource2, false },
				{ audioDestination1, true },
				{ audioDestination2, false },
			});
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

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_GetConnectivity()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioSource3 = api.Endpoints.Read("Audio Source 3");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");
			var audioDestination3 = api.Endpoints.Read("Audio Destination 3");

			api.CreateConnection(audioSource1, audioDestination1);
			api.CreatePendingConnection(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			var audioSource1Connectivity = connectivity.GetConnectivity(audioSource1);
			audioSource1Connectivity.IsConnected.ShouldBeTrue();
			audioSource1Connectivity.ConnectedDestinations.ShouldBe([audioDestination1]);

			var audioDestination1Connectivity = connectivity.GetConnectivity(audioDestination1);
			audioDestination1Connectivity.IsConnected.ShouldBeTrue();
			audioDestination1Connectivity.ConnectedSource.ShouldBe(audioSource1);

			var audioSource2Connectivity = connectivity.GetConnectivity(audioSource2);
			audioSource2Connectivity.IsPendingConnected.ShouldBeTrue();
			audioSource2Connectivity.PendingConnectedDestinations.ShouldBe([audioDestination2]);

			var audioDestination2Connectivity = connectivity.GetConnectivity(audioDestination2);
			audioDestination2Connectivity.IsPendingConnected.ShouldBeTrue();
			audioDestination2Connectivity.PendingConnectedSource.ShouldBe(audioSource2);

			var audioSource3Connectivity = connectivity.GetConnectivity(audioSource3);
			audioSource3Connectivity.IsConnected.ShouldBeFalse();
			audioSource3Connectivity.IsPendingConnected.ShouldBeFalse();

			var audioDestination3Connectivity = connectivity.GetConnectivity(audioDestination3);
			audioDestination3Connectivity.IsConnected.ShouldBeFalse();
			audioDestination3Connectivity.IsPendingConnected.ShouldBeFalse();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_GetConnectivity_Bulk()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioSource3 = api.Endpoints.Read("Audio Source 3");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");
			var audioDestination3 = api.Endpoints.Read("Audio Destination 3");

			api.CreateConnection(audioSource1, audioDestination1);
			api.CreatePendingConnection(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.GetConnectivity([audioSource1, audioDestination1, audioSource2, audioDestination2, audioSource3, audioDestination3]);

			result.Keys.ShouldBe([audioSource1, audioDestination1, audioSource2, audioDestination2, audioSource3, audioDestination3], ignoreOrder: true);
			result[audioSource1].IsConnected.ShouldBeTrue();
			result[audioSource1].ConnectedDestinations.ShouldBe([audioDestination1]);
			result[audioDestination1].IsConnected.ShouldBeTrue();
			result[audioDestination1].ConnectedSource.ShouldBe(audioSource1);
			result[audioSource2].IsPendingConnected.ShouldBeTrue();
			result[audioSource2].PendingConnectedDestinations.ShouldBe([audioDestination2]);
			result[audioDestination2].IsPendingConnected.ShouldBeTrue();
			result[audioDestination2].PendingConnectedSource.ShouldBe(audioSource2);
			result[audioSource3].IsConnected.ShouldBeFalse();
			result[audioSource3].IsPendingConnected.ShouldBeFalse();
			result[audioDestination3].IsConnected.ShouldBeFalse();
			result[audioDestination3].IsPendingConnected.ShouldBeFalse();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_IsPendingConnected_Bulk()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			api.CreatePendingConnection(audioSource1, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.IsPendingConnected([audioSource1, audioSource2, audioDestination1, audioDestination2]);

			result.ShouldBe(new Dictionary<Endpoint, bool>
			{
				{ audioSource1, true },
				{ audioSource2, false },
				{ audioDestination1, true },
				{ audioDestination2, false },
			});
		}

		#endregion

		#region Virtual Signal Groups

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_GetConnectedSources()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var videoDestination2 = api.Endpoints.Read("Video Destination 2");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");
			var destination3 = api.VirtualSignalGroups.Read("Destination 3");

			api.CreateConnection(videoSource1, videoDestination1);
			api.CreateConnection(videoSource1, videoDestination2);
			api.CreateConnection(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			connectivity.GetConnectedSources(destination1).ShouldBe([source1]);
			connectivity.GetConnectedSources(destination2).ShouldBe([source1, source2]);
			connectivity.GetConnectedSources(destination3).ShouldBeEmpty();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_GetConnectedSources_Bulk()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var videoDestination2 = api.Endpoints.Read("Video Destination 2");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");
			var destination3 = api.VirtualSignalGroups.Read("Destination 3");

			api.CreateConnection(videoSource1, videoDestination1);
			api.CreateConnection(videoSource1, videoDestination2);
			api.CreateConnection(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.GetConnectedSources([destination1, destination2, destination3]);

			result.Keys.ShouldBe([destination1, destination2, destination3], ignoreOrder: true);
			result[destination1].ShouldBe([source1]);
			result[destination2].ShouldBe([source1, source2], ignoreOrder: true);
			result[destination3].ShouldBeEmpty();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_GetConnectedDestinations()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var videoSource2 = api.Endpoints.Read("Video Source 2");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var videoDestination2 = api.Endpoints.Read("Video Destination 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var source3 = api.VirtualSignalGroups.Read("Source 3");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");

			api.CreateConnection(videoSource1, videoDestination1);
			api.CreateConnection(videoSource2, videoDestination2);
			api.CreateConnection(audioSource2, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			connectivity.GetConnectedDestinations(source1).ShouldBe([destination1]);
			connectivity.GetConnectedDestinations(source2).ShouldBe([destination1, destination2], ignoreOrder: true);
			connectivity.GetConnectedDestinations(source3).ShouldBeEmpty();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_GetConnectedDestinations_Bulk()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var videoSource2 = api.Endpoints.Read("Video Source 2");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var videoDestination2 = api.Endpoints.Read("Video Destination 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var source3 = api.VirtualSignalGroups.Read("Source 3");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");

			api.CreateConnection(videoSource1, videoDestination1);
			api.CreateConnection(videoSource2, videoDestination2);
			api.CreateConnection(audioSource2, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.GetConnectedDestinations([source1, source2, source3]);

			result.Keys.ShouldBe([source1, source2, source3], ignoreOrder: true);
			result[source1].ShouldBe([destination1]);
			result[source2].ShouldBe([destination1, destination2], ignoreOrder: true);
			result[source3].ShouldBeEmpty();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_IsConnected()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var source3 = api.VirtualSignalGroups.Read("Source 3");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");
			var destination3 = api.VirtualSignalGroups.Read("Destination 3");

			api.CreateConnection(videoSource1, videoDestination1);
			api.CreateConnection(audioSource1, audioDestination1);
			api.CreateConnection(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			Assert.AreEqual(ConnectionStatus.Connected, connectivity.IsConnected(source1));
			Assert.AreEqual(ConnectionStatus.Connected, connectivity.IsConnected(destination1));

			Assert.AreEqual(ConnectionStatus.Partial, connectivity.IsConnected(source2));
			Assert.AreEqual(ConnectionStatus.Partial, connectivity.IsConnected(destination2));

			Assert.AreEqual(ConnectionStatus.Disconnected, connectivity.IsConnected(source3));
			Assert.AreEqual(ConnectionStatus.Disconnected, connectivity.IsConnected(destination3));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_IsConnected_Bulk()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var source3 = api.VirtualSignalGroups.Read("Source 3");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");
			var destination3 = api.VirtualSignalGroups.Read("Destination 3");

			api.CreateConnection(videoSource1, videoDestination1);
			api.CreateConnection(audioSource1, audioDestination1);
			api.CreateConnection(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.IsConnected([source1, destination1, source2, destination2, source3, destination3]);

			result.ShouldBe(new Dictionary<VirtualSignalGroup, ConnectionStatus>
			{
				{ source1, ConnectionStatus.Connected },
				{ destination1, ConnectionStatus.Connected },
				{ source2, ConnectionStatus.Partial },
				{ destination2, ConnectionStatus.Partial },
				{ source3, ConnectionStatus.Disconnected },
				{ destination3, ConnectionStatus.Disconnected },
			});
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_IsPendingConnected()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var source3 = api.VirtualSignalGroups.Read("Source 3");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");
			var destination3 = api.VirtualSignalGroups.Read("Destination 3");

			api.CreatePendingConnection(videoSource1, videoDestination1);
			api.CreatePendingConnection(audioSource1, audioDestination1);
			api.CreatePendingConnection(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			Assert.AreEqual(ConnectionStatus.Connected, connectivity.IsPendingConnected(source1));
			Assert.AreEqual(ConnectionStatus.Connected, connectivity.IsPendingConnected(destination1));

			Assert.AreEqual(ConnectionStatus.Partial, connectivity.IsPendingConnected(source2));
			Assert.AreEqual(ConnectionStatus.Partial, connectivity.IsPendingConnected(destination2));

			Assert.AreEqual(ConnectionStatus.Disconnected, connectivity.IsPendingConnected(source3));
			Assert.AreEqual(ConnectionStatus.Disconnected, connectivity.IsPendingConnected(destination3));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_IsPendingConnected_Bulk()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var source3 = api.VirtualSignalGroups.Read("Source 3");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");
			var destination3 = api.VirtualSignalGroups.Read("Destination 3");

			api.CreatePendingConnection(videoSource1, videoDestination1);
			api.CreatePendingConnection(audioSource1, audioDestination1);
			api.CreatePendingConnection(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.IsPendingConnected([source1, destination1, source2, destination2, source3, destination3]);

			result.ShouldBe(new Dictionary<VirtualSignalGroup, ConnectionStatus>
			{
				{ source1, ConnectionStatus.Connected },
				{ destination1, ConnectionStatus.Connected },
				{ source2, ConnectionStatus.Partial },
				{ destination2, ConnectionStatus.Partial },
				{ source3, ConnectionStatus.Disconnected },
				{ destination3, ConnectionStatus.Disconnected },
			});
		}

		#endregion
	}
}
