namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using FluentAssertions;

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

			connectionMetrics.NumberOfRequests.Should().BeLessThan(15UL);
			connectionMetrics.NumberOfDomRequests.Should().BeLessThan(15UL);
			connectionMetrics.NumberOfDomInstancesRetrieved.Should().BeLessThan(100UL);
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
			receivedEvents.Count.Should().Be(1);
			receivedEvents.Last().Endpoints.Count.Should().Be(2); // Source and Destination
			receivedEvents.Last().VirtualSignalGroups.Count.Should().Be(2);
			receivedEvents.Last().VirtualSignalGroups.Should().OnlyContain(x => x.ConnectedState == ConnectionState.Partial);
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup).Should().BeEquivalentTo([source1, destination1]);

			// Create a second connection (VSG fully connected)
			simulation.CreateTestConnection(videoSource1, videoDestination1);
			receivedEvents.Count.Should().Be(2);
			receivedEvents.Last().Endpoints.Count.Should().Be(2); // Source and Destination
			receivedEvents.Last().VirtualSignalGroups.Count.Should().Be(2);
			receivedEvents.Last().VirtualSignalGroups.Should().OnlyContain(x => x.ConnectedState == ConnectionState.Connected);
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup)
				.Should().BeEquivalentTo([source1, destination1]);

			// Create connection that already exists
			simulation.CreateTestConnection(videoSource1, videoDestination1);
			receivedEvents.Count.Should().Be(2); // No new event, as the connection already exists

			// Create a pending connection action
			simulation.CreateTestPendingConnectionAction(audioSource2, audioDestination1);
			simulation.CreateTestPendingConnectionAction(videoSource2, videoDestination1);
			receivedEvents.Count.Should().Be(4);
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup)
				.Should().BeEquivalentTo([source2, destination1]);

			// Create a pending connection action that already exists
			simulation.CreateTestPendingConnectionAction(audioSource2, audioDestination1);
			receivedEvents.Count.Should().Be(4); // No new event

			// Create the real connections
			simulation.CreateTestConnection(audioSource2, audioDestination1);
			simulation.CreateTestConnection(videoSource2, videoDestination1);
			receivedEvents.Count.Should().Be(6); // 2 new events
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup)
				.Should().BeEquivalentTo([source1, source2, destination1]);

			// Start disconnecting the connections
			simulation.CreateTestPendingConnectionAction(audioSource2, audioDestination1, PendingConnectionActionType.Disconnect);
			simulation.CreateTestPendingConnectionAction(videoSource2, videoDestination1, PendingConnectionActionType.Disconnect);
			receivedEvents.Count.Should().Be(8); // 2 new events
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup)
				.Should().BeEquivalentTo([source2, destination1]);

			// Disconnecting the connections
			simulation.TestDisconnectDestination(audioDestination1);
			simulation.TestDisconnectDestination(videoDestination1);
			receivedEvents.Count.Should().Be(10); // 2 new events
			receivedEvents.Last().VirtualSignalGroups.Should().OnlyContain(x => x.ConnectedState == ConnectionState.Disconnected);
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup)
				.Should().BeEquivalentTo([source2, destination1]);

			// Connect to unknown sources
			simulation.CreateTestConnection(null, audioDestination1);
			simulation.CreateTestConnection(null, videoDestination1);
			receivedEvents.Count.Should().Be(12); // 2 new events
			receivedEvents.Last().VirtualSignalGroups.Select(x => x.VirtualSignalGroup)
				.Should().BeEquivalentTo([destination1]);
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

			Assert.IsTrue(connectivity.IsConnected(audioSource1, audioDestination1));
			Assert.IsFalse(connectivity.IsConnected(audioSource2, audioDestination1));
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

			result.Should().BeEquivalentTo(new Dictionary<Endpoint, bool>
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
			var audioSource5 = api.Endpoints.Read("Audio Source 5");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");
			var audioDestination3 = api.Endpoints.Read("Audio Destination 3");
			var audioDestination4 = api.Endpoints.Read("Audio Destination 4");
			var audioDestination5 = api.Endpoints.Read("Audio Destination 5");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");

			simulation.CreateTestConnection(audioSource1, audioDestination1);
			simulation.CreateTestPendingConnectionAction(audioSource1, audioDestination1); // should be ignored

			simulation.CreateTestPendingConnectionAction(audioSource2, audioDestination2); // pending connection

			simulation.CreateTestConnection(audioSource3, audioDestination3);
			simulation.CreateTestPendingConnectionAction(audioSource3, audioDestination3, PendingConnectionActionType.Disconnect); // pending disconnect

			simulation.CreateTestConnection(null, audioDestination4); // connected to an unknown source

			simulation.CreateTestPendingConnectionAction(null, audioDestination5, PendingConnectionActionType.Disconnect); // should be ignored because not connected

			using var connectivity = new ConnectivityInfoProvider(api);

			var audioSource1Connectivity = connectivity.GetConnectivity(audioSource1);
			audioSource1Connectivity.ConnectionState.Should().Be(EndpointConnectionState.Connected);
			audioSource1Connectivity.IsConnected.Should().BeTrue();
			audioSource1Connectivity.IsConnecting.Should().BeFalse();
			audioSource1Connectivity.IsDisconnecting.Should().BeFalse();
			audioSource1Connectivity.DestinationConnections.Should().BeEquivalentTo([new EndpointConnection(audioDestination1, EndpointConnectionState.Connected)]);
			audioSource1Connectivity.VirtualSignalGroups.Should().BeEquivalentTo([source1]);

			var audioDestination1Connectivity = connectivity.GetConnectivity(audioDestination1);
			audioDestination1Connectivity.ConnectionState.Should().Be(EndpointConnectionState.Connected);
			audioDestination1Connectivity.IsConnected.Should().BeTrue();
			audioDestination1Connectivity.IsConnecting.Should().BeFalse();
			audioDestination1Connectivity.IsDisconnecting.Should().BeFalse();
			audioDestination1Connectivity.ConnectedSource.Should().Be(audioSource1);
			audioDestination1Connectivity.VirtualSignalGroups.Should().BeEquivalentTo([destination1]);

			var audioSource2Connectivity = connectivity.GetConnectivity(audioSource2);
			audioSource2Connectivity.ConnectionState.Should().Be(EndpointConnectionState.Connecting);
			audioSource2Connectivity.IsConnected.Should().BeFalse();
			audioSource2Connectivity.IsConnecting.Should().BeTrue();
			audioSource2Connectivity.IsDisconnecting.Should().BeFalse();
			audioSource2Connectivity.PendingConnectedDestinations.Should().BeEquivalentTo([audioDestination2]);

			var audioDestination2Connectivity = connectivity.GetConnectivity(audioDestination2);
			audioDestination2Connectivity.ConnectionState.Should().Be(EndpointConnectionState.Connecting);
			audioDestination2Connectivity.IsConnected.Should().BeFalse();
			audioDestination2Connectivity.IsConnecting.Should().BeTrue();
			audioDestination2Connectivity.IsDisconnecting.Should().BeFalse();
			audioDestination2Connectivity.PendingConnectedSource.Should().Be(audioSource2);

			var audioSource3Connectivity = connectivity.GetConnectivity(audioSource3);
			audioSource3Connectivity.ConnectionState.Should().Be(EndpointConnectionState.Disconnecting);
			audioSource3Connectivity.IsConnected.Should().BeTrue();
			audioSource3Connectivity.IsConnecting.Should().BeFalse();
			audioSource3Connectivity.IsDisconnecting.Should().BeTrue();
			audioSource3Connectivity.DestinationConnections.Should().BeEquivalentTo([new EndpointConnection(audioDestination3, EndpointConnectionState.Disconnecting)]);

			var audioDestination3Connectivity = connectivity.GetConnectivity(audioDestination3);
			audioDestination3Connectivity.ConnectionState.Should().Be(EndpointConnectionState.Disconnecting);
			audioDestination3Connectivity.IsConnected.Should().BeTrue();
			audioDestination3Connectivity.IsConnecting.Should().BeFalse();
			audioDestination3Connectivity.IsDisconnecting.Should().BeTrue();
			audioDestination3Connectivity.ConnectedSource.Should().Be(audioSource3);

			var audioSource4Connectivity = connectivity.GetConnectivity(audioSource4);
			audioSource4Connectivity.ConnectionState.Should().Be(EndpointConnectionState.Disconnected);
			audioSource4Connectivity.IsConnected.Should().BeFalse();
			audioSource4Connectivity.IsConnecting.Should().BeFalse();
			audioSource4Connectivity.IsDisconnecting.Should().BeFalse();

			var audioDestination4Connectivity = connectivity.GetConnectivity(audioDestination4);
			audioDestination4Connectivity.ConnectionState.Should().Be(EndpointConnectionState.Connected);
			audioDestination4Connectivity.ConnectedSource.Should().BeNull(); // Connected to an unknown source
			audioDestination4Connectivity.IsConnected.Should().BeTrue();
			audioDestination4Connectivity.IsConnecting.Should().BeFalse();
			audioDestination4Connectivity.IsDisconnecting.Should().BeFalse();

			var audioSource5Connectivity = connectivity.GetConnectivity(audioSource5);
			audioSource5Connectivity.ConnectionState.Should().Be(EndpointConnectionState.Disconnected);
			audioSource5Connectivity.IsConnected.Should().BeFalse();
			audioSource5Connectivity.IsConnecting.Should().BeFalse();
			audioSource5Connectivity.IsDisconnecting.Should().BeFalse();

			var audioDestination5Connectivity = connectivity.GetConnectivity(audioDestination5);
			audioDestination5Connectivity.ConnectionState.Should().Be(EndpointConnectionState.Disconnected);
			audioDestination5Connectivity.IsConnected.Should().BeFalse();
			audioDestination5Connectivity.IsConnecting.Should().BeFalse();
			audioDestination5Connectivity.IsDisconnecting.Should().BeFalse();
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

			result.Keys.Should().BeEquivalentTo([audioSource1, audioDestination1, audioSource2, audioDestination2, audioSource3, audioDestination3]);
			result[audioSource1].IsConnected.Should().BeTrue();
			result[audioSource1].ConnectedDestinations.Should().BeEquivalentTo([audioDestination1]);
			result[audioDestination1].IsConnected.Should().BeTrue();
			result[audioDestination1].ConnectedSource.Should().Be(audioSource1);
			result[audioSource2].IsConnecting.Should().BeTrue();
			result[audioSource2].PendingConnectedDestinations.Should().BeEquivalentTo([audioDestination2]);
			result[audioDestination2].IsConnecting.Should().BeTrue();
			result[audioDestination2].PendingConnectedSource.Should().Be(audioSource2);
			result[audioSource3].IsConnected.Should().BeFalse();
			result[audioSource3].IsConnecting.Should().BeFalse();
			result[audioDestination3].IsConnected.Should().BeFalse();
			result[audioDestination3].IsConnecting.Should().BeFalse();
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

			result.Should().BeEquivalentTo(new Dictionary<VirtualSignalGroup, ConnectionState>
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
			var videoSource5 = api.Endpoints.Read("Video Source 5");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var videoDestination2 = api.Endpoints.Read("Video Destination 2");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");
			var videoDestination4 = api.Endpoints.Read("Video Destination 4");
			var videoDestination5 = api.Endpoints.Read("Video Destination 5");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var source2 = api.VirtualSignalGroups.Read("Source 2");
			var source3 = api.VirtualSignalGroups.Read("Source 3");
			var source4 = api.VirtualSignalGroups.Read("Source 4");
			var source5 = api.VirtualSignalGroups.Read("Source 5");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");
			var destination2 = api.VirtualSignalGroups.Read("Destination 2");
			var destination3 = api.VirtualSignalGroups.Read("Destination 3");
			var destination4 = api.VirtualSignalGroups.Read("Destination 4");
			var destination5 = api.VirtualSignalGroups.Read("Destination 5");

			simulation.CreateTestConnection(videoSource1, videoDestination1);
			simulation.CreateTestConnection(audioSource1, audioDestination2);
			simulation.CreateTestConnection(videoSource2, videoDestination2);
			simulation.CreateTestConnection(videoSource4, videoDestination4);
			simulation.CreateTestConnection(null, videoDestination5); // Connected to an unknown source
			simulation.CreateTestPendingConnectionAction(videoSource3, videoDestination1);
			simulation.CreateTestPendingConnectionAction(audioSource3, audioDestination1);
			simulation.CreateTestPendingConnectionAction(videoSource4, videoDestination4, PendingConnectionActionType.Disconnect);

			using var connectivity = new ConnectivityInfoProvider(api);

			var source1Connectivity = connectivity.GetConnectivity(source1);
			source1Connectivity.IsConnected.Should().BeTrue();
			source1Connectivity.IsConnecting.Should().BeFalse();
			source1Connectivity.IsDisconnecting.Should().BeFalse();
			source1Connectivity.ConnectedState.Should().Be(ConnectionState.Connected);
			source1Connectivity.ConnectedSources.Should().BeEmpty();
			source1Connectivity.ConnectedDestinations.Should().BeEquivalentTo([destination1, destination2]);
			source1Connectivity.Levels.Keys.Should().BeEquivalentTo([videoLevel, audioLevel]);
			source1Connectivity.Levels[videoLevel].ConnectedDestinations.Should().BeEquivalentTo([videoDestination1]);
			source1Connectivity.Levels[audioLevel].ConnectedDestinations.Should().BeEquivalentTo([audioDestination2]);

			var destination1Connectivity = connectivity.GetConnectivity(destination1);
			destination1Connectivity.IsConnected.Should().BeTrue();
			destination1Connectivity.IsConnecting.Should().BeTrue();
			destination1Connectivity.IsDisconnecting.Should().BeFalse();
			destination1Connectivity.ConnectedState.Should().Be(ConnectionState.Partial);
			destination1Connectivity.ConnectedSources.Should().BeEquivalentTo([source1]);
			destination1Connectivity.PendingConnectedSources.Should().BeEquivalentTo([source3]);
			destination1Connectivity.ConnectedDestinations.Should().BeEmpty();
			destination1Connectivity.Levels.Keys.Should().BeEquivalentTo([videoLevel, audioLevel]);
			destination1Connectivity.Levels[videoLevel].ConnectedSource.Should().Be(videoSource1);
			destination1Connectivity.Levels[videoLevel].PendingConnectedSource.Should().Be(videoSource3);
			destination1Connectivity.Levels[audioLevel].PendingConnectedSource.Should().Be(audioSource3);

			var source2Connectivity = connectivity.GetConnectivity(source2);
			source2Connectivity.IsConnected.Should().BeTrue();
			source2Connectivity.IsConnecting.Should().BeFalse();
			source2Connectivity.IsDisconnecting.Should().BeFalse();
			source2Connectivity.ConnectedState.Should().Be(ConnectionState.Partial);
			source2Connectivity.ConnectedSources.Should().BeEmpty();
			source2Connectivity.ConnectedDestinations.Should().BeEquivalentTo([destination2]);
			source2Connectivity.PendingConnectedDestinations.Should().BeEmpty();

			var destination2Connectivity = connectivity.GetConnectivity(destination2);
			destination2Connectivity.IsConnected.Should().BeTrue();
			destination2Connectivity.IsConnecting.Should().BeFalse();
			destination2Connectivity.IsDisconnecting.Should().BeFalse();
			destination2Connectivity.ConnectedState.Should().Be(ConnectionState.Connected);
			destination2Connectivity.ConnectedSources.Should().BeEquivalentTo([source1, source2]);
			destination2Connectivity.ConnectedDestinations.Should().BeEmpty();

			var source3Connectivity = connectivity.GetConnectivity(source3);
			source3Connectivity.IsConnected.Should().BeFalse();
			source3Connectivity.IsConnecting.Should().BeTrue();
			source3Connectivity.IsDisconnecting.Should().BeFalse();
			source3Connectivity.ConnectedState.Should().Be(ConnectionState.Disconnected);
			source3Connectivity.ConnectedSources.Should().BeEmpty();
			source3Connectivity.ConnectedDestinations.Should().BeEmpty();
			source3Connectivity.PendingConnectedDestinations.Should().BeEquivalentTo([destination1]);

			var destination3Connectivity = connectivity.GetConnectivity(destination3);
			destination3Connectivity.IsConnected.Should().BeFalse();
			destination3Connectivity.IsConnecting.Should().BeFalse();
			destination3Connectivity.IsDisconnecting.Should().BeFalse();
			destination3Connectivity.ConnectedState.Should().Be(ConnectionState.Disconnected);
			destination3Connectivity.ConnectedSources.Should().BeEmpty();
			destination3Connectivity.ConnectedDestinations.Should().BeEmpty();

			var source4Connectivity = connectivity.GetConnectivity(source4);
			source4Connectivity.IsConnected.Should().BeTrue();
			source4Connectivity.IsConnecting.Should().BeFalse();
			source4Connectivity.IsDisconnecting.Should().BeTrue();
			source4Connectivity.ConnectedState.Should().Be(ConnectionState.Partial);

			var destination4Connectivity = connectivity.GetConnectivity(destination4);
			destination4Connectivity.IsConnected.Should().BeTrue();
			destination4Connectivity.IsConnecting.Should().BeFalse();
			destination4Connectivity.IsDisconnecting.Should().BeTrue();
			destination4Connectivity.ConnectedState.Should().Be(ConnectionState.Partial);

			var source5Connectivity = connectivity.GetConnectivity(source5);
			source5Connectivity.IsConnected.Should().BeFalse();
			source5Connectivity.IsConnecting.Should().BeFalse();
			source5Connectivity.IsDisconnecting.Should().BeFalse();
			source5Connectivity.ConnectedState.Should().Be(ConnectionState.Disconnected);

			var destination5Connectivity = connectivity.GetConnectivity(destination5);
			destination5Connectivity.IsConnected.Should().BeTrue();
			destination5Connectivity.IsConnecting.Should().BeFalse();
			destination5Connectivity.IsDisconnecting.Should().BeFalse();
			destination5Connectivity.ConnectedState.Should().Be(ConnectionState.Partial);
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

			result.Keys.Should().BeEquivalentTo([source1, destination1, source2, destination2, source3, destination3]);

			result[source1].IsConnected.Should().BeTrue();
			result[source1].IsConnecting.Should().BeFalse();
			result[source1].IsDisconnecting.Should().BeFalse();
			result[source1].ConnectedState.Should().Be(ConnectionState.Connected);
			result[source1].ConnectedDestinations.Should().BeEquivalentTo([destination1, destination2]);
			result[source1].PendingConnectedDestinations.Should().BeEmpty();
			result[source1].Levels.Keys.Should().BeEquivalentTo([videoLevel, audioLevel]);
			result[source1].Levels[videoLevel].ConnectedDestinations.Should().BeEquivalentTo([videoDestination1]);
			result[source1].Levels[audioLevel].ConnectedDestinations.Should().BeEquivalentTo([audioDestination2]);

			result[destination1].IsConnected.Should().BeTrue();
			result[destination1].IsConnecting.Should().BeTrue();
			result[destination1].IsDisconnecting.Should().BeFalse();
			result[destination1].ConnectedState.Should().Be(ConnectionState.Partial);
			result[destination1].ConnectedSources.Should().BeEquivalentTo([source1]);
			result[destination1].PendingConnectedSources.Should().BeEquivalentTo([source3]);
			result[destination1].Levels.Keys.Should().BeEquivalentTo([videoLevel, audioLevel]);
			result[destination1].Levels[videoLevel].ConnectedSource.Should().Be(videoSource1);
			result[destination1].Levels[videoLevel].PendingConnectedSource.Should().Be(videoSource3);
			result[destination1].Levels[audioLevel].PendingConnectedSource.Should().Be(audioSource3);

			result[source2].IsConnected.Should().BeTrue();
			result[source2].IsConnecting.Should().BeFalse();
			result[source2].IsDisconnecting.Should().BeFalse();
			result[source2].ConnectedState.Should().Be(ConnectionState.Partial);
			result[source2].ConnectedDestinations.Should().BeEquivalentTo([destination2]);
			result[source2].PendingConnectedDestinations.Should().BeEmpty();

			result[destination2].IsConnected.Should().BeTrue();
			result[destination2].IsConnecting.Should().BeFalse();
			result[destination2].IsDisconnecting.Should().BeFalse();
			result[destination2].ConnectedState.Should().Be(ConnectionState.Connected);
			result[destination2].ConnectedSources.Should().BeEquivalentTo([source1, source2]);
			result[destination2].PendingConnectedSources.Should().BeEmpty();

			result[source3].IsConnected.Should().BeFalse();
			result[source3].IsConnecting.Should().BeTrue();
			result[source3].IsDisconnecting.Should().BeFalse();
			result[source3].ConnectedState.Should().Be(ConnectionState.Disconnected);
			result[source3].ConnectedDestinations.Should().BeEmpty();
			result[source3].PendingConnectedDestinations.Should().BeEquivalentTo([destination1]);

			result[destination3].IsConnected.Should().BeFalse();
			result[destination3].IsConnecting.Should().BeFalse();
			result[destination3].IsDisconnecting.Should().BeFalse();
			result[destination3].ConnectedState.Should().Be(ConnectionState.Disconnected);
			result[destination3].ConnectedSources.Should().BeEmpty();
			result[destination3].PendingConnectedSources.Should().BeEmpty();
		}

		#endregion
	}
}
