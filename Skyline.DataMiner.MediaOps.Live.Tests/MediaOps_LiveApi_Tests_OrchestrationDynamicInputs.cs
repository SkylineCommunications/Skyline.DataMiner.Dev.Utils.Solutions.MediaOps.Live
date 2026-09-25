namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Newtonsoft.Json;

	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
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

			var providedValues = new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
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

			var providedValues = new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
			{
				[NumberOfDestinationsPath] = 2,
				["Destinations/Destination 1/Endpoint"] = "ENC-A",
			});

			var info = simulation.Api.Orchestration.Scripts.GetOrchestrationScriptInputInfo(ScriptName, providedValues);

			Assert.IsTrue(info.InputDefinition.TryGetField("Destinations/Destination 1/Endpoint", out var endpoint));
			Assert.AreEqual<OrchestrationInputValue>("ENC-A", endpoint.Value);
		}

		[TestMethod]
		public void OrchestrationScriptInfoHelper_GetOrchestrationScriptInputInfo_LegacyScriptHasNoDynamicInputs()
		{
			MediaOpsLiveApi api = new MediaOpsLiveApiMock();

			var info = api.Orchestration.Scripts.GetOrchestrationScriptInputInfo("OrchestrationScript");

			Assert.IsFalse(info.HasDynamicInputs);
			Assert.IsNull(info.InputDefinition);
		}

		[TestMethod]
		public void OrchestrationHelper_SaveOrchestrationJobConfiguration_RejectsAConfirmedEventWithAMissingRequiredInput()
		{
			var simulation = CreateSimulationWithDynamicScript();

			var job = CreateJobWithDynamicScript(simulation.Api, new Dictionary<string, OrchestrationInputValue>
			{
				[NumberOfDestinationsPath] = 2,
				["Destinations/Destination 1/Endpoint"] = "ENC-A",
			});

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => simulation.Api.Orchestration.SaveOrchestrationJobConfiguration(job));

			Assert.Contains("Endpoint", exception.Message);
		}

		[TestMethod]
		public void OrchestrationHelper_SaveOrchestrationJobConfiguration_StoresTheDynamicInputValuesAsProfileValues()
		{
			var simulation = CreateSimulationWithDynamicScript();

			var job = CreateJobWithDynamicScript(simulation.Api, new Dictionary<string, OrchestrationInputValue>
			{
				[NumberOfDestinationsPath] = 2,
				["Destinations/Destination 1/Endpoint"] = "ENC-A",
				["Destinations/Destination 2/Endpoint"] = "ENC-B",
			});

			simulation.Api.Orchestration.SaveOrchestrationJobConfiguration(job);

			var stored = simulation.Api.Orchestration.GetOrchestrationJobConfiguration(job.JobId).OrchestrationEvents.Single().Profile.GetInputValues();

			Assert.HasCount(3, stored.ToDictionary());
			Assert.AreEqual(2, stored.GetInt32(NumberOfDestinationsPath));
			Assert.IsTrue(stored.TryGetValue(NumberOfDestinationsPath, out var count));
			Assert.IsTrue(count.IsNumber);
			Assert.AreEqual("ENC-B", stored.GetString("Destinations/Destination 2/Endpoint"));
		}

		[TestMethod]
		public void OrchestrationEventExecutionHelper_ExecuteOrchestrationScript_PassesTheDynamicInputValuesToTheScript()
		{
			var simulation = CreateSimulationWithDynamicScript();

			var profile = new OrchestrationProfile();
			profile.SetInputValues(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
			{
				[NumberOfDestinationsPath] = 1,
				["Destinations/Destination 1/Endpoint"] = "ENC-A",
			}));

			var result = OrchestrationEventExecutionHelper.ExecuteOrchestrationScript(simulation.Api.Connection, ScriptName, new List<OrchestrationScriptArgument>(), profile);

			Assert.IsFalse(result.HadError);

			var metaData = simulation.Dms.ExecutedScripts.Single(x => x.ScriptName == ScriptName).CustomEntryPoint.Parameters.OfType<RequestScriptInfoInput>().Single().Data;
			var input = JsonConvert.DeserializeObject<OrchestrationScriptInput>(metaData[OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey]);

			Assert.AreEqual<OrchestrationInputValue>("ENC-A", input.InputValues["Destinations/Destination 1/Endpoint"]);
			Assert.AreEqual<OrchestrationInputValue>(1, input.InputValues[NumberOfDestinationsPath]);
		}

		private static OrchestrationJobConfiguration CreateJobWithDynamicScript(MediaOpsLiveApi api, Dictionary<string, OrchestrationInputValue> inputValues)
		{
			var eventConfig = new OrchestrationEventConfiguration
			{
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventState = EventState.Confirmed,
				EventType = EventType.Other,
				Name = "Dynamic Event",
				GlobalOrchestrationScript = ScriptName,
			};

			eventConfig.Profile = new OrchestrationProfile();
			eventConfig.Profile.SetInputValues(new OrchestrationInputValues(inputValues));

			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(eventConfig);

			return job;
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
