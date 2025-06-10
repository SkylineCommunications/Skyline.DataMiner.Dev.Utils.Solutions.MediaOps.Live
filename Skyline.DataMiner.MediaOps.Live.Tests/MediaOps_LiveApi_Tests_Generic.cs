namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Generic
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_IsInstalled()
		{
			var api = new MediaOpsLiveApiMock(installDomModules: false);

			Assert.IsFalse(api.IsInstalled());

			api = new MediaOpsLiveApiMock(installDomModules: true);

			var slcOrchestrationDomModule = new SlcOrchestrationDomModule();
			DomModuleInstaller.Install(api.MessageHandler.HandleMessages, slcOrchestrationDomModule, x => { });

			Assert.IsTrue(api.IsInstalled());
		}
	}
}
