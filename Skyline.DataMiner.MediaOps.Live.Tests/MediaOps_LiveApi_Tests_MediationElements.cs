namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using FluentAssertions;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_MediationElements
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_MediationElements_GetMediatedElements()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var mediatedElements = api.MediationElements
				.GetAllElements()
				.SelectMany(x => x.GetMediatedElements())
				.ToList();

			mediatedElements.Should().BeEquivalentTo(
			[
				new MediatedElementInfo(123, 1, "MediaOps Simulator 1")
				{
					IsEnabled = true,
					ConnectionHandlerScript = "Simulator_ConnectionHandler",
				},
				new MediatedElementInfo(123, 2, "MediaOps Simulator 2")
				{
					IsEnabled = true,
					ConnectionHandlerScript = "Simulator_ConnectionHandler",
				},
			]);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_MediationElements_GetConnectionHandlerScriptNames()
		{
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var scripts = api.MediationElements
				.GetAllElements()
				.SelectMany(x => x.GetConnectionHandlerScriptNames())
				.Distinct()
				.ToList();

			scripts.Should().BeEquivalentTo(["Simulator_ConnectionHandler"]);
		}
	}
}