namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Newtonsoft.Json;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationScriptInfoHelper
	{
		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_GetOrchestrationScriptInfo()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			OrchestrationScriptInputInfo info = api.Orchestration.Scripts.GetOrchestrationScriptInputInfo("OrchestrationScript");

			Assert.AreEqual("OrchestrationScript", info.ScriptName);
			Assert.HasCount(5, info.Parameters);
			Assert.HasCount(4, info.Parameters.Where(param => param.IsFromProfile));
			Assert.HasCount(1, info.Elements);
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_GetOrchestrationScripts()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var info = api.Orchestration.Scripts.GetOrchestrationScripts();

			Assert.HasCount(3, info);
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_GetValidElementsForScriptDummy()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();
			OrchestrationScriptInputInfo info = api.Orchestration.Scripts.GetOrchestrationScriptInputInfo("OrchestrationScript");

			var elements = info.Elements.First().GetApplicableElements(api);
			Console.WriteLine(JsonConvert.SerializeObject(elements.Select(e => e.Name), Formatting.Indented));
			Assert.HasCount(2, elements);
		}

		[TestMethod]
		public void MediaOps_Live_Api_Tests_OrchestrationJob_GetValidInstancesForDefinition()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			OrchestrationScriptInputInfo info = api.Orchestration.Scripts.GetOrchestrationScriptInputInfo("OrchestrationScript");

			var instances = info.GetApplicableProfileInstances(new ProfileHelper(api.Connection.HandleMessages));
			Assert.HasCount(1, instances);
		}
	}
}
