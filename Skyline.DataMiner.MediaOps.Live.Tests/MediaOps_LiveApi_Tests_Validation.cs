namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Tests.Mocking;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Validation
	{
		private static readonly MediaOpsLiveApi _api = new MediaOpsLiveApiMock();

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_TransportTypes_CheckDuplicates()
		{
			// doesn't throw exception
			var tt = new TransportType { Name = "IP2" };
			_api.TransportTypes.Create(tt);

			tt.Name = "IP3";
			_api.TransportTypes.Update(tt);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { _api.TransportTypes.Create(new TransportType { Name = "IP3" }); });
			Assert.AreEqual("Transport type with same name already exists.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_TransportTypes_CheckStillInUse()
		{
			var transportType = _api.TransportTypes.Query().First(x => x.Name == "IP");

			// deleting transport type that is still in use throws exception
			var ex = Assert.Throws<Exception>(
				() => { _api.TransportTypes.Delete(transportType); });
			Assert.AreEqual("Cannot delete transport type 'IP' because it is still in use.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Levels_CheckDuplicates()
		{
			var transportType = _api.TransportTypes.Query().First(x => x.Name == "IP");

			// doesn't throw exception
			var l = new Level { Name = "L1", Number = 101, TransportType = transportType };
			_api.Levels.Create(l);

			l.Name = "L2";
			l.Number = 102;
			_api.Levels.Update(l);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { _api.Levels.Create(new Level { Name = "L2", Number = 102, TransportType = transportType }); });
			Assert.AreEqual("Level with same name or number already exists.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Levels_CheckStillInUse()
		{
			var level = _api.Levels.Query().First(x => x.Name == "Video");

			// deleting level that is still in use throws exception
			var ex = Assert.Throws<Exception>(
				() => { _api.Levels.Delete(level); });
			Assert.AreEqual("Cannot delete level 'Video' because it is still in use.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Endpoints_CheckDuplicates()
		{
			// doesn't throw exception
			var c = new Endpoint { Name = "E1", Role = Role.Source };
			_api.Endpoints.Create(c);

			c.Name = "E2";
			_api.Endpoints.Update(c);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { _api.Endpoints.Create(new Endpoint { Name = "E2", Role = Role.Destination }); });
			Assert.AreEqual("Endpoint with same name already exists.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Endpoints_CheckStillInUse()
		{
			var endpoint = _api.Endpoints.Query().First(x => x.Name == "Video Source 1");

			// deleting endpoint that is still in use throws exception
			var ex = Assert.Throws<Exception>(
				() => { _api.Endpoints.Delete(endpoint); });
			Assert.AreEqual("Cannot delete endpoint 'Video Source 1' because it is still in use.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_VirtualSignalGroups_CheckDuplicates()
		{
			// doesn't throw exception
			var c = new VirtualSignalGroup { Name = "VSG1", Role = Role.Source };
			_api.VirtualSignalGroups.Create(c);

			c.Name = "VSG2";
			_api.VirtualSignalGroups.Update(c);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { _api.VirtualSignalGroups.Create(new VirtualSignalGroup { Name = "VSG2", Role = Role.Destination }); });
			Assert.AreEqual("Virtual signal group with same name already exists.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Categories_CheckDuplicates()
		{
			// doesn't throw exception
			var c = new Category { Name = "C1" };
			_api.Categories.Create(c);

			c.Name = "C2";
			_api.Categories.Update(c);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { _api.Categories.Create(new Category { Name = "C2" }); });
			Assert.AreEqual("Category with same name already exists.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Categories_CheckStillInUse()
		{
			var category = _api.Categories.Query().First(x => x.Name == "Category 1");

			// deleting category that is still in use throws exception
			var ex = Assert.Throws<Exception>(
				() => { _api.Categories.Delete(category); });
			Assert.AreEqual("Cannot delete category 'Category 1' because it is still in use.", ex.Message);
		}
	}
}
