namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API.TransportTypes;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_TransportTypes
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_TransportTypes_CreatePredefinedTransportTypes()
		{
			var api = new MediaOpsLiveApiMock();

			api.TransportTypes.CreatePredefinedTransportTypes();

			var all = api.TransportTypes.ReadAll().ToList();
			CollectionAssert.IsSubsetOf(PredefinedTransportTypes.All, all);
		}
	}
}
