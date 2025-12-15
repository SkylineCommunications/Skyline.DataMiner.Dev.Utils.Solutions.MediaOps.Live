namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using FluentAssertions;

	using Skyline.DataMiner.Automation;

	[TestClass]
	public sealed class MediaOps_Live_Tests_Interactive
	{
		[TestMethod]
		public void MediaOps_Live_Tests_Interactive_Test()
		{
			var uib = new UIBuilder();

			uib.Should().NotBeNull();
		}
	}
}
