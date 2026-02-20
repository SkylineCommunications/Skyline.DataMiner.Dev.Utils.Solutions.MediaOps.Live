namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

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
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.TransportTypes.Create(new TransportType { Name = "IP3" }); });
			Assert.AreEqual("Cannot save transport types. The following names are already in use: IP3", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_TransportTypes_CheckDuplicates_WithinBatch()
		{
			var api = new MediaOpsLiveApiMock();

			// two transport types with the same name in the same batch throws
			var ex = Assert.Throws<InvalidOperationException>(
				() => api.TransportTypes.CreateOrUpdate([
					new TransportType { Name = "IP2" },
					new TransportType { Name = "IP2" },
				]));
			Assert.AreEqual("Cannot save transport types. The following names are already in use: IP2", ex.Message);
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
		public void MediaOps_LiveApi_Tests_Validation_Levels_CheckDuplicates_WithinBatch()
		{
			var api = new MediaOpsLiveApiMock();

			var transportType = api.TransportTypes.Query().First(x => x.Name == "TSoIP");

			// two levels with the same name in the same batch throws
			var nameEx = Assert.Throws<InvalidOperationException>(
				() => api.Levels.CreateOrUpdate([
					new Level { Name = "L1", Number = 101, TransportType = transportType },
					new Level { Name = "L1", Number = 102, TransportType = transportType },
				]));
			Assert.AreEqual("Cannot save levels. The following names are already in use: L1", nameEx.Message);

			// two levels with the same number in the same batch throws
			var numberEx = Assert.Throws<InvalidOperationException>(
				() => api.Levels.CreateOrUpdate([
					new Level { Name = "L1", Number = 101, TransportType = transportType },
					new Level { Name = "L2", Number = 101, TransportType = transportType },
				]));
			Assert.AreEqual("Cannot save levels. The following numbers are already in use: 101", numberEx.Message);
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
			var endpoint = new Endpoint { Name = "E1", Role = EndpointRole.Source, TransportType = transportType };
			api.Endpoints.Create(endpoint);

			endpoint.Name = "E2";
			api.Endpoints.Update(endpoint);

			// create item with same name and same role throws
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.Endpoints.Create(new Endpoint { Name = "E2", Role = EndpointRole.Source, TransportType = transportType }); });
			Assert.AreEqual("Cannot save endpoints. The following names are already in use: E2", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Endpoints_CheckDuplicates_DifferentRoleAllowed()
		{
			var api = new MediaOpsLiveApiMock();

			var transportType = api.TransportTypes.Query().First(x => x.Name == "TSoIP");

			// create a source endpoint
			api.Endpoints.Create(new Endpoint { Name = "E1", Role = EndpointRole.Source, TransportType = transportType });

			// a destination endpoint with the same name is allowed
			api.Endpoints.Create(new Endpoint { Name = "E1", Role = EndpointRole.Destination, TransportType = transportType });

			// a second source endpoint with the same name is not allowed
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.Endpoints.Create(new Endpoint { Name = "E1", Role = EndpointRole.Source, TransportType = transportType }); });
			Assert.AreEqual("Cannot save endpoints. The following names are already in use: E1", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Endpoints_CheckDuplicates_WithinBatch()
		{
			var api = new MediaOpsLiveApiMock();

			var transportType = api.TransportTypes.Query().First(x => x.Name == "TSoIP");

			// two endpoints with the same name and same role in the same batch throws
			var ex = Assert.Throws<InvalidOperationException>(
				() => api.Endpoints.CreateOrUpdate([
					new Endpoint { Name = "E1", Role = EndpointRole.Source, TransportType = transportType },
					new Endpoint { Name = "E1", Role = EndpointRole.Source, TransportType = transportType },
				]));
			Assert.AreEqual("Cannot save endpoints. The following names are already in use: E1", ex.Message);

			// two endpoints with the same name but different roles in the same batch is allowed
			api.Endpoints.CreateOrUpdate([
				new Endpoint { Name = "E2", Role = EndpointRole.Source, TransportType = transportType },
				new Endpoint { Name = "E2", Role = EndpointRole.Destination, TransportType = transportType },
			]);
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

			var endpoint = new Endpoint { Name = "E1", Role = EndpointRole.Source };

			var ex = Assert.Throws<Exception>(
				() => { api.Endpoints.CreateOrUpdate(endpoint); });
			Assert.AreEqual("Validation failed:\r\n- Transport type is mandatory.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_VirtualSignalGroups_CheckDuplicates()
		{
			var api = new MediaOpsLiveApiMock();

			// doesn't throw exception
			var c = new VirtualSignalGroup { Name = "VSG1", Role = EndpointRole.Source };
			api.VirtualSignalGroups.Create(c);

			c.Name = "VSG2";
			api.VirtualSignalGroups.Update(c);

			// create item with same name and same role throws
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.VirtualSignalGroups.Create(new VirtualSignalGroup { Name = "VSG2", Role = EndpointRole.Source }); });
			Assert.AreEqual("Cannot save VSGs. The following names are already in use: VSG2", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_VirtualSignalGroups_CheckDuplicates_DifferentRoleAllowed()
		{
			var api = new MediaOpsLiveApiMock();

			// create a source VSG
			api.VirtualSignalGroups.Create(new VirtualSignalGroup { Name = "VSG1", Role = EndpointRole.Source });

			// a destination VSG with the same name is allowed
			api.VirtualSignalGroups.Create(new VirtualSignalGroup { Name = "VSG1", Role = EndpointRole.Destination });

			// a second source VSG with the same name is not allowed
			var ex = Assert.Throws<InvalidOperationException>(
				() => { api.VirtualSignalGroups.Create(new VirtualSignalGroup { Name = "VSG1", Role = EndpointRole.Source }); });
			Assert.AreEqual("Cannot save VSGs. The following names are already in use: VSG1", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_VirtualSignalGroups_CheckDuplicates_WithinBatch()
		{
			var api = new MediaOpsLiveApiMock();

			// two VSGs with the same name and same role in the same batch throws
			var ex = Assert.Throws<InvalidOperationException>(
				() => api.VirtualSignalGroups.CreateOrUpdate([
					new VirtualSignalGroup { Name = "VSG1", Role = EndpointRole.Source },
					new VirtualSignalGroup { Name = "VSG1", Role = EndpointRole.Source },
				]));
			Assert.AreEqual("Cannot save VSGs. The following names are already in use: VSG1", ex.Message);

			// two VSGs with the same name but different roles in the same batch is allowed
			api.VirtualSignalGroups.CreateOrUpdate([
				new VirtualSignalGroup { Name = "VSG2", Role = EndpointRole.Source },
				new VirtualSignalGroup { Name = "VSG2", Role = EndpointRole.Destination },
			]);
		}
	}
}
