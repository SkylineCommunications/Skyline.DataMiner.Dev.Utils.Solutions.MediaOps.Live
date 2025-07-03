namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Shouldly;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.GQI.Metrics;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_ConnectivityInfoProvider
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Performance()
		{
			var simulation = new MediaOpsLiveSimulation();
			var connection = simulation.Dms.CreateConnection();
			var interceptedConnection = new ConnectionInterceptor(connection);
			var api = new MediaOpsLiveApi(interceptedConnection);

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");

			simulation.CreateTestConnection(audioSource1, audioDestination1);

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
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var videoSource2 = api.Endpoints.Read("Video Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");

			using var connectivity = new ConnectivityInfoProvider(api, subscribe: true);

			var receivedEvents = new List<ConnectionsUpdatedEvent>();
			connectivity.ConnectionsUpdated += (sender, e) => receivedEvents.Add(e);

			// Create a first connection (VSG partially connected)
			simulation.CreateTestConnection(audioSource1, audioDestination1);
			receivedEvents.Count.ShouldBe(1);
			receivedEvents.Last().Endpoints.Count.ShouldBe(2); // Source and Destination
			receivedEvents.Last().VirtualSignalGroups.Count.ShouldBe(2);
			receivedEvents.Last().VirtualSignalGroups.ShouldAllBe(x => x.ConnectedState == ConnectionState.Partial);
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup).ShouldBe([source1, destination1], ignoreOrder: true);

			// Create a second connection (VSG fully connected)
			simulation.CreateTestConnection(videoSource1, videoDestination1);
			receivedEvents.Count.ShouldBe(2);
			receivedEvents.Last().Endpoints.Count.ShouldBe(2); // Source and Destination
			receivedEvents.Last().VirtualSignalGroups.Count.ShouldBe(2);
			receivedEvents.Last().VirtualSignalGroups.ShouldAllBe(x => x.ConnectedState == ConnectionState.Connected);
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup)
				.ShouldBe([source1, destination1], ignoreOrder: true);

			// Create connection that already exists
			simulation.CreateTestConnection(videoSource1, videoDestination1);
			receivedEvents.Count.ShouldBe(2); // No new event, as the connection already exists

			// Create a pending connection action
			simulation.CreateTestPendingConnectionAction(audioSource2, audioDestination1);
			simulation.CreateTestPendingConnectionAction(videoSource2, videoDestination1);
			receivedEvents.Count.ShouldBe(4);
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup)
				.ShouldBe([source2, destination1], ignoreOrder: true);

			// Create the real connections
			simulation.CreateTestConnection(audioSource2, audioDestination1);
			simulation.CreateTestConnection(videoSource2, videoDestination1);
			receivedEvents.Count.ShouldBe(8); // 4 new events: 2 to clear the pending actions + 2 for the new connections
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup)
				.ShouldBe([source1, source2, destination1], ignoreOrder: true);

			// Start disconnecting the connections
			simulation.CreateTestPendingConnectionAction(audioSource2, audioDestination1, PendingConnectionAction.PendingActionType.Disconnect);
			simulation.CreateTestPendingConnectionAction(videoSource2, videoDestination1, PendingConnectionAction.PendingActionType.Disconnect);
			receivedEvents.Count.ShouldBe(10); // 2 new events
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup)
				.ShouldBe([source2, destination1], ignoreOrder: true);

			// Disconnecting the connections
			simulation.CreateTestConnection(null, audioDestination1);
			simulation.CreateTestConnection(null, videoDestination1);
			receivedEvents.Count.ShouldBe(14); // 4 new events: 2 to clear the pending actions + 2 for the disconnected connections
			receivedEvents.Last().VirtualSignalGroups.ShouldAllBe(x => x.ConnectedState == ConnectionState.Disconnected);
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup)
				.ShouldBe([source2, destination1], ignoreOrder: true);
		}

		#region Endpoints

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_IsConnected()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			simulation.CreateTestConnection(audioSource1, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			Assert.IsTrue(connectivity.IsConnected(audioSource1));
			Assert.IsTrue(connectivity.IsConnected(audioDestination1));

			Assert.IsFalse(connectivity.IsConnected(audioSource2));
			Assert.IsFalse(connectivity.IsConnected(audioDestination2));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_IsConnected_Bulk()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

			simulation.CreateTestConnection(audioSource1, audioDestination1);

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
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioSource3 = api.Endpoints.Read("Audio Source 3");
			var audioSource4 = api.Endpoints.Read("Audio Source 4");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");
			var audioDestination3 = api.Endpoints.Read("Audio Destination 3");
			var audioDestination4 = api.Endpoints.Read("Audio Destination 4");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");

			simulation.CreateTestConnection(audioSource1, audioDestination1);
			simulation.CreateTestPendingConnectionAction(audioSource1, audioDestination1); // should be ignored

			simulation.CreateTestPendingConnectionAction(audioSource2, audioDestination2); // pending connection

			simulation.CreateTestConnection(audioSource3, audioDestination3);
			simulation.CreateTestPendingConnectionAction(audioSource3, audioDestination3, PendingConnectionAction.PendingActionType.Disconnect); // pending disconnect

			using var connectivity = new ConnectivityInfoProvider(api);

			var audioSource1Connectivity = connectivity.GetConnectivity(audioSource1);
			audioSource1Connectivity.IsConnected.ShouldBeTrue();
			audioSource1Connectivity.IsPendingConnected.ShouldBeFalse();
			audioSource1Connectivity.IsDisconnecting.ShouldBeFalse();
			audioSource1Connectivity.DestinationConnections.ShouldBe([new(audioDestination1, EndpointConnectionState.Connected)]);
			audioSource1Connectivity.VirtualSignalGroups.ShouldBe([source1]);

			var audioDestination1Connectivity = connectivity.GetConnectivity(audioDestination1);
			audioDestination1Connectivity.IsConnected.ShouldBeTrue();
			audioDestination1Connectivity.IsPendingConnected.ShouldBeFalse();
			audioDestination1Connectivity.IsDisconnecting.ShouldBeFalse();
			audioDestination1Connectivity.ConnectedSource.ShouldBe(new(audioSource1, EndpointConnectionState.Connected));
			audioDestination1Connectivity.VirtualSignalGroups.ShouldBe([destination1]);

			var audioSource2Connectivity = connectivity.GetConnectivity(audioSource2);
			audioSource2Connectivity.IsConnected.ShouldBeFalse();
			audioSource2Connectivity.IsPendingConnected.ShouldBeTrue();
			audioSource2Connectivity.IsDisconnecting.ShouldBeFalse();
			audioSource2Connectivity.PendingConnectedDestinations.ShouldBe([audioDestination2]);

			var audioDestination2Connectivity = connectivity.GetConnectivity(audioDestination2);
			audioDestination2Connectivity.IsConnected.ShouldBeFalse();
			audioDestination2Connectivity.IsPendingConnected.ShouldBeTrue();
			audioDestination2Connectivity.IsDisconnecting.ShouldBeFalse();
			audioDestination2Connectivity.PendingConnectedSource.ShouldBe(audioSource2);

			var audioSource3Connectivity = connectivity.GetConnectivity(audioSource3);
			audioSource3Connectivity.IsConnected.ShouldBeTrue();
			audioSource3Connectivity.IsPendingConnected.ShouldBeFalse();
			audioSource3Connectivity.IsDisconnecting.ShouldBeTrue();
			audioSource3Connectivity.DestinationConnections.ShouldBe([new(audioDestination3, EndpointConnectionState.Disconnecting)]);

			var audioDestination3Connectivity = connectivity.GetConnectivity(audioDestination3);
			audioDestination3Connectivity.IsConnected.ShouldBeTrue();
			audioDestination3Connectivity.IsPendingConnected.ShouldBeFalse();
			audioDestination3Connectivity.IsDisconnecting.ShouldBeTrue();
			audioDestination3Connectivity.ConnectedSource.ShouldBe(new(audioSource3, EndpointConnectionState.Disconnecting));

			var audioSource4Connectivity = connectivity.GetConnectivity(audioSource4);
			audioSource4Connectivity.IsConnected.ShouldBeFalse();
			audioSource4Connectivity.IsPendingConnected.ShouldBeFalse();
			audioSource4Connectivity.IsDisconnecting.ShouldBeFalse();

			var audioDestination4Connectivity = connectivity.GetConnectivity(audioDestination4);
			audioDestination4Connectivity.IsConnected.ShouldBeFalse();
			audioDestination4Connectivity.IsPendingConnected.ShouldBeFalse();
			audioDestination4Connectivity.IsDisconnecting.ShouldBeFalse();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_Endpoint_GetConnectivity_Bulk()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioSource3 = api.Endpoints.Read("Audio Source 3");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");
			var audioDestination3 = api.Endpoints.Read("Audio Destination 3");

			simulation.CreateTestConnection(audioSource1, audioDestination1);
			simulation.CreateTestPendingConnectionAction(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.GetConnectivity([audioSource1, audioDestination1, audioSource2, audioDestination2, audioSource3, audioDestination3]);

			result.Keys.ShouldBe([audioSource1, audioDestination1, audioSource2, audioDestination2, audioSource3, audioDestination3], ignoreOrder: true);
			result[audioSource1].IsConnected.ShouldBeTrue();
			result[audioSource1].ConnectedDestinations.ShouldBe([audioDestination1]);
			result[audioDestination1].IsConnected.ShouldBeTrue();
			result[audioDestination1].ConnectedSource.ShouldBe(new(audioSource1, EndpointConnectionState.Connected));
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
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

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

			simulation.CreateTestConnection(videoSource1, videoDestination1);
			simulation.CreateTestConnection(audioSource1, audioDestination1);
			simulation.CreateTestConnection(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			Assert.AreEqual(ConnectionState.Connected, connectivity.IsConnected(source1));
			Assert.AreEqual(ConnectionState.Connected, connectivity.IsConnected(destination1));

			Assert.AreEqual(ConnectionState.Partial, connectivity.IsConnected(source2));
			Assert.AreEqual(ConnectionState.Partial, connectivity.IsConnected(destination2));

			Assert.AreEqual(ConnectionState.Disconnected, connectivity.IsConnected(source3));
			Assert.AreEqual(ConnectionState.Disconnected, connectivity.IsConnected(destination3));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_IsConnected_Bulk()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

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

			simulation.CreateTestConnection(videoSource1, videoDestination1);
			simulation.CreateTestConnection(audioSource1, audioDestination1);
			simulation.CreateTestConnection(audioSource2, audioDestination2);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.IsConnected([source1, destination1, source2, destination2, source3, destination3]);

			result.ShouldBe(new Dictionary<VirtualSignalGroup, ConnectionState>
			{
				{ source1, ConnectionState.Connected },
				{ destination1, ConnectionState.Connected },
				{ source2, ConnectionState.Partial },
				{ destination2, ConnectionState.Partial },
				{ source3, ConnectionState.Disconnected },
				{ destination3, ConnectionState.Disconnected },
			});
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_GetConnectivity()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var videoLevel = api.Levels.Read("Video");
			var audioLevel = api.Levels.Read("Audio");

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var videoSource2 = api.Endpoints.Read("Video Source 2");
			var videoSource3 = api.Endpoints.Read("Video Source 3");
			var audioSource3 = api.Endpoints.Read("Audio Source 3");
			var videoSource4 = api.Endpoints.Read("Video Source 4");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var videoDestination2 = api.Endpoints.Read("Video Destination 2");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");
			var videoDestination4 = api.Endpoints.Read("Video Destination 4");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var source3 = api.VirtualSignalGroups.Read("Source 3");
			var source4 = api.VirtualSignalGroups.Read("Source 4");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");
			var destination3 = api.VirtualSignalGroups.Read("Destination 3");
			var destination4 = api.VirtualSignalGroups.Read("Destination 4");

			simulation.CreateTestConnection(videoSource1, videoDestination1);
			simulation.CreateTestConnection(audioSource1, audioDestination2);
			simulation.CreateTestConnection(videoSource2, videoDestination2);
			simulation.CreateTestConnection(videoSource4, videoDestination4);
			simulation.CreateTestPendingConnectionAction(videoSource3, videoDestination1);
			simulation.CreateTestPendingConnectionAction(audioSource3, audioDestination1);
			simulation.CreateTestPendingConnectionAction(videoSource4, videoDestination4, PendingConnectionAction.PendingActionType.Disconnect);

			using var connectivity = new ConnectivityInfoProvider(api);

			var source1Connectivity = connectivity.GetConnectivity(source1);
			source1Connectivity.IsConnected.ShouldBeTrue();
			source1Connectivity.IsPendingConnected.ShouldBeFalse();
			source1Connectivity.IsDisconnecting.ShouldBeFalse();
			source1Connectivity.ConnectedState.ShouldBe(ConnectionState.Connected);
			source1Connectivity.ConnectedSources.ShouldBeEmpty();
			source1Connectivity.ConnectedDestinations.ShouldBe([destination1, destination2], ignoreOrder: true);
			source1Connectivity.Levels.Keys.ShouldBe([videoLevel, audioLevel], ignoreOrder: true);
			source1Connectivity.Levels[videoLevel].ConnectedDestinations.ShouldBe([videoDestination1]);
			source1Connectivity.Levels[audioLevel].ConnectedDestinations.ShouldBe([audioDestination2]);

			var destination1Connectivity = connectivity.GetConnectivity(destination1);
			destination1Connectivity.IsConnected.ShouldBeTrue();
			destination1Connectivity.IsPendingConnected.ShouldBeTrue();
			destination1Connectivity.IsDisconnecting.ShouldBeFalse();
			destination1Connectivity.ConnectedState.ShouldBe(ConnectionState.Partial);
			destination1Connectivity.ConnectedSources.ShouldBe([source1]);
			destination1Connectivity.PendingConnectedSources.ShouldBe([source3]);
			destination1Connectivity.ConnectedDestinations.ShouldBeEmpty();
			destination1Connectivity.Levels.Keys.ShouldBe([videoLevel, audioLevel], ignoreOrder: true);
			destination1Connectivity.Levels[videoLevel].ConnectedSource.ShouldBe(new(videoSource1, EndpointConnectionState.Connected));
			destination1Connectivity.Levels[videoLevel].PendingConnectedSource.ShouldBe(videoSource3);
			destination1Connectivity.Levels[audioLevel].PendingConnectedSource.ShouldBe(audioSource3);

			var source2Connectivity = connectivity.GetConnectivity(source2);
			source2Connectivity.IsConnected.ShouldBeTrue();
			source2Connectivity.IsPendingConnected.ShouldBeFalse();
			source2Connectivity.IsDisconnecting.ShouldBeFalse();
			source2Connectivity.ConnectedState.ShouldBe(ConnectionState.Partial);
			source2Connectivity.ConnectedSources.ShouldBeEmpty();
			source2Connectivity.ConnectedDestinations.ShouldBe([destination2]);
			source2Connectivity.PendingConnectedDestinations.ShouldBeEmpty();

			var destination2Connectivity = connectivity.GetConnectivity(destination2);
			destination2Connectivity.IsConnected.ShouldBeTrue();
			destination2Connectivity.IsPendingConnected.ShouldBeFalse();
			destination2Connectivity.IsDisconnecting.ShouldBeFalse();
			destination2Connectivity.ConnectedState.ShouldBe(ConnectionState.Connected);
			destination2Connectivity.ConnectedSources.ShouldBe([source1, source2], ignoreOrder: true);
			destination2Connectivity.ConnectedDestinations.ShouldBeEmpty();

			var source3Connectivity = connectivity.GetConnectivity(source3);
			source3Connectivity.IsConnected.ShouldBeFalse();
			source3Connectivity.IsPendingConnected.ShouldBeTrue();
			source3Connectivity.IsDisconnecting.ShouldBeFalse();
			source3Connectivity.ConnectedState.ShouldBe(ConnectionState.Disconnected);
			source3Connectivity.ConnectedSources.ShouldBeEmpty();
			source3Connectivity.ConnectedDestinations.ShouldBeEmpty();
			source3Connectivity.PendingConnectedDestinations.ShouldBe([destination1]);

			var destination3Connectivity = connectivity.GetConnectivity(destination3);
			destination3Connectivity.IsConnected.ShouldBeFalse();
			destination3Connectivity.IsPendingConnected.ShouldBeFalse();
			destination3Connectivity.IsDisconnecting.ShouldBeFalse();
			destination3Connectivity.ConnectedState.ShouldBe(ConnectionState.Disconnected);
			destination3Connectivity.ConnectedSources.ShouldBeEmpty();
			destination3Connectivity.ConnectedDestinations.ShouldBeEmpty();

			var source4Connectivity = connectivity.GetConnectivity(source4);
			source4Connectivity.IsConnected.ShouldBeTrue();
			source4Connectivity.IsPendingConnected.ShouldBeFalse();
			source4Connectivity.IsDisconnecting.ShouldBeTrue();

			var destination4Connectivity = connectivity.GetConnectivity(destination4);
			destination4Connectivity.IsConnected.ShouldBeTrue();
			destination4Connectivity.IsPendingConnected.ShouldBeFalse();
			destination4Connectivity.IsDisconnecting.ShouldBeTrue();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectivityInfoProvider_VirtualSignalGroup_GetConnectivity_Bulk()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

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

			simulation.CreateTestConnection(videoSource1, videoDestination1);
			simulation.CreateTestConnection(audioSource1, audioDestination2);
			simulation.CreateTestConnection(videoSource2, videoDestination2);
			simulation.CreateTestPendingConnectionAction(videoSource3, videoDestination1);
			simulation.CreateTestPendingConnectionAction(audioSource3, audioDestination1);

			using var connectivity = new ConnectivityInfoProvider(api);

			var result = connectivity.GetConnectivity([source1, destination1, source2, destination2, source3, destination3]);

			result.Keys.ShouldBe([source1, destination1, source2, destination2, source3, destination3], ignoreOrder: true);

			result[source1].IsConnected.ShouldBeTrue();
			result[source1].IsPendingConnected.ShouldBeFalse();
			result[source1].IsDisconnecting.ShouldBeFalse();
			result[source1].ConnectedState.ShouldBe(ConnectionState.Connected);
			result[source1].ConnectedDestinations.ShouldBe([destination1, destination2], ignoreOrder: true);
			result[source1].PendingConnectedDestinations.ShouldBeEmpty();
			result[source1].Levels.Keys.ShouldBe([videoLevel, audioLevel], ignoreOrder: true);
			result[source1].Levels[videoLevel].ConnectedDestinations.ShouldBe([videoDestination1]);
			result[source1].Levels[audioLevel].ConnectedDestinations.ShouldBe([audioDestination2]);

			result[destination1].IsConnected.ShouldBeTrue();
			result[destination1].IsPendingConnected.ShouldBeTrue();
			result[destination1].IsDisconnecting.ShouldBeFalse();
			result[destination1].ConnectedState.ShouldBe(ConnectionState.Partial);
			result[destination1].ConnectedSources.ShouldBe([source1]);
			result[destination1].PendingConnectedSources.ShouldBe([source3]);
			result[destination1].Levels.Keys.ShouldBe([videoLevel, audioLevel], ignoreOrder: true);
			result[destination1].Levels[videoLevel].ConnectedSource.ShouldBe(new(videoSource1, EndpointConnectionState.Connected));
			result[destination1].Levels[videoLevel].PendingConnectedSource.ShouldBe(videoSource3);
			result[destination1].Levels[audioLevel].PendingConnectedSource.ShouldBe(audioSource3);

			result[source2].IsConnected.ShouldBeTrue();
			result[source2].IsPendingConnected.ShouldBeFalse();
			result[source2].IsDisconnecting.ShouldBeFalse();
			result[source2].ConnectedState.ShouldBe(ConnectionState.Partial);
			result[source2].ConnectedDestinations.ShouldBe([destination2]);
			result[source2].PendingConnectedDestinations.ShouldBeEmpty();

			result[destination2].IsConnected.ShouldBeTrue();
			result[destination2].IsPendingConnected.ShouldBeFalse();
			result[destination2].IsDisconnecting.ShouldBeFalse();
			result[destination2].ConnectedState.ShouldBe(ConnectionState.Connected);
			result[destination2].ConnectedSources.ShouldBe([source1, source2], ignoreOrder: true);
			result[destination2].PendingConnectedSources.ShouldBeEmpty();

			result[source3].IsConnected.ShouldBeFalse();
			result[source3].IsPendingConnected.ShouldBeTrue();
			result[source3].IsDisconnecting.ShouldBeFalse();
			result[source3].ConnectedState.ShouldBe(ConnectionState.Disconnected);
			result[source3].ConnectedDestinations.ShouldBeEmpty();
			result[source3].PendingConnectedDestinations.ShouldBe([destination1]);

			result[destination3].IsConnected.ShouldBeFalse();
			result[destination3].IsPendingConnected.ShouldBeFalse();
			result[destination3].IsDisconnecting.ShouldBeFalse();
			result[destination3].ConnectedState.ShouldBe(ConnectionState.Disconnected);
			result[destination3].ConnectedSources.ShouldBeEmpty();
			result[destination3].PendingConnectedSources.ShouldBeEmpty();
		}

		#endregion
	}
}
