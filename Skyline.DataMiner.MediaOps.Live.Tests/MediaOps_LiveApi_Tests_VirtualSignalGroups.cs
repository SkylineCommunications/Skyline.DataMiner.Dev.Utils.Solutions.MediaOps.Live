namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;
	using Skyline.DataMiner.Utils.Categories.API.Objects;
	using Categories = Skyline.DataMiner.Utils.Categories.API.Objects;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_VirtualSignalGroups
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_GetByEndpoints()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Query().First(x => x.Name == "Video Source 1");
			var videoSource2 = api.Endpoints.Query().First(x => x.Name == "Video Source 2");

			var vsgs = api.VirtualSignalGroups.GetByEndpoints([videoSource1, videoSource2]).ToList();

			Assert.HasCount(2, vsgs);
			CollectionAssert.AreEquivalent(
				new[] { "Source 1", "Source 2" },
				vsgs.Select(x => x.Name).ToList());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_GetByEndpointIds()
		{
			var api = new MediaOpsLiveApiMock();

			var videoSource1 = api.Endpoints.Query().First(x => x.Name == "Video Source 1");
			var videoSource2 = api.Endpoints.Query().First(x => x.Name == "Video Source 2");

			var vsgs = api.VirtualSignalGroups.GetByEndpointIds([videoSource1.ID, videoSource2.ID]).ToList();

			Assert.HasCount(2, vsgs);
			CollectionAssert.AreEquivalent(
				new[] { "Source 1", "Source 2" },
				vsgs.Select(x => x.Name).ToList());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_AssignEndpoint()
		{
			var api = new MediaOpsLiveApiMock();

			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			var videoLevel = api.Levels.Query().First(x => x.Name == "Video");
			var videoSource1 = api.Endpoints.Query().First(x => x.Name == "Video Source 1");

			vsg.Levels.Remove(vsg.Levels.FirstOrDefault(x => x.Level == videoLevel));
			api.VirtualSignalGroups.Update(vsg);
			vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			Assert.HasCount(1, vsg.Levels);

			vsg.Levels.Add(new LevelEndpoint(videoLevel, videoSource1));
			api.VirtualSignalGroups.Update(vsg);
			vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			Assert.HasCount(2, vsg.Levels);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_JoinEndpoints()
		{
			var api = new MediaOpsLiveApiMock();

			// act
			var result = api.VirtualSignalGroups.ReadAllPaged()
				.JoinEndpoints(api.Endpoints)
				.Flatten()
				.ToList();

			// assert
			Assert.IsNotNull(result);
			Assert.IsGreaterThan(0, result.Count, "Expected at least one joined result.");

			foreach (var (virtualSignalGroup, endpoints) in result)
			{
				var vsgName = virtualSignalGroup.Name;

				Assert.IsNotNull(virtualSignalGroup, "VirtualSignalGroup should not be null.");
				Assert.IsNotNull(endpoints, "Endpoints list should not be null.");

				Assert.IsTrue(endpoints.Any(), "Each VSG should have at least one endpoint.");

				foreach (var endpoint in endpoints)
				{
					Assert.EndsWith(vsgName, endpoint.Name, $"Endpoint '{endpoint.Name}' should end with VSG name '{vsgName}'.");
				}
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_Categories_BasicTest()
		{
			var api = new MediaOpsLiveApiMock();

			var category = new Categories.Category { Name = "Category 1" };

			var vsg = api.VirtualSignalGroups.Read("Source 1");
			Assert.IsFalse(vsg.IsAssignedToCategory(category));

			// Assign
			vsg.AssignToCategory(category);
			api.VirtualSignalGroups.Update(vsg);

			vsg = api.VirtualSignalGroups.Read("Source 1");
			Assert.IsTrue(vsg.IsAssignedToCategory(category));
			Assert.ContainsSingle(api.VirtualSignalGroups.GetByCategory(category));

			// Unassign
			vsg.UnassignFromCategory(category);
			api.VirtualSignalGroups.Update(vsg);
			vsg = api.VirtualSignalGroups.Read("Source 1");

			Assert.IsFalse(vsg.IsAssignedToCategory(category));
			Assert.IsEmpty(api.VirtualSignalGroups.GetByCategory(category));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroups_Categories_SyncItems()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;
			var categoriesApi = simulation.CategoriesApi;

			var category1 = new Category { Name = "Category 1" };
			var category2 = new Category { Name = "Category 2" };
			var category3 = new Category { Name = "Category 3" };

			// Test 1: Assign categories to VSGs and verify sync creates category items
			var vsg1 = api.VirtualSignalGroups.Read("Source 1");
			var vsg2 = api.VirtualSignalGroups.Read("Source 2");

			// Clear any pre-existing categories from the simulation
			vsg1.Categories.Clear();
			vsg2.Categories.Clear();
			vsg1 = api.VirtualSignalGroups.Update(vsg1);
			vsg2 = api.VirtualSignalGroups.Update(vsg2);

			// Assign categories
			vsg1.AssignToCategory(category1);
			vsg1.AssignToCategory(category2);
			api.VirtualSignalGroups.Update(vsg1);

			vsg2.AssignToCategory(category1);
			api.VirtualSignalGroups.Update(vsg2);

			// Verify CategoryItems were created in the Categories API
			var categoryItems1 = categoriesApi.CategoryItems.GetChildItems(category1).ToList();
			Assert.HasCount(2, categoryItems1, "Expected 2 category items for category1");

			var categoryItems2 = categoriesApi.CategoryItems.GetChildItems(category2).ToList();
			Assert.ContainsSingle(categoryItems2, "Expected 1 category item for category2");

			// Verify the category items point to the correct VSG instances
			var vsg1CategoryItem1 = categoryItems1.FirstOrDefault(ci => ci.InstanceId == vsg1.ID.ToString());
			Assert.IsNotNull(vsg1CategoryItem1, "Category item for vsg1 in category1 should exist");
			Assert.AreEqual(category1.ID, vsg1CategoryItem1.Category.ID);

			// Verify both VSGs are in category 1
			var vsgsInCategory1 = api.VirtualSignalGroups.GetByCategory(category1).ToList();
			Assert.HasCount(2, vsgsInCategory1);
			CollectionAssert.AreEquivalent(
				new[] { "Source 1", "Source 2" },
				vsgsInCategory1.Select(x => x.Name).ToList());

			// Verify only vsg1 is in category 2
			var vsgsInCategory2 = api.VirtualSignalGroups.GetByCategory(category2).ToList();
			Assert.ContainsSingle(vsgsInCategory2);
			Assert.AreEqual("Source 1", vsgsInCategory2[0].Name);

			// Test 2: Update categories (remove one, add another)
			vsg1 = api.VirtualSignalGroups.Read("Source 1");
			vsg1.UnassignFromCategory(category1);
			vsg1.AssignToCategory(category3);
			api.VirtualSignalGroups.Update(vsg1);

			// Verify the update was persisted - re-read vsg1
			vsg1 = api.VirtualSignalGroups.Read("Source 1");
			Assert.HasCount(2, vsg1.Categories, "vsg1 should have exactly 2 categories after Test 2");
			Assert.IsFalse(vsg1.IsAssignedToCategory(category1), "vsg1 should not have category1 after Test 2");
			Assert.IsTrue(vsg1.IsAssignedToCategory(category2), "vsg1 should still have category2 after Test 2");
			Assert.IsTrue(vsg1.IsAssignedToCategory(category3), "vsg1 should have category3 after Test 2");

			// Verify category item was removed from category1
			categoryItems1 = categoriesApi.CategoryItems.GetChildItems(category1).ToList();
			Assert.ContainsSingle(categoryItems1, "Expected only 1 category item remaining in category1");
			Assert.AreEqual(vsg2.ID.ToString(), categoryItems1[0].InstanceId, "Remaining item should be vsg2");

			// Verify category item was added to category3
			var categoryItems3 = categoriesApi.CategoryItems.GetChildItems(category3).ToList();
			Assert.ContainsSingle(categoryItems3, "Expected 1 category item for category3");
			Assert.AreEqual(vsg1.ID.ToString(), categoryItems3[0].InstanceId);

			// Verify vsg1 is no longer in category 1
			vsgsInCategory1 = api.VirtualSignalGroups.GetByCategory(category1).ToList();
			Assert.ContainsSingle(vsgsInCategory1);
			Assert.AreEqual("Source 2", vsgsInCategory1[0].Name);

			// Test 3: Remove all categories from a VSG
			vsg1 = api.VirtualSignalGroups.Read("Source 1");

			// Verify the state before removing - should have category2 and category3
			Assert.IsTrue(vsg1.IsAssignedToCategory(category2), "vsg1 should be assigned to category2 before removal");
			Assert.IsTrue(vsg1.IsAssignedToCategory(category3), "vsg1 should be assigned to category3 before removal");
			Assert.HasCount(2, vsg1.Categories, "vsg1 should have exactly 2 categories before removal");

			vsg1.UnassignFromCategory(category2);
			vsg1.UnassignFromCategory(category3);

			// Verify categories were removed from the object
			Assert.IsEmpty(vsg1.Categories, "vsg1 should have 0 categories after unassigning");

			api.VirtualSignalGroups.Update(vsg1);

			// Verify all category items for vsg1 were removed
			var allCategoryItemsForVsg1 = categoriesApi.CategoryItems.Read(
				CategoryItemExposers.InstanceId.Equal(vsg1.ID.ToString()))
				.ToList();
			Assert.IsEmpty(allCategoryItemsForVsg1, "All category items for vsg1 should be removed");

			// Verify vsg1 is not in any categories
			vsg1 = api.VirtualSignalGroups.Read("Source 1");
			Assert.IsFalse(vsg1.IsAssignedToCategory(category1));
			Assert.IsFalse(vsg1.IsAssignedToCategory(category2));
			Assert.IsFalse(vsg1.IsAssignedToCategory(category3));

			// Test 4: Delete VSG and verify category items are removed
			vsg2 = api.VirtualSignalGroups.Read("Source 2");
			Assert.IsTrue(vsg2.IsAssignedToCategory(category1));
			api.VirtualSignalGroups.Delete(vsg2);

			// Verify category items for vsg2 were removed from Categories API
			var allCategoryItemsForVsg2 = categoriesApi.CategoryItems.Read(
				CategoryItemExposers.InstanceId.Equal(vsg2.ID.ToString()))
				.ToList();
			Assert.IsEmpty(allCategoryItemsForVsg2, "All category items for vsg2 should be removed after deletion");

			// Verify category 1 is now empty
			vsgsInCategory1 = api.VirtualSignalGroups.GetByCategory(category1).ToList();
			Assert.IsEmpty(vsgsInCategory1);

			// Test 5: Bulk CreateOrUpdate with categories
			var vsg3 = api.VirtualSignalGroups.Read("Source 3");
			var vsg4 = api.VirtualSignalGroups.Read("Source 4");

			// Clear pre-existing categories
			vsg3.Categories.Clear();
			vsg4.Categories.Clear();

			// Assign categories
			vsg3.AssignToCategory(category1);
			vsg4.AssignToCategory(category1);
			vsg4.AssignToCategory(category2);

			api.VirtualSignalGroups.CreateOrUpdate(new[] { vsg3, vsg4 });

			// Verify category items were created for both VSGs in Categories API
			categoryItems1 = categoriesApi.CategoryItems.GetChildItems(category1).ToList();
			Assert.HasCount(2, categoryItems1, "Expected 2 category items for category1 after bulk update");

			var instanceIds = categoryItems1.Select(ci => ci.InstanceId).ToList();
			CollectionAssert.Contains(instanceIds, vsg3.ID.ToString());
			CollectionAssert.Contains(instanceIds, vsg4.ID.ToString());

			categoryItems2 = categoriesApi.CategoryItems.GetChildItems(category2).ToList();
			Assert.ContainsSingle(categoryItems2, "Expected 1 category item for category2 after bulk update");
			Assert.AreEqual(vsg4.ID.ToString(), categoryItems2[0].InstanceId);

			// Verify bulk operation created category items correctly
			vsgsInCategory1 = api.VirtualSignalGroups.GetByCategory(category1).ToList();
			Assert.HasCount(2, vsgsInCategory1);
			CollectionAssert.AreEquivalent(
				new[] { "Source 3", "Source 4" },
				vsgsInCategory1.Select(x => x.Name).ToList());

			vsgsInCategory2 = api.VirtualSignalGroups.GetByCategory(category2).ToList();
			Assert.ContainsSingle(vsgsInCategory2);
			Assert.AreEqual("Source 4", vsgsInCategory2[0].Name);
		}
	}
}
