namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Validation
	{
		private static readonly MediaOpsLiveApi _api = new MediaOpsLiveApiMock();

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_TransportTypes_CheckDuplicates()
		{
			// doesn't throw exception
			var tt = new TransportType { Name = "IP2" };
			_api.TransportTypes.Create(tt);

			tt.Name = "IP3";
			_api.TransportTypes.Update(tt);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { _api.TransportTypes.Create(new TransportType { Name = "IP3" }); });
			Assert.AreEqual("Transport type with same name already exists.", ex.Message);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Validation_Levels_CheckDuplicates()
		{
			var transportType = _api.TransportTypes.Query().First(x => x.Name == "IP");

			// doesn't throw exception
			var l = new Level { Name = "L1", Number = 101, TransportType = transportType };
			_api.Levels.Create(l);

			l.Name = "L2";
			l.Number = 102;
			_api.Levels.Update(l);

			// create item with same name
			var ex = Assert.Throws<Exception>(
				() => { _api.Levels.Create(new Level { Name = "L2", Number = 102, TransportType = transportType }); });
			Assert.AreEqual("Level with same name or number already exists.", ex.Message);
		}
	}
}
