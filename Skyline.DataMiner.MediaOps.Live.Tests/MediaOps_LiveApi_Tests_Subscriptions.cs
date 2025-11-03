namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.MediaOps.Live.API.TransportTypes;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Subscriptions
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_Subscriptions_CreateWithFilter()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();

			var endpointX = new Endpoint { Name = "Endpoint X", Role = EndpointRole.Source, TransportType = PredefinedTransportTypes.TSoIP };
			var endpointY = new Endpoint { Name = "Endpoint Y", Role = EndpointRole.Source, TransportType = PredefinedTransportTypes.TSoIP };

			var receivedEvents = new List<ApiObjectsChangedEvent<Endpoint>>();

			var filter = EndpointExposers.Name.Equal(endpointX.Name);
			using var subscription = api.Endpoints.Subscribe(filter);
			subscription.Changed += (s, e) => receivedEvents.Add(e);

			// Act
			api.Endpoints.Create(endpointX); // matches filter
			api.Endpoints.Create(endpointY); // does not match filter

			// Assert
			Assert.HasCount(1, receivedEvents);

			var receivedEvent = receivedEvents[0];
			CollectionAssert.AreEquivalent(new[] { endpointX }, receivedEvent.Created.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Endpoint>(), receivedEvent.Updated.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Endpoint>(), receivedEvent.Deleted.ToArray());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Subscriptions_CreateWithoutFilter()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();

			var endpointX = new Endpoint { Name = "Endpoint X", Role = EndpointRole.Source, TransportType = PredefinedTransportTypes.TSoIP };
			var endpointY = new Endpoint { Name = "Endpoint Y", Role = EndpointRole.Source, TransportType = PredefinedTransportTypes.TSoIP };

			var receivedEvents = new List<ApiObjectsChangedEvent<Endpoint>>();

			using var subscription = api.Endpoints.Subscribe();
			subscription.Changed += (s, e) => receivedEvents.Add(e);

			// Act
			api.Endpoints.Create(endpointX);
			api.Endpoints.Create(endpointY);

			// Assert
			Assert.HasCount(2, receivedEvents);

			var receivedEvent1 = receivedEvents[0];
			CollectionAssert.AreEquivalent(new[] { endpointX }, receivedEvent1.Created.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Endpoint>(), receivedEvent1.Updated.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Endpoint>(), receivedEvent1.Deleted.ToArray());

			var receivedEvent2 = receivedEvents[1];
			CollectionAssert.AreEquivalent(new[] { endpointY }, receivedEvent2.Created.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Endpoint>(), receivedEvent2.Updated.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Endpoint>(), receivedEvent2.Deleted.ToArray());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Subscriptions_Update()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();

			var endpoint = new Endpoint { Name = "Endpoint X", Role = EndpointRole.Source, TransportType = PredefinedTransportTypes.TSoIP };
			api.Endpoints.Create(endpoint);

			var receivedEvents = new List<ApiObjectsChangedEvent<Endpoint>>();

			using var subscription = api.Endpoints.Subscribe();
			subscription.Changed += (s, e) => receivedEvents.Add(e);

			// Act
			api.Endpoints.Update(endpoint);

			// Assert
			Assert.HasCount(1, receivedEvents);

			var receivedEvent = receivedEvents[0];
			CollectionAssert.AreEquivalent(Array.Empty<Endpoint>(), receivedEvent.Created.ToArray());
			CollectionAssert.AreEquivalent(new[] { endpoint }, receivedEvent.Updated.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Endpoint>(), receivedEvent.Deleted.ToArray());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Subscriptions_Delete()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();

			var endpoint = new Endpoint { Name = "Endpoint X", Role = EndpointRole.Source, TransportType = PredefinedTransportTypes.TSoIP };
			api.Endpoints.Create(endpoint);

			var receivedEvents = new List<ApiObjectsChangedEvent<Endpoint>>();

			using var subscription = api.Endpoints.Subscribe();
			subscription.Changed += (s, e) => receivedEvents.Add(e);

			// Act
			api.Endpoints.Delete(endpoint);

			// Assert
			Assert.HasCount(1, receivedEvents);

			var receivedEvent = receivedEvents[0];
			CollectionAssert.AreEquivalent(Array.Empty<Endpoint>(), receivedEvent.Created.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Endpoint>(), receivedEvent.Updated.ToArray());
			CollectionAssert.AreEquivalent(new[] { endpoint }, receivedEvent.Deleted.ToArray());
		}
	}
}
