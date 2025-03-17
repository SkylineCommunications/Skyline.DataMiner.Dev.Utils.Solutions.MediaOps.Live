namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Analytics.GenericInterface.JoinFilter;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Querying;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests
	{
		private static readonly MediaOpsLiveApi _api = new MediaOpsLiveApiMock();

		[TestMethod]
		public void MediaOps_LiveApi_Tests_CountAll()
		{
			var endpointCount = _api.Endpoints.CountAll();
			Assert.AreEqual(40, endpointCount);

			var vsgCount = _api.VirtualSignalGroups.CountAll();
			Assert.AreEqual(20, vsgCount);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Count()
		{
			var endpointFilter = EndpointExposers.Name.Contains("Source");
			var endpointCount = _api.Endpoints.Count(endpointFilter);
			Assert.AreEqual(20, endpointCount);

			var vsgFilter = VirtualSignalGroupExposers.Role.UncheckedEqual(Role.Destination);
			var vsgCount = _api.VirtualSignalGroups.Count(vsgFilter);
			Assert.AreEqual(10, vsgCount);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ReadAll()
		{
			var endpoints = _api.Endpoints.ReadAll().ToList();
			Assert.AreEqual(40, endpoints.Count);
			CollectionAssert.AllItemsAreUnique(endpoints);

			var vsgs = _api.VirtualSignalGroups.ReadAll().ToList();
			Assert.AreEqual(20, vsgs.Count);
			CollectionAssert.AllItemsAreUnique(vsgs);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Read()
		{
			var endpointFilter = EndpointExposers.Name.Contains("Video Source")
				.OR(EndpointExposers.Name.Contains("Audio Source"));
			var endpoints = _api.Endpoints.Read(endpointFilter).ToList();
			Assert.AreEqual(20, endpoints.Count);
			Assert.IsTrue(endpoints.All(x => x.Name.Contains("Video Source") || x.Name.Contains("Audio Source")));
			CollectionAssert.AllItemsAreUnique(endpoints);

			var levelFilter = LevelExposers.Number.Equal(1);
			var levels = _api.Levels.Read(levelFilter).ToList();
			Assert.AreEqual(1, levels.Count);
			Assert.AreEqual("Video", levels[0].Name);

			var vsgFilter = VirtualSignalGroupExposers.Endpoint.Contains(endpoints[0]);
			var vsgs = _api.VirtualSignalGroups.Read(vsgFilter).ToList();
			Assert.AreEqual(1, vsgs.Count);
			Assert.IsTrue(vsgs.All(vsg => vsg.Levels.Any(le => le.Endpoint == endpoints[0])));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Read_Ids()
		{
			var endpoints = _api.Endpoints.ReadAll().ToList();
			var id0 = endpoints[0].ID;
			var id1 = endpoints[1].ID;

			var endpoint_read = _api.Endpoints.Read(id0);
			Assert.AreEqual(endpoints[0], endpoint_read);

			var endpoints_read = _api.Endpoints.Read([id0, id1]);
			Assert.AreEqual(2, endpoints_read.Count);
			CollectionAssert.AreEqual(
				new[] { endpoints[0], endpoints[1] },
				new[] { endpoints_read[id0], endpoints_read[id1] });
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Read_Query()
		{
			var endpointFilter = EndpointExposers.Name.Contains("Source");
			var endpointQuery = endpointFilter.Limit(5);

			var endpoints_filter = _api.Endpoints.Read(endpointFilter).ToList();
			Assert.AreEqual(20, endpoints_filter.Count);

			var endpoints_query = _api.Endpoints.Read(endpointQuery).ToList();
			Assert.AreEqual(5, endpoints_query.Count);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Update()
		{
			var endpoints = _api.Endpoints.ReadAll().ToList();

			var endpoint0 = endpoints[0];
			var id0 = endpoints[0].ID;

			endpoint0.Element = "10/123";
			_api.Endpoints.Update(endpoint0);

			var endpoint_read = _api.Endpoints.Read(id0);
			Assert.AreEqual("10/123", endpoint_read.Element);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_CreateDelete()
		{
			var ip = _api.TransportTypes.ReadAll().Single();

			CollectionAssert.AreEquivalent(
				new[] { ip },
				_api.TransportTypes.ReadAll().ToList());

			var sdi = new TransportType { Name = "SDI" };
			_api.TransportTypes.Create(sdi);

			CollectionAssert.AreEquivalent(
				new[] { ip, sdi },
				_api.TransportTypes.ReadAll().ToList());

			_api.TransportTypes.Delete(sdi);

			CollectionAssert.AreEquivalent(
				new[] { ip },
				_api.TransportTypes.ReadAll().ToList());
		}
	}
}
