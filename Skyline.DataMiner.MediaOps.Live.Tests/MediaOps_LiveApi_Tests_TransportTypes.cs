namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API.Repositories;
	using Skyline.DataMiner.MediaOps.Live.Tests.Mocking;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_TransportTypes
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_TransportTypes_CreatePredefinedTransportTypes()
		{
			var api = new MediaOpsLiveApiMock();

			api.TransportTypes.CreatePredefinedTransportTypes();

			CollectionAssert.IsSubsetOf(
				PredefinedTransportTypes.All,
				api.TransportTypes.ReadAll().ToList());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_TransportTypes_ModifyingPredefinedTransportTypeThrowsException()
		{
			var api = new MediaOpsLiveApiMock();
			api.TransportTypes.CreatePredefinedTransportTypes();

			var tsoip = api.TransportTypes.Read("TSoIP");
			tsoip.Name = "TSoIP 2";

			var ex = Assert.Throws<Exception>(
				() => { api.TransportTypes.Update(tsoip); });

			Assert.AreEqual("Modifying a predefined transport type is not allowed.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_TransportTypes_DeletingPredefinedTransportTypeThrowsException()
		{
			var api = new MediaOpsLiveApiMock();
			api.TransportTypes.CreatePredefinedTransportTypes();

			var tsoip = api.TransportTypes.Read("TSoIP");

			var ex = Assert.Throws<Exception>(
				() => { api.TransportTypes.Delete(tsoip); });

			Assert.AreEqual("Modifying a predefined transport type is not allowed.", ex.Message);
		}
	}
}
