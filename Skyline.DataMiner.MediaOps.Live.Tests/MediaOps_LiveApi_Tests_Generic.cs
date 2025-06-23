namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.Tests.Mocking;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Generic
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_IsInstalled()
		{
			var api = new MediaOpsLiveApiMock(installDomModules: false);

			Assert.IsFalse(api.IsInstalled());

			api = new MediaOpsLiveApiMock(installDomModules: true);

			Assert.IsTrue(api.IsInstalled());
		}
	}
}
