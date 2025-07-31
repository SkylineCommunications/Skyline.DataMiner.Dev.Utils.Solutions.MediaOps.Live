namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Shouldly;

	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_LiteConnectivityInfoProvider
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_LiteConnectivityInfoProvider_IsConnected1()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

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

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var audioDestination2 = api.Endpoints.Read("Audio Destination 2");

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

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var audioSource2 = api.Endpoints.Read("Audio Source 2");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");

			using var connectivity = new LiteConnectivityInfoProvider(api, subscribe: true);

			var receivedEvents = new List<ICollection<ApiObjectReference<Endpoint>>>();
			connectivity.EndpointsImpacted += (sender, e) => receivedEvents.Add(e);

			receivedEvents.Count.ShouldBe(0);

			// Create a connection
			simulation.CreateTestConnection(audioSource1, audioDestination1);
			receivedEvents.Count.ShouldBe(1);
			receivedEvents.Last().ShouldBe([audioSource1, audioDestination1], ignoreOrder: true);
			connectivity.IsConnected(audioDestination1).ShouldBeTrue();
			connectivity.IsConnected(audioSource1, audioDestination1).ShouldBeTrue();
			connectivity.IsConnected(audioSource2, audioDestination1).ShouldBeFalse();

			// Connect same source
			simulation.CreateTestConnection(audioSource1, audioDestination1);
			receivedEvents.Count.ShouldBe(1); // No change, so no new event

			// Connect another source
			simulation.CreateTestConnection(audioSource2, audioDestination1);
			receivedEvents.Count.ShouldBe(2);
			receivedEvents.Last().ShouldBe([audioSource1, audioSource2, audioDestination1], ignoreOrder: true);
			connectivity.IsConnected(audioDestination1).ShouldBeTrue();
			connectivity.IsConnected(audioSource1, audioDestination1).ShouldBeFalse();
			connectivity.IsConnected(audioSource2, audioDestination1).ShouldBeTrue();

			// Connect an unknown source
			simulation.CreateTestConnection(null, audioDestination1);
			receivedEvents.Count.ShouldBe(3);
			receivedEvents.Last().ShouldBe([audioSource2, audioDestination1], ignoreOrder: true);
			connectivity.IsConnected(audioDestination1).ShouldBeTrue();
			connectivity.IsConnected(audioSource1, audioDestination1).ShouldBeFalse();
			connectivity.IsConnected(audioSource2, audioDestination1).ShouldBeFalse();

			// Disconnect
			simulation.TestDisconnectDestination(audioDestination1);
			receivedEvents.Count.ShouldBe(4);
			receivedEvents.Last().ShouldBe([audioDestination1], ignoreOrder: true);
			connectivity.IsConnected(audioDestination1).ShouldBeFalse();
			connectivity.IsConnected(audioSource1, audioDestination1).ShouldBeFalse();
			connectivity.IsConnected(audioSource2, audioDestination1).ShouldBeFalse();
		}
	}
}