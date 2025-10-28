namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using FluentAssertions;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.TransportTypes;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Endpoints
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_Endpoints_GetByRoleElementAndIdentifier()
		{
			var api = new MediaOpsLiveApiMock();

			var endpoint = api.Endpoints.GetByRoleElementAndIdentifier(
				EndpointRole.Destination,
				new DmsElementId(123, 1),
				"Audio-1");

			endpoint.Should().NotBeNull();
			endpoint.Name.Should().Be("Audio Destination 1");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Endpoints_GetByRoleElementAndIdentifier_Cache()
		{
			var api = new MediaOpsLiveApiMock();

			var cache = new EndpointsCache();
			cache.LoadInitialData(api);

			var endpoints = cache.GetEndpointsWithElementAndIdentifier(new DmsElementId(123, 1), "Audio-1")
				.Where(e => e.Role == EndpointRole.Destination)
				.ToList();

			endpoints.Should().HaveCount(1);
			endpoints[0].Name.Should().Be("Audio Destination 1");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Endpoints_GetByTransportMetadata()
		{
			var api = new MediaOpsLiveApiMock();

			// Test 1: Find endpoint by multicast IP
			{
				var endpoints = api.Endpoints.GetByTransportMetadata(TsoipTransportType.FieldNames.MulticastIp, "239.1.1.1").ToList();

				endpoints.Should().HaveCount(1);
				endpoints[0].Name.Should().Be("Video Source 1");
			}

			// Test 2: Find endpoints by source IP
			{
				var endpoints = api.Endpoints.GetByTransportMetadata(TsoipTransportType.FieldNames.SourceIp, "10.0.0.1").ToList();

				endpoints.Should().HaveCountGreaterThan(1);
				endpoints.Should().OnlyContain(
					endpoint => endpoint.HasTransportMetadata(TsoipTransportType.FieldNames.SourceIp, "10.0.0.1"));
			}

			// Test 3: Find by source IP, multicast IP and port
			{
				var endpoints = api.Endpoints.GetByTransportMetadata(
					(TsoipTransportType.FieldNames.SourceIp, "10.0.0.1"),
					(TsoipTransportType.FieldNames.MulticastIp, "239.1.1.1"),
					(TsoipTransportType.FieldNames.MulticastPort, "5000"))
					.ToList();

				endpoints.Should().HaveCount(1);
				endpoints[0].Name.Should().Be("Video Source 1");
			}

			// Test 4: Test the post-filtering (DOM doesn't check if the field name and value are in the same section)
			{
				var endpoints = api.Endpoints.GetByTransportMetadata(TsoipTransportType.FieldNames.MulticastIp, "10.0.0.1").ToList();

				endpoints.Should().BeEmpty();
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Endpoints_GetByTransportMetadata_Cache()
		{
			var api = new MediaOpsLiveApiMock();

			var cache = new EndpointsCache();
			cache.LoadInitialData(api);

			// Test 1: Find endpoint by multicast IP
			{
				var endpoints = cache.GetEndpointsWithTransportMetadata(TsoipTransportType.FieldNames.MulticastIp, "239.1.1.1").ToList();

				endpoints.Should().HaveCount(1);
				endpoints[0].Name.Should().Be("Video Source 1");
			}

			// Test 2: Find endpoints by source IP
			{
				var endpoints = cache.GetEndpointsWithTransportMetadata(TsoipTransportType.FieldNames.SourceIp, "10.0.0.1").ToList();

				endpoints.Should().HaveCountGreaterThan(1);
				endpoints.Should().OnlyContain(
					endpoint => endpoint.HasTransportMetadata(TsoipTransportType.FieldNames.SourceIp, "10.0.0.1"));
			}

			// Test 3: Find by source IP, multicast IP and port
			{
				var endpoints = cache.GetEndpointsWithTransportMetadata(
					(TsoipTransportType.FieldNames.SourceIp, "10.0.0.1"),
					(TsoipTransportType.FieldNames.MulticastIp, "239.1.1.1"),
					(TsoipTransportType.FieldNames.MulticastPort, "5000"))
					.ToList();

				endpoints.Should().HaveCount(1);
				endpoints[0].Name.Should().Be("Video Source 1");
			}

			// Test 4: Test the post-filtering (DOM doesn't check if the field name and value are in the same section)
			{
				var endpoints = cache.GetEndpointsWithTransportMetadata(TsoipTransportType.FieldNames.MulticastIp, "10.0.0.1").ToList();

				endpoints.Should().BeEmpty();
			}
		}
	}
}
