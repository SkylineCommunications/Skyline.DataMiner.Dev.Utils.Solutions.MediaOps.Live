namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using System.Diagnostics;

	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_ConnectionMonitor
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectionMonitor_WaitUntilConnected()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");

			using (var connectivity = new ConnectivityInfoProvider(api, subscribe: true))
			{
				var monitor = new ConnectionMonitor(connectivity);

				var waitTask = Task.Run(() =>
					monitor.WaitUntilConnected(videoSource1, videoDestination1, TimeSpan.FromMinutes(1)));

				Thread.Sleep(200);
				Assert.IsFalse(waitTask.IsCompleted);

				simulation.CreateTestConnection(videoSource1, videoDestination1);

				waitTask.Wait(TimeSpan.FromSeconds(5));

				Assert.IsTrue(waitTask.IsCompleted);
				Assert.IsTrue(waitTask.Result);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectionMonitor_WaitUntilConnected_Timeout()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");

			var timeout = TimeSpan.FromMilliseconds(200);

			using (var connectivity = new ConnectivityInfoProvider(api, subscribe: true))
			{
				var monitor = new ConnectionMonitor(connectivity);

				var stopwatch = Stopwatch.StartNew();
				var connected = monitor.WaitUntilConnected(videoSource1, videoDestination1, timeout);
				stopwatch.Stop();

				Assert.IsFalse(connected);
				Assert.IsTrue(stopwatch.Elapsed >= timeout, "Elapsed time should be more than the timeout.");
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectionMonitor_WaitUntilDisconnected()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");

			simulation.CreateTestConnection(videoSource1, videoDestination1);

			using (var connectivity = new ConnectivityInfoProvider(api, subscribe: true))
			{
				var monitor = new ConnectionMonitor(connectivity);

				var waitTask = Task.Run(() =>
					monitor.WaitUntilDisconnected(videoDestination1, TimeSpan.FromMinutes(1)));

				Thread.Sleep(200);
				Assert.IsFalse(waitTask.IsCompleted);

				simulation.CreateTestConnection(null, videoDestination1);

				waitTask.Wait(TimeSpan.FromSeconds(5));

				Assert.IsTrue(waitTask.IsCompleted);
				Assert.IsTrue(waitTask.Result);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectionMonitor_WaitUntilDisconnected_Timeout()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");

			simulation.CreateTestConnection(videoSource1, videoDestination1);

			var timeout = TimeSpan.FromMilliseconds(200);

			using (var connectivity = new ConnectivityInfoProvider(api, subscribe: true))
			{
				var monitor = new ConnectionMonitor(connectivity);

				var stopwatch = Stopwatch.StartNew();
				var connected = monitor.WaitUntilDisconnected(videoDestination1, timeout);
				stopwatch.Stop();

				Assert.IsFalse(connected);
				Assert.IsTrue(stopwatch.Elapsed >= timeout, "Elapsed time should be more than the timeout.");
			}
		}
	}
}
