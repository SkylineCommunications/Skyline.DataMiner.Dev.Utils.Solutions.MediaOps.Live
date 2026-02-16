namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using FluentAssertions;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.GQI.Metrics;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting.Simulation;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Generic
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConstructorDoesNotExecuteRequest()
		{
			var simulation = new MediaOpsLiveSimulation();
			var connection = simulation.Dms.CreateConnection();
			var interceptedConnection = new ConnectionInterceptor(connection);

			using (var connectionMetrics = new ConnectionMetrics(interceptedConnection))
			{
				new MediaOpsLiveApi(interceptedConnection);

				// MediaOpsLiveApi constructor should not execute any requests
				Assert.AreEqual(0UL, connectionMetrics.NumberOfRequests);
				Assert.AreEqual(0UL, connectionMetrics.NumberOfDomRequests);
				Assert.AreEqual(0UL, connectionMetrics.NumberOfDomInstancesRetrieved);
			}
		}
	}
}
