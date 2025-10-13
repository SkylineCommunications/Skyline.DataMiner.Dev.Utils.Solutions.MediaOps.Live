namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using FluentAssertions;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Endpoints
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_Endpoints_GetByRoleElementAndIdentifier()
		{
			var api = new MediaOpsLiveApiMock();

			var endpoint = api.Endpoints.GetByRoleElementAndIdentifier(
				Role.Destination,
				new DmsElementId(123, 1),
				"Audio-1");

			endpoint.Should().NotBeNull();
			endpoint.Name.Should().Be("Audio Destination 1");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Endpoints_GetByTransportMetadata()
		{
			var api = new MediaOpsLiveApiMock();

			// Test 1: Find endpoint by multicast IP
			{
				var endpoints = api.Endpoints.GetByTransportMetadata("Multicast IP", "239.1.1.1").ToList();

				endpoints.Should().HaveCount(1);
				endpoints[0].Name.Should().Be("Video Source 1");
			}

			// Test 2: Find endpoints by source IP
			{
				var endpoints = api.Endpoints.GetByTransportMetadata("Source IP", "10.0.0.1").ToList();

				endpoints.Should().HaveCountGreaterThan(1);
				endpoints.Should().OnlyContain(
					endpoint => endpoint.HasTransportMetadata("Source IP", "10.0.0.1"));
			}

			// Test 3: Find by source IP, multicast IP and port
			{
				var endpoints = api.Endpoints.GetByTransportMetadata(
					("Source IP", "10.0.0.1"),
					("Multicast IP", "239.1.1.1"),
					("Multicast Port", "5000"))
					.ToList();

				endpoints.Should().HaveCount(1);
				endpoints[0].Name.Should().Be("Video Source 1");
			}

			// Test 4: Test the post-filtering (DOM doesn't check if the field name and value are in the same section)
			{
				var endpoints = api.Endpoints.GetByTransportMetadata("Multicast IP", "10.0.0.1").ToList();

				endpoints.Should().BeEmpty();
			}
		}
	}
}
