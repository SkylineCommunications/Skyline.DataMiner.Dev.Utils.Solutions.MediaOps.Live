namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Validation
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_TransportTypes_CheckDuplicates()
		{
			var api = new MediaOpsLiveApiMock();

			// doesn't throw exception
			var tt = new TransportType { Name = "IP2" };
			api.TransportTypes.Create(tt);

			tt.Name = "IP3";
			api.TransportTypes.Update(tt);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { api.TransportTypes.Create(new TransportType { Name = "IP3" }); });
			Assert.AreEqual("Transport type with same name already exists.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_TransportTypes_CheckStillInUse()
		{
			var api = new MediaOpsLiveApiMock();

			var transportType = api.TransportTypes.Query().First(x => x.Name == "IP");

			// deleting transport type that is still in use throws exception
			var ex = Assert.Throws<Exception>(
				() => { api.TransportTypes.Delete(transportType); });
			Assert.AreEqual("Cannot delete transport type 'IP' because it is still in use.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Levels_CheckDuplicates()
		{
			var api = new MediaOpsLiveApiMock();

			var transportType = api.TransportTypes.Query().First(x => x.Name == "IP");

			// doesn't throw exception
			var l = new Level { Name = "L1", Number = 101, TransportType = transportType };
			api.Levels.Create(l);

			l.Name = "L2";
			l.Number = 102;
			api.Levels.Update(l);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { api.Levels.Create(new Level { Name = "L2", Number = 102, TransportType = transportType }); });
			Assert.AreEqual("Level with same name or number already exists.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Levels_CheckStillInUse()
		{
			var api = new MediaOpsLiveApiMock();

			var level = api.Levels.Query().First(x => x.Name == "Video");

			// deleting level that is still in use throws exception
			var ex = Assert.Throws<Exception>(
				() => { api.Levels.Delete(level); });
			Assert.AreEqual("Cannot delete level 'Video' because it is still in use.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Endpoints_CheckDuplicates()
		{
			var api = new MediaOpsLiveApiMock();

			// doesn't throw exception
			var c = new Endpoint { Name = "E1", Role = Role.Source };
			api.Endpoints.Create(c);

			c.Name = "E2";
			api.Endpoints.Update(c);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { api.Endpoints.Create(new Endpoint { Name = "E2", Role = Role.Destination }); });
			Assert.AreEqual("Endpoint with same name already exists.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Endpoints_CheckStillInUse()
		{
			var api = new MediaOpsLiveApiMock();

			var endpoint = api.Endpoints.Query().First(x => x.Name == "Video Source 1");

			// deleting endpoint that is still in use throws exception
			var ex = Assert.Throws<Exception>(
				() => { api.Endpoints.Delete(endpoint); });
			Assert.AreEqual("Cannot delete endpoint 'Video Source 1' because it is still in use.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_VirtualSignalGroups_CheckDuplicates()
		{
			var api = new MediaOpsLiveApiMock();

			// doesn't throw exception
			var c = new VirtualSignalGroup { Name = "VSG1", Role = Role.Source };
			api.VirtualSignalGroups.Create(c);

			c.Name = "VSG2";
			api.VirtualSignalGroups.Update(c);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { api.VirtualSignalGroups.Create(new VirtualSignalGroup { Name = "VSG2", Role = Role.Destination }); });
			Assert.AreEqual("Virtual signal group with same name already exists.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Categories_CheckDuplicates()
		{
			var api = new MediaOpsLiveApiMock();

			// doesn't throw exception
			var c = new Category { Name = "C1" };
			api.Categories.Create(c);

			c.Name = "C2";
			api.Categories.Update(c);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { api.Categories.Create(new Category { Name = "C2" }); });
			Assert.AreEqual("Category with same name already exists.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Categories_CheckStillInUse()
		{
			var api = new MediaOpsLiveApiMock();

			var category = api.Categories.Query().First(x => x.Name == "Category 1");

			// deleting category that is still in use throws exception
			var ex = Assert.Throws<Exception>(
				() => { api.Categories.Delete(category); });
			Assert.AreEqual("Cannot delete category 'Category 1' because it is still in use.", ex.Message);
		}
	}
}
