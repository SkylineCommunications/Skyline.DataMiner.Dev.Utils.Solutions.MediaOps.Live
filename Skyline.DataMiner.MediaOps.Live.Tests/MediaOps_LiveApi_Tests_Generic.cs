namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.DOM.Definitions;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Generic
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_IsInstalled()
		{
			var api = new MediaOpsLiveApiMock();

			Assert.IsFalse(api.IsInstalled());

			var slcConnectivityManagementDomModule = new SlcConnectivityManagementDomModule();
			DomModuleInstaller.Install(api.MessageHandler.HandleMessages, slcConnectivityManagementDomModule, x => { });

			Assert.IsTrue(api.IsInstalled());
		}
	}
}
