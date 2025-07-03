namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Query
	{
		private static readonly MediaOpsLiveApi _api = new MediaOpsLiveApiMock();

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_GetAll()
		{
			var endpoints = _api.Endpoints.Query().ToList();
			Assert.AreEqual(40, endpoints.Count);
			CollectionAssert.AllItemsAreUnique(endpoints);
			CollectionAssert.AreEquivalent(
				_api.Endpoints.ReadAll().ToList(),
				endpoints);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_Where()
		{
			{
				var endpoints = _api.Endpoints.Query().Where(x => !(x.ID == Guid.Empty)).ToList();
				Assert.AreEqual(40, endpoints.Count);
				CollectionAssert.AreEquivalent(
					_api.Endpoints.ReadAll().Where(x => !(x.ID == Guid.Empty)).ToList(),
					endpoints);
			}

			{
				var endpoints = _api.Endpoints.Query().Where(x => x.Role == Role.Source).ToList();
				Assert.AreEqual(20, endpoints.Count);
				CollectionAssert.AreEquivalent(
					_api.Endpoints.ReadAll().Where(x => x.Role == Role.Source).ToList(),
					endpoints);
			}

			{
				var source = 1;
				var endpoints = _api.Endpoints.Query().Where(x => x.Name == "Video Source " + source || x.Name == "Audio Source " + source).ToList();
				Assert.AreEqual(2, endpoints.Count);
				CollectionAssert.AreEquivalent(
					_api.Endpoints.ReadAll().Where(x => x.Name == "Video Source " + source || x.Name == "Audio Source " + source).ToList(),
					endpoints);
			}

			{
				var endpoints = _api.Endpoints.Query().Where(x => x.Name.Contains("Video")).ToList();
				Assert.AreEqual(20, endpoints.Count);
				CollectionAssert.AreEquivalent(
					_api.Endpoints.ReadAll().Where(x => x.Name.Contains("Video")).ToList(),
					endpoints);
			}

			{
				var endpoints = _api.Endpoints.Query().Where(x => !x.Name.Contains("Video")).ToList();
				Assert.AreEqual(20, endpoints.Count);
				CollectionAssert.AreEquivalent(
					_api.Endpoints.ReadAll().Where(x => !x.Name.Contains("Video")).ToList(),
					endpoints);
			}

			{
				var list = new List<Guid>();

				var exception = Assert.ThrowsExactly<NotSupportedException>(
					() => _api.Endpoints.Query()
						.Where(x => x.Role == Role.Source && !list.Contains(x.ID))
						.ToList());
				Assert.AreEqual("Unsupported method call: Boolean Contains(System.Guid)", exception.Message);
			}
		}


		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_FilterCollection()
		{
			var videoSource1 = _api.Endpoints.Query().First(x => x.Name == "Video Source 1");
			var videoSource2 = _api.Endpoints.Query().First(x => x.Name == "Video Source 2");

			{
				var virtualSignalGroups = _api.VirtualSignalGroups.Query()
					.Where(x => x.Levels.Any(l => l.Endpoint == videoSource1))
					.ToList();

				Assert.AreEqual(1, virtualSignalGroups.Count);
				Assert.AreEqual("Source 1", virtualSignalGroups[0].Name);
			}

			{
				var virtualSignalGroups = _api.VirtualSignalGroups.Query()
					.Where(x => x.Levels.Any(l => l.Endpoint == videoSource1 || l.Endpoint == videoSource2))
					.ToList();

				Assert.AreEqual(2, virtualSignalGroups.Count);
				CollectionAssert.AreEquivalent(
					new[] { "Source 1", "Source 2" },
					virtualSignalGroups.Select(x => x.Name).ToArray());
			}

			{
				var virtualSignalGroups = _api.VirtualSignalGroups.Query()
					.Where(x => x.Levels.Any(l => (Guid)l.Endpoint == videoSource1.ID))
					.ToList();

				Assert.AreEqual(1, virtualSignalGroups.Count);
				Assert.AreEqual("Source 1", virtualSignalGroups[0].Name);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_First()
		{
			{
				var endpoint = _api.Endpoints.Query().First();
				Assert.IsNotNull(endpoint);
			}

			{
				var endpoint = _api.Endpoints.Query().First(x => x.Name == "Video Source 1");
				Assert.IsNotNull(endpoint);
				Assert.AreEqual("Video Source 1", endpoint.Name);
			}

			{
				var exception = Assert.ThrowsExactly<InvalidOperationException>(
					() => _api.Endpoints.Query().First(x => x.ID == Guid.NewGuid()));
				Assert.AreEqual("Sequence contains no matching element", exception.Message);
			}

			{
				var endpoint = _api.Endpoints.Query().FirstOrDefault();
				Assert.IsNotNull(endpoint);
			}

			{
				var endpoint = _api.Endpoints.Query().FirstOrDefault(x => x.Name == "Video Source 1");
				Assert.IsNotNull(endpoint);
				Assert.AreEqual("Video Source 1", endpoint.Name);
			}

			{
				var endpoint = _api.Endpoints.Query().FirstOrDefault(x => x.ID == Guid.NewGuid());
				Assert.IsNull(endpoint);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_Single()
		{
			{
				var transportType = _api.TransportTypes.Query().Single();
				Assert.IsNotNull(transportType);
			}

			{
				var endpoint = _api.Endpoints.Query().Single(x => x.Name == "Video Source 1");
				Assert.IsNotNull(endpoint);
				Assert.AreEqual("Video Source 1", endpoint.Name);
			}

			{
				var exception = Assert.ThrowsExactly<InvalidOperationException>(
					() => _api.Endpoints.Query().Single(x => x.ID == Guid.NewGuid()));
				Assert.AreEqual("Sequence contains no matching element", exception.Message);
			}

			{
				var exception = Assert.ThrowsExactly<InvalidOperationException>(
					() => _api.Endpoints.Query().Single(x => x.Role == Role.Destination));
				Assert.AreEqual("Sequence contains more than one matching element", exception.Message);
			}

			{
				var transportType = _api.TransportTypes.Query().SingleOrDefault();
				Assert.IsNotNull(transportType);
			}

			{
				var endpoint = _api.Endpoints.Query().SingleOrDefault(x => x.Name == "Video Source 1");
				Assert.IsNotNull(endpoint);
				Assert.AreEqual("Video Source 1", endpoint.Name);
			}

			{
				var endpoint = _api.Endpoints.Query().SingleOrDefault(x => x.ID == Guid.NewGuid());
				Assert.IsNull(endpoint);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_Any()
		{
			{
				var result = _api.TransportTypes.Query().Any();
				Assert.IsTrue(result);
			}

			{
				var result = _api.Endpoints.Query().Any(x => x.Name == "Video Source 1");
				Assert.IsTrue(result);
			}

			{
				var result = _api.Endpoints.Query().Any(x => x.ID == Guid.NewGuid());
				Assert.IsFalse(result);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_All()
		{
			{
				var result = _api.TransportTypes.Query().All(x => x.Name == "IP");
				Assert.IsTrue(result);
			}

			{
				var result = _api.Endpoints.Query().All(x => x.Name == "Video Source 1");
				Assert.IsFalse(result);
			}

			{
				var result = _api.Endpoints.Query()
					.All(x => x.Role == Role.Source || x.Role == Role.Destination);
				Assert.IsTrue(result);
			}

			{
				var result = _api.Endpoints.Query()
					.Where(x => x.Role == Role.Source)
					.All(x => x.Name.Contains("Source"));
				Assert.IsTrue(result);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_Count()
		{
			{
				var endpointCount = _api.Endpoints.Query().Count();
				Assert.AreEqual(40, endpointCount);
			}

			{
				var endpointCount = _api.Endpoints.Query().Count(x => x.Role == Role.Source);
				Assert.AreEqual(20, endpointCount);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_OrderBy()
		{
			{
				var endpoints = _api.Endpoints.Query().OrderBy(x => x.Name).ToList();
				CollectionAssert.AreEqual(
					_api.Endpoints.ReadAll().OrderBy(x => x.Name).ToList(),
					endpoints);
			}

			{
				var endpoints = _api.Endpoints.Query().OrderByDescending(x => x.Name).ToList();
				CollectionAssert.AreEqual(
					_api.Endpoints.ReadAll().OrderByDescending(x => x.Name).ToList(),
					endpoints);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_OrderByThenBy()
		{
			{
				var endpoints = _api.Endpoints.Query().OrderBy(x => x.Role).ThenBy(x => x.Name).ToList();
				CollectionAssert.AreEqual(
					_api.Endpoints.ReadAll().OrderBy(x => x.Role).ThenBy(x => x.Name).ToList(),
					endpoints);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_Take()
		{
			{
				var endpoints = _api.Endpoints.Query().Take(3).ToList();
				Assert.AreEqual(3, endpoints.Count);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_WhereCount()
		{
			{
				var endpointCount = _api.Endpoints.Query().Where(x => x.Role == Role.Source).Count();
				Assert.AreEqual(20, endpointCount);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_WhereWhere()
		{
			{
				var endpoints = _api.Endpoints.Query()
					.Where(x => x.Role == Role.Source)
					.Where(x => x.Name == "Video Source 1")
					.ToList();

				Assert.AreEqual(1, endpoints.Count);
				CollectionAssert.AreEquivalent(endpoints.Select(x => x.Name).ToList(), new[] { "Video Source 1" });
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_WhereTake()
		{
			{
				var endpoints = _api.Endpoints.Query()
					.Where(x => x.Role == Role.Source)
					.Take(3)
					.ToList();

				Assert.AreEqual(3, endpoints.Count);
				Assert.IsTrue(endpoints.All(x => x.Role == Role.Source));
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Query_WhereAny()
		{
			{
				var result = _api.Endpoints.Query()
					.Where(x => x.Role == Role.Source)
					.Any();

				Assert.IsTrue(result);
			}

			{
				var result = _api.Endpoints.Query()
					.Where(x => x.Role == Role.Source)
					.Any(x => x.Name == "Video Source 1");

				Assert.IsTrue(result);
			}

			{
				var result = _api.Endpoints.Query()
					.Where(x => x.Name == Guid.NewGuid().ToString())
					.Any();

				Assert.IsFalse(result);
			}
		}
	}
}
