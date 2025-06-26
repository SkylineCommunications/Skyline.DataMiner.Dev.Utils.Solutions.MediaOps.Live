namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Querying;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Api
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_CountAll()
		{
			var api = new MediaOpsLiveApiMock();

			var endpointCount = api.Endpoints.CountAll();
			Assert.AreEqual(40, endpointCount);

			var vsgCount = api.VirtualSignalGroups.CountAll();
			Assert.AreEqual(20, vsgCount);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Count()
		{
			var api = new MediaOpsLiveApiMock();

			var endpointFilter = EndpointExposers.Name.Contains("Source");
			var endpointCount = api.Endpoints.Count(endpointFilter);
			Assert.AreEqual(20, endpointCount);

			var vsgFilter = VirtualSignalGroupExposers.Role.UncheckedEqual(Role.Destination);
			var vsgCount = api.VirtualSignalGroups.Count(vsgFilter);
			Assert.AreEqual(10, vsgCount);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ReadAll()
		{
			var api = new MediaOpsLiveApiMock();

			var endpoints = api.Endpoints.ReadAll().ToList();
			Assert.AreEqual(40, endpoints.Count);
			CollectionAssert.AllItemsAreUnique(endpoints);

			var vsgs = api.VirtualSignalGroups.ReadAll().ToList();
			Assert.AreEqual(20, vsgs.Count);
			CollectionAssert.AllItemsAreUnique(vsgs);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Read()
		{
			var api = new MediaOpsLiveApiMock();

			var endpointFilter = EndpointExposers.Name.Contains("Video Source")
				.OR(EndpointExposers.Name.Contains("Audio Source"));
			var endpoints = api.Endpoints.Read(endpointFilter).ToList();
			Assert.AreEqual(20, endpoints.Count);
			Assert.IsTrue(endpoints.All(x => x.Name.Contains("Video Source") || x.Name.Contains("Audio Source")));
			CollectionAssert.AllItemsAreUnique(endpoints);

			var levelFilter = LevelExposers.Number.Equal(1);
			var levels = api.Levels.Read(levelFilter).ToList();
			Assert.AreEqual(1, levels.Count);
			Assert.AreEqual("Video", levels[0].Name);

			var vsgFilter = VirtualSignalGroupExposers.Endpoint.Contains(endpoints[0]);
			var vsgs = api.VirtualSignalGroups.Read(vsgFilter).ToList();
			Assert.AreEqual(1, vsgs.Count);
			Assert.IsTrue(vsgs.All(vsg => vsg.Levels.Any(le => le.Endpoint == endpoints[0])));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Read_Ids()
		{
			var api = new MediaOpsLiveApiMock();

			var endpoints = api.Endpoints.ReadAll().ToList();
			var id0 = endpoints[0].ID;
			var id1 = endpoints[1].ID;

			var endpoint_read = api.Endpoints.Read(id0);
			Assert.AreEqual(endpoints[0], endpoint_read);

			var endpoints_read = api.Endpoints.Read([id0, id1]);
			Assert.AreEqual(2, endpoints_read.Count);
			CollectionAssert.AreEqual(
				new[] { endpoints[0], endpoints[1] },
				new[] { endpoints_read[id0], endpoints_read[id1] });
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Read_Query()
		{
			var api = new MediaOpsLiveApiMock();

			var endpointFilter = EndpointExposers.Name.Contains("Source");
			var endpointQuery = endpointFilter.Limit(5);

			var endpoints_filter = api.Endpoints.Read(endpointFilter).ToList();
			Assert.AreEqual(20, endpoints_filter.Count);

			var endpoints_query = api.Endpoints.Read(endpointQuery).ToList();
			Assert.AreEqual(5, endpoints_query.Count);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Update()
		{
			var api = new MediaOpsLiveApiMock();

			var endpoints = api.Endpoints.ReadAll().ToList();

			var endpoint0 = endpoints[0];
			var id0 = endpoints[0].ID;

			endpoint0.Element = "10/123";
			api.Endpoints.Update(endpoint0);

			var endpoint_read = api.Endpoints.Read(id0);
			Assert.AreEqual("10/123", endpoint_read.Element);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Delete()
		{
			var api = new MediaOpsLiveApiMock();

			var vsgName = "Source 10";
			Assert.AreEqual(1, api.VirtualSignalGroups.Query().Count(x => x.Name == vsgName));

			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == vsgName);
			api.VirtualSignalGroups.Delete(vsg);

			Assert.AreEqual(0, api.VirtualSignalGroups.Query().Count(x => x.Name == vsgName));
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_DeleteAll()
		{
			var api = new MediaOpsLiveApiMock();

			var allVirtualSignalGroups = api.VirtualSignalGroups.ReadAll().ToList();
			api.VirtualSignalGroups.Delete(allVirtualSignalGroups);

			Assert.AreEqual(0, api.VirtualSignalGroups.CountAll());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_CreateDelete()
		{
			var api = new MediaOpsLiveApiMock();

			var ip = api.TransportTypes.ReadAll().Single();

			CollectionAssert.AreEquivalent(
				new[] { ip },
				api.TransportTypes.ReadAll().ToList());

			var sdi = new TransportType { Name = "SDI" };
			api.TransportTypes.Create(sdi);

			CollectionAssert.AreEquivalent(
				new[] { ip, sdi },
				api.TransportTypes.ReadAll().ToList());

			api.TransportTypes.Delete(sdi);

			CollectionAssert.AreEquivalent(
				new[] { ip },
				api.TransportTypes.ReadAll().ToList());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Join()
		{
			var api = new MediaOpsLiveApiMock();

			// act
			var levelsWithTransportType = api.Levels.ReadAllPaged()
				.JoinInBatches(
					api.TransportTypes,
					level => level.TransportType,
					(l, t) => new { Level = l, TransportType = t })
				.Flatten()
				.ToList();

			// assert
			var ip = api.TransportTypes.Query().First(x => x.Name == "IP");
			var video = api.Levels.Query().First(x => x.Name == "Video");
			var audio = api.Levels.Query().First(x => x.Name == "Audio");
			var data = api.Levels.Query().First(x => x.Name == "Data");

			CollectionAssert.AreEquivalent(
				new[]
				{
					new { Level = video, TransportType = ip },
					new { Level = audio, TransportType = ip },
					new { Level = data, TransportType = ip },
				},
				levelsWithTransportType);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_JoinInBatches_ById()
		{
			var api = new MediaOpsLiveApiMock();

			// act
			var levelsWithTransportType = api.Levels.ReadAll()
				.Batch(100)
				.JoinInBatches(
					api.TransportTypes,
					level => level.TransportType,
					(l, t) => new { Level = l, TransportType = t })
				.Flatten()
				.ToList();

			// assert
			var ip = api.TransportTypes.Query().First(x => x.Name == "IP");
			var video = api.Levels.Query().First(x => x.Name == "Video");
			var audio = api.Levels.Query().First(x => x.Name == "Audio");
			var data = api.Levels.Query().First(x => x.Name == "Data");

			CollectionAssert.AreEquivalent(
				new[]
				{
					new { Level = video, TransportType = ip },
					new { Level = audio, TransportType = ip },
					new { Level = data, TransportType = ip },
				},
				levelsWithTransportType);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_JoinInBatches_ByName()
		{
			var api = new MediaOpsLiveApiMock();

			var names = new[] { "Video", "Audio", "Data" };

			// act
			var levels = names
				.Batch(100)
				.JoinInBatches(
					x => x,
					api.Levels.Read,
					(n, l) => new { Name = n, Level = l })
				.Flatten()
				.ToList();

			// assert
			var video = api.Levels.Query().First(x => x.Name == "Video");
			var audio = api.Levels.Query().First(x => x.Name == "Audio");
			var data = api.Levels.Query().First(x => x.Name == "Data");

			CollectionAssert.AreEquivalent(
				new[]
				{
					new { Name = "Video", Level = video },
					new { Name = "Audio", Level = audio },
					new { Name = "Data", Level = data },
				},
				levels);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_JoinMultiple()
		{
			var api = new MediaOpsLiveApiMock();

			// act
			var result = api.VirtualSignalGroups.ReadAllPaged()
				.JoinInBatches(
					api.Endpoints,
					vsg => vsg.GetLevelEndpoints().Select(x => x.Endpoint),
					(vsg, endpoints) => new { VirtualSignalGroup = vsg, Endpoints = endpoints })
				.JoinInBatches(
					api.Levels,
					vsg => vsg.VirtualSignalGroup.GetLevelEndpoints().Select(x => x.Level),
					(vsg, levels) => new { vsg.VirtualSignalGroup, vsg.Endpoints, Levels = levels })
				.Flatten()
				.ToList();

			// assert
			Assert.IsNotNull(result);
			Assert.IsTrue(result.Count > 0, "Expected at least one joined result.");

			foreach (var item in result)
			{
				var vsg = item.VirtualSignalGroup;
				var endpoints = item.Endpoints;
				var levels = item.Levels;

				Assert.IsNotNull(vsg, "VirtualSignalGroup should not be null.");

				Assert.IsNotNull(endpoints, "Endpoints list should not be null.");
				Assert.IsTrue(endpoints.Any(), "Each VSG should have at least one endpoint.");

				Assert.IsNotNull(levels, "Levels list should not be null.");
				Assert.IsTrue(levels.Any(), "Each VSG should have at least one level.");

				foreach (var endpoint in endpoints)
				{
					Assert.IsTrue(
						endpoint.Name.EndsWith(vsg.Name),
						$"Endpoint '{endpoint.Name}' should end with VSG name '{vsg.Name}'.");
				}
			}
		}
	}
}
