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
	}
}
