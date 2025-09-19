namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using FluentAssertions;

	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Api
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_Api_Version()
		{
			var api = new MediaOpsLiveApiMock();
			var version = api.GetVersion();

			version.Should().NotBeNullOrEmpty();
		}
	}
}
