namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using FluentAssertions;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.MediaOps.Live.GQI.Metrics;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting.Simulation;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Generic
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_IsInstalled()
		{
			var dms = new SimulatedDms();
			var connection = dms.CreateConnection();
			var api = connection.GetMediaOpsLiveApi();

			Assert.IsFalse(api.IsInstalled());

			dms.AddApplicationPackage(Constants.AppPackageName, MediaOpsLiveApi.GetVersion());

			api.InstalledAppPackages.Refresh();
			Assert.IsTrue(api.IsInstalled());
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Version()
		{
			var version = MediaOpsLiveApi.GetVersion();

			version.Should().NotBeNullOrEmpty();
		}

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
