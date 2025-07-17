namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.GQI.Metrics;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

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

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConstructorDoesNotExecuteRequest()
		{
			var simulation = new MediaOpsLiveSimulation();
			var connection = simulation.Dms.CreateConnection();
			var interceptedConnection = new ConnectionInterceptor(connection);

			var connectionMetrics = new ConnectionMetrics(interceptedConnection);
			new MediaOpsLiveApi(interceptedConnection);

			// MediaOpsLiveApi constructor should not execute any requests
			Assert.AreEqual(0UL, connectionMetrics.NumberOfRequests);
			Assert.AreEqual(0UL, connectionMetrics.NumberOfDomRequests);
			Assert.AreEqual(0UL, connectionMetrics.NumberOfDomInstancesRetrieved);
		}
	}
}
