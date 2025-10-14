namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	[DoNotParallelize]
	public sealed class MediaOps_LiveApi_Tests_Validation
	{
		[TestInitialize]
		public void TestInitialize()
		{
			// Clear the cached data before each test
			StaticMediaOpsLiveCache.Reset();
		}

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
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.TransportTypes.Create(new TransportType { Name = "IP3" }); });
			Assert.AreEqual("Cannot save transport types. The following names are already in use: IP3", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_TransportTypes_CheckStillInUse()
		{
			var api = new MediaOpsLiveApiMock();

			var transportType = api.TransportTypes.Query().First(x => x.Name == "TSoIP");

			// deleting transport type that is still in use throws exception
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.TransportTypes.Delete(transportType); });
			Assert.AreEqual("One or more transport types are still in use", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Levels_CheckDuplicates()
		{
			var api = new MediaOpsLiveApiMock();

			var transportType = api.TransportTypes.Query().First(x => x.Name == "TSoIP");

			// doesn't throw exception
			var l = new Level { Name = "L1", Number = 101, TransportType = transportType };
			api.Levels.Create(l);

			l.Name = "L2";
			l.Number = 102;
			api.Levels.Update(l);

			// create item with same name
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.Levels.Create(new Level { Name = "L2", Number = 102, TransportType = transportType }); });
			Assert.AreEqual("Cannot save levels. The following names are already in use: L2", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Levels_TransportTypeIsMandatory()
		{
			var api = new MediaOpsLiveApiMock();

			var level = new Level { Name = "L1", Number = 101 };

			var ex = Assert.Throws<Exception>(
				() => { api.Levels.CreateOrUpdate(level); });
			Assert.AreEqual("Validation failed:\r\n- Transport type is mandatory.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Levels_CheckStillInUse()
		{
			var api = new MediaOpsLiveApiMock();

			var level = api.Levels.Query().First(x => x.Name == "Video");

			// deleting level that is still in use throws exception
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.Levels.Delete(level); });
			Assert.AreEqual("One or more levels are still in use", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Endpoints_CheckDuplicates()
		{
			var api = new MediaOpsLiveApiMock();

			var transportType = api.TransportTypes.Query().First(x => x.Name == "TSoIP");

			// doesn't throw exception
			var endpoint = new Endpoint { Name = "E1", Role = Role.Source, TransportType = transportType };
			api.Endpoints.Create(endpoint);

			endpoint.Name = "E2";
			api.Endpoints.Update(endpoint);

			// create item with same name
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.Endpoints.Create(new Endpoint { Name = "E2", Role = Role.Destination, TransportType = transportType }); });
			Assert.AreEqual("Cannot save endpoints. The following names are already in use: E2", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Endpoints_CheckStillInUse()
		{
			var api = new MediaOpsLiveApiMock();

			var endpoint = api.Endpoints.Query().First(x => x.Name == "Video Source 1");

			// deleting endpoint that is still in use throws exception
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.Endpoints.Delete(endpoint); });
			Assert.AreEqual("One or more endpoints are still in use", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Endpoints_TransportTypeIsMandatory()
		{
			var api = new MediaOpsLiveApiMock();

			var endpoint = new Endpoint { Name = "E1", Role = Role.Source };

			var ex = Assert.Throws<Exception>(
				() => { api.Endpoints.CreateOrUpdate(endpoint); });
			Assert.AreEqual("Validation failed:\r\n- Transport type is mandatory.", ex.Message);
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
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.VirtualSignalGroups.Create(new VirtualSignalGroup { Name = "VSG2", Role = Role.Destination }); });
			Assert.AreEqual("Cannot save VSGs. The following names are already in use: VSG2", ex.Message);
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
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.Categories.Create(new Category { Name = "C2" }); });
			Assert.AreEqual("Cannot save categories. The following names are already in use: C2", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Categories_CheckStillInUse()
		{
			var api = new MediaOpsLiveApiMock();

			var category = api.Categories.Query().First(x => x.Name == "Category 1");

			// deleting category that is still in use throws exception
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.Categories.Delete(category); });
			Assert.AreEqual("One or more categories are still in use", ex.Message);
		}
	}
}
