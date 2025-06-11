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

			Assert.IsTrue(connectionMetrics.NumberOfRequests < 20);
			Assert.IsTrue(connectionMetrics.NumberOfDomInstancesRetrieved < 20);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Subscription()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");

			using var connectivity = new ConnectivityInfoProvider(api, subscribe: true);

			var receivedEvents = new List<ConnectionsUpdatedEvent>();
			connectivity.ConnectionsUpdated += (sender, e) => receivedEvents.Add(e);

			api.CreateConnection(audioSource1, audioDestination1);
			receivedEvents.Count.ShouldBe(1);
			receivedEvents[0].Endpoints.Count.ShouldBe(2); // Source and Destination
			receivedEvents[0].VirtualSignalGroups.Count.ShouldBe(2);
			receivedEvents[0].VirtualSignalGroups.ShouldAllBe(x => x.ConnectedStatus == ConnectionStatus.Partial);

			api.CreateConnection(videoSource1, videoDestination1);
			receivedEvents.Count.ShouldBe(2);
			receivedEvents[1].Endpoints.Count.ShouldBe(2); // Source and Destination
			receivedEvents[1].VirtualSignalGroups.Count.ShouldBe(2);
			receivedEvents[1].VirtualSignalGroups.ShouldAllBe(x => x.ConnectedStatus == ConnectionStatus.Connected);

			api.CreateConnection(null, audioDestination1);
			api.CreateConnection(null, videoDestination1);
			receivedEvents.Count.ShouldBe(4);
			receivedEvents[3].VirtualSignalGroups.ShouldAllBe(x => x.ConnectedStatus == ConnectionStatus.Disconnected);
		}

		#region Endpoints

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
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_GetConnectivity()
		{
			var api = new MediaOpsLiveApiMock();

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioSource3 = api.Endpoints.Read("Audio Source 3");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");
			var audioDestination3 = api.Endpoints.Read("Audio Destination 3");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");

			api.CreateConnection(audioSource1, audioDestination1);
			api.CreatePendingConnection(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			var audioSource1Connectivity = connectivity.GetConnectivity(audioSource1);
			audioSource1Connectivity.IsConnected.ShouldBeTrue();
			audioSource1Connectivity.IsPendingConnected.ShouldBeFalse();
			audioSource1Connectivity.ConnectedDestinations.ShouldBe([audioDestination1]);
			audioSource1Connectivity.VirtualSignalGroups.ShouldBe([source1]);

			var audioDestination1Connectivity = connectivity.GetConnectivity(audioDestination1);
			audioDestination1Connectivity.IsConnected.ShouldBeTrue();
			audioDestination1Connectivity.IsPendingConnected.ShouldBeFalse();
			audioDestination1Connectivity.ConnectedSource.ShouldBe(audioSource1);
			audioDestination1Connectivity.VirtualSignalGroups.ShouldBe([destination1]);

			var audioSource2Connectivity = connectivity.GetConnectivity(audioSource2);
			audioSource2Connectivity.IsConnected.ShouldBeFalse();
			audioSource2Connectivity.IsPendingConnected.ShouldBeTrue();
			audioSource2Connectivity.PendingConnectedDestinations.ShouldBe([audioDestination2]);

			var audioDestination2Connectivity = connectivity.GetConnectivity(audioDestination2);
			audioDestination2Connectivity.IsConnected.ShouldBeFalse();
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

		#endregion

		#region Virtual Signal Groups

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
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_GetConnectivity()
		{
			var api = new MediaOpsLiveApiMock();

			var videoLevel = api.Levels.Read("Video");
			var audioLevel = api.Levels.Read("Audio");

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var videoSource2 = api.Endpoints.Read("Video Source 2");
			var videoSource3 = api.Endpoints.Read("Video Source 3");
			var audioSource3 = api.Endpoints.Read("Audio Source 3");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var videoDestination2 = api.Endpoints.Read("Video Destination 2");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var source3 = api.VirtualSignalGroups.Read("Source 3");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");
			var destination3 = api.VirtualSignalGroups.Read("Destination 3");

			api.CreateConnection(videoSource1, videoDestination1);
			api.CreateConnection(audioSource1, audioDestination2);
			api.CreateConnection(videoSource2, videoDestination2);
			api.CreatePendingConnection(videoSource3, videoDestination1);
			api.CreatePendingConnection(audioSource3, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			var source1Connectivity = connectivity.GetConnectivity(source1);
			source1Connectivity.IsConnected.ShouldBeTrue();
			source1Connectivity.IsPendingConnected.ShouldBeFalse();
			source1Connectivity.ConnectedStatus.ShouldBe(ConnectionStatus.Connected);
			source1Connectivity.PendingConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			source1Connectivity.ConnectedSources.ShouldBeEmpty();
			source1Connectivity.ConnectedDestinations.ShouldBe([destination1, destination2], ignoreOrder: true);
			source1Connectivity.Levels.Keys.ShouldBe([videoLevel, audioLevel], ignoreOrder: true);
			source1Connectivity.Levels[videoLevel].ConnectedDestinations.ShouldBe([videoDestination1]);
			source1Connectivity.Levels[audioLevel].ConnectedDestinations.ShouldBe([audioDestination2]);

			var destination1Connectivity = connectivity.GetConnectivity(destination1);
			destination1Connectivity.IsConnected.ShouldBeTrue();
			destination1Connectivity.IsPendingConnected.ShouldBeTrue();
			destination1Connectivity.ConnectedStatus.ShouldBe(ConnectionStatus.Partial);
			destination1Connectivity.PendingConnectedStatus.ShouldBe(ConnectionStatus.Connected);
			destination1Connectivity.ConnectedSources.ShouldBe([source1]);
			destination1Connectivity.PendingConnectedSources.ShouldBe([source3]);
			destination1Connectivity.ConnectedDestinations.ShouldBeEmpty();
			destination1Connectivity.Levels.Keys.ShouldBe([videoLevel, audioLevel], ignoreOrder: true);
			destination1Connectivity.Levels[videoLevel].ConnectedSource.ShouldBe(videoSource1);
			destination1Connectivity.Levels[videoLevel].PendingConnectedSource.ShouldBe(videoSource3);
			destination1Connectivity.Levels[audioLevel].PendingConnectedSource.ShouldBe(audioSource3);

			var source2Connectivity = connectivity.GetConnectivity(source2);
			source2Connectivity.IsConnected.ShouldBeTrue();
			source2Connectivity.IsPendingConnected.ShouldBeFalse();
			source2Connectivity.ConnectedStatus.ShouldBe(ConnectionStatus.Partial);
			source2Connectivity.PendingConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			source2Connectivity.ConnectedSources.ShouldBeEmpty();
			source2Connectivity.ConnectedDestinations.ShouldBe([destination2]);
			source2Connectivity.PendingConnectedDestinations.ShouldBeEmpty();

			var destination2Connectivity = connectivity.GetConnectivity(destination2);
			destination2Connectivity.IsConnected.ShouldBeTrue();
			destination2Connectivity.IsPendingConnected.ShouldBeFalse();
			destination2Connectivity.ConnectedStatus.ShouldBe(ConnectionStatus.Connected);
			destination2Connectivity.PendingConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			destination2Connectivity.ConnectedSources.ShouldBe([source1, source2], ignoreOrder: true);
			destination2Connectivity.ConnectedDestinations.ShouldBeEmpty();

			var source3Connectivity = connectivity.GetConnectivity(source3);
			source3Connectivity.IsConnected.ShouldBeFalse();
			source3Connectivity.IsPendingConnected.ShouldBeTrue();
			source3Connectivity.ConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			source3Connectivity.PendingConnectedStatus.ShouldBe(ConnectionStatus.Connected);
			source3Connectivity.ConnectedSources.ShouldBeEmpty();
			source3Connectivity.ConnectedDestinations.ShouldBeEmpty();
			source3Connectivity.PendingConnectedDestinations.ShouldBe([destination1]);

			var destination3Connectivity = connectivity.GetConnectivity(destination3);
			destination3Connectivity.IsConnected.ShouldBeFalse();
			destination3Connectivity.IsPendingConnected.ShouldBeFalse();
			destination3Connectivity.ConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			destination3Connectivity.PendingConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			destination3Connectivity.ConnectedSources.ShouldBeEmpty();
			destination3Connectivity.ConnectedDestinations.ShouldBeEmpty();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_GetConnectivity_Bulk()
		{
			var api = new MediaOpsLiveApiMock();

			var videoLevel = api.Levels.Read("Video");
			var audioLevel = api.Levels.Read("Audio");

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var videoSource2 = api.Endpoints.Read("Video Source 2");
			var videoSource3 = api.Endpoints.Read("Video Source 3");
			var audioSource3 = api.Endpoints.Read("Audio Source 3");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var videoDestination2 = api.Endpoints.Read("Video Destination 2");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var source3 = api.VirtualSignalGroups.Read("Source 3");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");
			var destination3 = api.VirtualSignalGroups.Read("Destination 3");

			api.CreateConnection(videoSource1, videoDestination1);
			api.CreateConnection(audioSource1, audioDestination2);
			api.CreateConnection(videoSource2, videoDestination2);
			api.CreatePendingConnection(videoSource3, videoDestination1);
			api.CreatePendingConnection(audioSource3, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.GetConnectivity([source1, destination1, source2, destination2, source3, destination3]);

			result.Keys.ShouldBe([source1, destination1, source2, destination2, source3, destination3], ignoreOrder: true);

			result[source1].IsConnected.ShouldBeTrue();
			result[source1].IsPendingConnected.ShouldBeFalse();
			result[source1].ConnectedStatus.ShouldBe(ConnectionStatus.Connected);
			result[source1].PendingConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			result[source1].ConnectedDestinations.ShouldBe([destination1, destination2], ignoreOrder: true);
			result[source1].PendingConnectedDestinations.ShouldBeEmpty();
			result[source1].Levels.Keys.ShouldBe([videoLevel, audioLevel], ignoreOrder: true);
			result[source1].Levels[videoLevel].ConnectedDestinations.ShouldBe([videoDestination1]);
			result[source1].Levels[audioLevel].ConnectedDestinations.ShouldBe([audioDestination2]);

			result[destination1].IsConnected.ShouldBeTrue();
			result[destination1].IsPendingConnected.ShouldBeTrue();
			result[destination1].ConnectedStatus.ShouldBe(ConnectionStatus.Partial);
			result[destination1].PendingConnectedStatus.ShouldBe(ConnectionStatus.Connected);
			result[destination1].ConnectedSources.ShouldBe([source1]);
			result[destination1].PendingConnectedSources.ShouldBe([source3]);
			result[destination1].Levels.Keys.ShouldBe([videoLevel, audioLevel], ignoreOrder: true);
			result[destination1].Levels[videoLevel].ConnectedSource.ShouldBe(videoSource1);
			result[destination1].Levels[videoLevel].PendingConnectedSource.ShouldBe(videoSource3);
			result[destination1].Levels[audioLevel].PendingConnectedSource.ShouldBe(audioSource3);

			result[source2].IsConnected.ShouldBeTrue();
			result[source2].IsPendingConnected.ShouldBeFalse();
			result[source2].ConnectedStatus.ShouldBe(ConnectionStatus.Partial);
			result[source2].PendingConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			result[source2].ConnectedDestinations.ShouldBe([destination2]);
			result[source2].PendingConnectedDestinations.ShouldBeEmpty();

			result[destination2].IsConnected.ShouldBeTrue();
			result[destination2].IsPendingConnected.ShouldBeFalse();
			result[destination2].ConnectedStatus.ShouldBe(ConnectionStatus.Connected);
			result[destination2].PendingConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			result[destination2].ConnectedSources.ShouldBe([source1, source2], ignoreOrder: true);
			result[destination2].PendingConnectedSources.ShouldBeEmpty();

			result[source3].IsConnected.ShouldBeFalse();
			result[source3].IsPendingConnected.ShouldBeTrue();
			result[source3].ConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			result[source3].PendingConnectedStatus.ShouldBe(ConnectionStatus.Connected);
			result[source3].ConnectedDestinations.ShouldBeEmpty();
			result[source3].PendingConnectedDestinations.ShouldBe([destination1]);

			result[destination3].IsConnected.ShouldBeFalse();
			result[destination3].IsPendingConnected.ShouldBeFalse();
			result[destination3].ConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			result[destination3].PendingConnectedStatus.ShouldBe(ConnectionStatus.Disconnected);
			result[destination3].ConnectedSources.ShouldBeEmpty();
			result[destination3].PendingConnectedSources.ShouldBeEmpty();
		}

		#endregion
	}
}
