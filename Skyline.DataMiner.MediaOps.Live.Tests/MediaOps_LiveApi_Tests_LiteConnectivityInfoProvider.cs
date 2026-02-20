namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using FluentAssertions;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_LiteConnectivityInfoProvider
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_LiteConnectivityInfoProvider_IsConnected1()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var audioSource1 = api.Endpoints.ReadSingle("Audio Source 1");
			var audioSource2 = api.Endpoints.ReadSingle("Audio Source 2");
			var audioDestination1 = api.Endpoints.ReadSingle("Audio Destination 1");
			var audioDestination2 = api.Endpoints.ReadSingle("Audio Destination 2");

			simulation.CreateTestConnection(audioSource1, audioDestination1);

			using var connectivity = new LiteConnectivityInfoProvider(api);

			Assert.IsTrue(connectivity.IsConnected(audioSource1));
			Assert.IsTrue(connectivity.IsConnected(audioDestination1));

			Assert.IsFalse(connectivity.IsConnected(audioSource2));
			Assert.IsFalse(connectivity.IsConnected(audioDestination2));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_LiteConnectivityInfoProvider_IsConnected2()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var audioSource1 = api.Endpoints.ReadSingle("Audio Source 1");
			var audioSource2 = api.Endpoints.ReadSingle("Audio Source 2");
			var audioDestination1 = api.Endpoints.ReadSingle("Audio Destination 1");
			var audioDestination2 = api.Endpoints.ReadSingle("Audio Destination 2");

			simulation.CreateTestConnection(audioSource1, audioDestination1);

			using var connectivity = new LiteConnectivityInfoProvider(api);

			Assert.IsTrue(connectivity.IsConnected(audioSource1, audioDestination1));
			Assert.IsFalse(connectivity.IsConnected(audioSource2, audioDestination1));
			Assert.IsFalse(connectivity.IsConnected(audioSource1, audioDestination2));
			Assert.IsFalse(connectivity.IsConnected(audioSource2, audioDestination2));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_LiteConnectivityInfoProvider_Subscription()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var audioSource1 = api.Endpoints.ReadSingle("Audio Source 1");
			var audioSource2 = api.Endpoints.ReadSingle("Audio Source 2");
			var audioDestination1 = api.Endpoints.ReadSingle("Audio Destination 1");

			using var connectivity = new LiteConnectivityInfoProvider(api, subscribe: true);

			var receivedEvents = new List<ICollection<ApiObjectReference<Endpoint>>>();
			connectivity.EndpointsImpacted += (sender, e) => receivedEvents.Add(e);

			receivedEvents.Count.Should().Be(0);

			// Create a connection
			simulation.CreateTestConnection(audioSource1, audioDestination1);
			receivedEvents.Count.Should().Be(1);
			receivedEvents.Last().Should().BeEquivalentTo([audioSource1, audioDestination1]);
			connectivity.IsConnected(audioDestination1).Should().BeTrue();
			connectivity.IsConnected(audioSource1, audioDestination1).Should().BeTrue();
			connectivity.IsConnected(audioSource2, audioDestination1).Should().BeFalse();

			// Connect same source
			simulation.CreateTestConnection(audioSource1, audioDestination1);
			receivedEvents.Count.Should().Be(1); // No change, so no new event

			// Connect another source
			simulation.CreateTestConnection(audioSource2, audioDestination1);
			receivedEvents.Count.Should().Be(2);
			receivedEvents.Last().Should().BeEquivalentTo([audioSource1, audioSource2, audioDestination1]);
			connectivity.IsConnected(audioDestination1).Should().BeTrue();
			connectivity.IsConnected(audioSource1, audioDestination1).Should().BeFalse();
			connectivity.IsConnected(audioSource2, audioDestination1).Should().BeTrue();

			// Connect an unknown source
			simulation.CreateTestConnection(null, audioDestination1);
			receivedEvents.Count.Should().Be(3);
			receivedEvents.Last().Should().BeEquivalentTo([audioSource2, audioDestination1]);
			connectivity.IsConnected(audioDestination1).Should().BeTrue();
			connectivity.IsConnected(audioSource1, audioDestination1).Should().BeFalse();
			connectivity.IsConnected(audioSource2, audioDestination1).Should().BeFalse();

			// Disconnect
			simulation.TestDisconnectDestination(audioDestination1);
			receivedEvents.Count.Should().Be(4);
			receivedEvents.Last().Should().BeEquivalentTo([audioDestination1]);
			connectivity.IsConnected(audioDestination1).Should().BeFalse();
			connectivity.IsConnected(audioSource1, audioDestination1).Should().BeFalse();
			connectivity.IsConnected(audioSource2, audioDestination1).Should().BeFalse();

			// Disconnect again
			simulation.TestDisconnectDestination(audioDestination1);
			receivedEvents.Count.Should().Be(4); // No change, so no new event
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_LiteConnectivityInfoProvider_StartStopElement()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var audioSource1 = api.Endpoints.ReadSingle("Audio Source 1");
			var audioDestination1 = api.Endpoints.ReadSingle("Audio Destination 1");

			var mediationElement = api.MediationElements.GetElementForEndpoint(audioDestination1);
			var simulatedMediationElement = simulation.Dms.Agents[mediationElement.DmaId].Elements[mediationElement.ElementId];

			simulation.CreateTestConnection(audioSource1, audioDestination1);

			using var connectivity = new LiteConnectivityInfoProvider(api, subscribe: true);

			var receivedEvents = new List<ICollection<ApiObjectReference<Endpoint>>>();
			connectivity.EndpointsImpacted += (sender, e) => receivedEvents.Add(e);

			connectivity.IsConnected(audioSource1, audioDestination1).Should().BeTrue();

			simulatedMediationElement.Stop();
			receivedEvents.Count.Should().Be(1);
			connectivity.IsConnected(audioSource1, audioDestination1).Should().BeFalse();

			simulatedMediationElement.Start();
			receivedEvents.Count.Should().Be(2);
			connectivity.IsConnected(audioSource1, audioDestination1).Should().BeTrue();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_LiteConnectivityInfoProvider_Dispose()
		{
			var simulation = new MediaOpsLiveSimulation();
			var connection = simulation.Dms.CreateConnection();
			var api = new API.MediaOpsLiveApi(connection);

			using (var connectivity = new LiteConnectivityInfoProvider(api, subscribe: true))
			{
				connection.SubscriptionCount.Should().BeGreaterThan(0);
				connection.HasOnNewMessageSubscribers.Should().BeTrue();
			}

			connection.SubscriptionCount.Should().Be(0);
			connection.HasOnNewMessageSubscribers.Should().BeFalse();
		}
	}
}