namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_ConnectionWaiter
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectionWaiter_WaitUntilConnected_VirtualSignalGroup()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var audioSource1 = api.Endpoints.Read("Audio Source 1");
			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var audioDestination1 = api.Endpoints.Read("Audio Destination 1");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");

			var source1 = api.VirtualSignalGroups.Read("Source 1");
			var destination1 = api.VirtualSignalGroups.Read("Destination 1");

			using (var connectivity = new ConnectivityInfoProvider(api))
			{
				var waitTask = Task.Run(() =>
					ConnectionWaiter.WaitUntilConnected(connectivity, source1, destination1, TimeSpan.FromMinutes(1)));

				Thread.Sleep(200);
				Assert.IsFalse(waitTask.IsCompleted);

				simulation.CreateTestConnection(videoSource1, videoDestination1);
				simulation.CreateTestConnection(audioSource1, audioDestination1);

				waitTask.Wait(TimeSpan.FromSeconds(5));

				Assert.IsTrue(waitTask.IsCompleted);
				Assert.IsTrue(waitTask.Result);
			}
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_ConnectionWaiter_WaitUntilConnected_Endpoint()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var videoSource1 = api.Endpoints.Read("Video Source 1");
			var videoDestination1 = api.Endpoints.Read("Video Destination 1");

			using (var connectivity = new ConnectivityInfoProvider(api))
			{
				var waitTask = Task.Run(() =>
					ConnectionWaiter.WaitUntilConnected(connectivity, videoSource1, videoDestination1, TimeSpan.FromMinutes(1)));

				Thread.Sleep(200);
				Assert.IsFalse(waitTask.IsCompleted);

				simulation.CreateTestConnection(videoSource1, videoDestination1);

				waitTask.Wait(TimeSpan.FromSeconds(5));

				Assert.IsTrue(waitTask.IsCompleted);
				Assert.IsTrue(waitTask.Result);
			}
		}
	}
}
