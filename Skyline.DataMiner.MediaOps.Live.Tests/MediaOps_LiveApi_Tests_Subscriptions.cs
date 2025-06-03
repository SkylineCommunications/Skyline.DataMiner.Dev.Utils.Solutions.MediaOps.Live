namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Subscriptions
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_Subscriptions_CreateWithFilter()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();

			var categoryX = new Category { Name = "Category X" };
			var categoryY = new Category { Name = "Category Y" };

			var receivedEvents = new List<ApiObjectsChangedEvent<Category>>();

			var filter = CategoryExposers.Name.Equal(categoryX.Name);
			using var subscription = api.Categories.Subscribe(filter);
			subscription.Changed += (s, e) => receivedEvents.Add(e);

			// Act
			api.Categories.Create(categoryX); // matches filter
			api.Categories.Create(categoryY); // does not match filter

			// Assert
			Assert.AreEqual(1, receivedEvents.Count);

			var receivedEvent = receivedEvents[0];
			CollectionAssert.AreEquivalent(new[] { categoryX }, receivedEvent.Created.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Category>(), receivedEvent.Updated.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Category>(), receivedEvent.Deleted.ToArray());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Subscriptions_CreateWithoutFilter()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();

			var categoryX = new Category { Name = "Category X" };
			var categoryY = new Category { Name = "Category Y" };

			var receivedEvents = new List<ApiObjectsChangedEvent<Category>>();

			using var subscription = api.Categories.Subscribe();
			subscription.Changed += (s, e) => receivedEvents.Add(e);

			// Act
			api.Categories.Create(categoryX);
			api.Categories.Create(categoryY);

			// Assert
			Assert.AreEqual(2, receivedEvents.Count);

			var receivedEvent1 = receivedEvents[0];
			CollectionAssert.AreEquivalent(new[] { categoryX }, receivedEvent1.Created.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Category>(), receivedEvent1.Updated.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Category>(), receivedEvent1.Deleted.ToArray());

			var receivedEvent2 = receivedEvents[1];
			CollectionAssert.AreEquivalent(new[] { categoryY }, receivedEvent2.Created.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Category>(), receivedEvent2.Updated.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Category>(), receivedEvent2.Deleted.ToArray());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Subscriptions_Update()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();

			var category = new Category { Name = "Category X" };
			api.Categories.Create(category);

			var receivedEvents = new List<ApiObjectsChangedEvent<Category>>();

			using var subscription = api.Categories.Subscribe();
			subscription.Changed += (s, e) => receivedEvents.Add(e);

			// Act
			api.Categories.Update(category);

			// Assert
			Assert.AreEqual(1, receivedEvents.Count);

			var receivedEvent = receivedEvents[0];
			CollectionAssert.AreEquivalent(Array.Empty<Category>(), receivedEvent.Created.ToArray());
			CollectionAssert.AreEquivalent(new[] { category }, receivedEvent.Updated.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Category>(), receivedEvent.Deleted.ToArray());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Subscriptions_Delete()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();

			var category = new Category { Name = "Category X" };
			api.Categories.Create(category);

			var receivedEvents = new List<ApiObjectsChangedEvent<Category>>();

			using var subscription = api.Categories.Subscribe();
			subscription.Changed += (s, e) => receivedEvents.Add(e);

			// Act
			api.Categories.Delete(category);

			// Assert
			Assert.AreEqual(1, receivedEvents.Count);

			var receivedEvent = receivedEvents[0];
			CollectionAssert.AreEquivalent(Array.Empty<Category>(), receivedEvent.Created.ToArray());
			CollectionAssert.AreEquivalent(Array.Empty<Category>(), receivedEvent.Updated.ToArray());
			CollectionAssert.AreEquivalent(new[] { category }, receivedEvent.Deleted.ToArray());
		}
	}
}
