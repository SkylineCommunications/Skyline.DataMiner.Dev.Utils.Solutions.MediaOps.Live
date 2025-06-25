namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.Tests.Mocking;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Generic
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_IsInstalled()
		{
			var simulation = new MediaOpsLiveSimulation(installDomModules: false);

			Assert.IsFalse(simulation.Api.IsInstalled());

			simulation = new MediaOpsLiveSimulation(installDomModules: true);

			Assert.IsTrue(simulation.Api.IsInstalled());
		}
	}
}
