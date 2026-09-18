namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationDynamicInputs
	{
		private const string ScriptName = "DynamicOrchestrationScript";
		private const string NumberOfDestinationsPath = "General/Number of destinations";

		[TestMethod]
		public void OrchestrationScriptInfoHelper_GetOrchestrationScriptInputInfo_ReturnsDynamicInputDefinition()
		{
			var simulation = CreateSimulationWithDynamicScript();

			var info = simulation.Api.Orchestration.Scripts.GetOrchestrationScriptInputInfo(ScriptName);

			Assert.IsTrue(info.HasDynamicInputs);
			Assert.IsTrue(info.InputDefinition.TryGetField(NumberOfDestinationsPath, out _));
			Assert.IsTrue(info.InputDefinition.TryGetGroup("Destinations", out var destinations));
			Assert.HasCount(1, destinations.Groups.ToList());
		}

		[TestMethod]
		public void OrchestrationScriptInfoHelper_GetOrchestrationScriptInputInfo_RepeatsGroupForProvidedCount()
		{
			var simulation = CreateSimulationWithDynamicScript();

			var providedValues = new OrchestrationInputValues(new Dictionary<string, object>
			{
				[NumberOfDestinationsPath] = 3,
			});

			var info = simulation.Api.Orchestration.Scripts.GetOrchestrationScriptInputInfo(ScriptName, providedValues);

			Assert.IsTrue(info.InputDefinition.TryGetGroup("Destinations", out var destinations));
			Assert.HasCount(3, destinations.Groups.ToList());
			Assert.IsTrue(info.InputDefinition.TryGetField("Destinations/Destination 3/Endpoint", out _));
		}

		[TestMethod]
		public void OrchestrationScriptInfoHelper_GetOrchestrationScriptInputInfo_KeepsProvidedValuesOfRemainingFields()
		{
			var simulation = CreateSimulationWithDynamicScript();

			var providedValues = new OrchestrationInputValues(new Dictionary<string, object>
			{
				[NumberOfDestinationsPath] = 2,
				["Destinations/Destination 1/Endpoint"] = "ENC-A",
			});

			var info = simulation.Api.Orchestration.Scripts.GetOrchestrationScriptInputInfo(ScriptName, providedValues);

			Assert.IsTrue(info.InputDefinition.TryGetField("Destinations/Destination 1/Endpoint", out var endpoint));
			Assert.AreEqual("ENC-A", endpoint.Value);
		}

		[TestMethod]
		public void OrchestrationScriptInfoHelper_GetOrchestrationScriptInputInfo_LegacyScriptHasNoDynamicInputs()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var info = api.Orchestration.Scripts.GetOrchestrationScriptInputInfo("OrchestrationScript");

			Assert.IsFalse(info.HasDynamicInputs);
			Assert.IsNull(info.InputDefinition);
		}

		private static MediaOpsLiveSimulation CreateSimulationWithDynamicScript()
		{
			var simulation = new MediaOpsLiveSimulation();

			simulation.Dms.AddScript(
				ScriptName,
				BuildScriptInfo,
				folder: "MediaOps/OrchestrationScripts");

			return simulation;
		}

		private static OrchestrationScriptInfo BuildScriptInfo(OrchestrationInputValues providedValues)
		{
			var builder = new OrchestrationInputBuilder();

			builder.AddGroup("General", general =>
				general.AddNumber("Number of destinations", field =>
				{
					field.Minimum = 1;
					field.Maximum = 8;
					field.DefaultValue = 1;
					field.TriggersReevaluation = true;
				}));

			var destinationCount = providedValues.GetInt32(NumberOfDestinationsPath, 1, 8);

			builder.AddGroup("Destinations", destinations =>
			{
				for (var index = 1; index <= destinationCount; index++)
				{
					destinations.AddGroup($"Destination {index}", destination =>
						destination.AddText("Endpoint", field => field.IsRequired = true));
				}
			});

			var definition = builder.Build();
			definition.ApplyValues(providedValues);

			return new OrchestrationScriptInfo { InputDefinition = definition };
		}
	}
}
