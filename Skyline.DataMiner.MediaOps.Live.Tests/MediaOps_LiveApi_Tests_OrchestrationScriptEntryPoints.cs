namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Moq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	using OrchestrationScriptInput = Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects.OrchestrationScriptInput;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationScriptEntryPoints
	{
		[TestMethod]
		public void OrchestrationScript_ExecuteOrchestration_CallsOrchestrate()
		{
			var script = new LegacyScript();

			script.ExecuteOrchestration(null, false);

			Assert.IsTrue(script.WasOrchestrated);
		}

		[TestMethod]
		public void OrchestrationScript_EvaluateInputs_ReturnsNoInputDefinition()
		{
			var script = new LegacyScript();

			Assert.IsNull(script.EvaluateInputs(null, OrchestrationInputValues.Empty));
		}

		[TestMethod]
		public void DynamicOrchestrationScript_ExecuteOrchestration_PassesTheInputValues()
		{
			var script = new DynamicScript();
			var inputs = new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue> { ["General/Endpoint"] = "ENC-A" });
			script.EvaluateInputs(Mock.Of<IEngine>(), inputs);

			script.ExecuteOrchestration(null, false);

			Assert.AreEqual("ENC-A", script.Endpoint);
		}

		[TestMethod]
		public void DynamicOrchestrationScript_ExecuteOrchestration_RejectsAMissingRequiredInput()
		{
			var script = new DynamicScript();
			script.EvaluateInputs(Mock.Of<IEngine>(), OrchestrationInputValues.Empty);

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => script.ExecuteOrchestration(null, false));

			StringAssert.Contains(exception.Message, "'Endpoint' requires a value.");
			Assert.IsNull(script.Endpoint);
		}

		[TestMethod]
		public void DynamicOrchestrationScript_ExecuteOrchestration_RejectsAValueTheScriptReportsAsInvalid()
		{
			var script = new DynamicScript();
			var inputs = new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue> { ["General/Endpoint"] = "BLOCKED" });
			script.EvaluateInputs(Mock.Of<IEngine>(), inputs);

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => script.ExecuteOrchestration(null, false));

			StringAssert.Contains(exception.Message, "BLOCKED cannot be used.");
		}

		[TestMethod]
		public void DynamicOrchestrationScript_EvaluateInputs_PassesTheEngineToGetInputs()
		{
			var script = new DynamicScript();
			var engine = Mock.Of<IEngine>();

			script.EvaluateInputs(engine, OrchestrationInputValues.Empty);

			Assert.AreSame(engine, script.ReceivedEngine);
		}

		[TestMethod]
		public void DynamicOrchestrationScript_GetProfileParameters_ReturnsNone()
		{
			var script = new DynamicScript();

			Assert.IsEmpty(script.GetProfileParameters());
		}

		[TestMethod]
		public void DynamicOrchestrationScript_OnRequestScriptInfoRequest_ProvidesTheMetadataToGetInputs()
		{
			var script = new DynamicScript();
			var input = new OrchestrationScriptInput();
			input.Metadata["Region"] = "EU";

			var data = new Dictionary<string, string>
			{
				[OrchestrationScriptConstants.OrchestrationScriptActionRequestScriptInfoKey] = nameof(OrchestrationScriptAction.OrchestrationScriptInfo),
				[OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey] = JsonConvert.SerializeObject(input),
			};

			var output = script.OnRequestScriptInfoRequest(Mock.Of<IEngine>(), new RequestScriptInfoInput { Data = data });

			Assert.IsFalse(output.Data.ContainsKey(OrchestrationScriptConstants.ScriptOutputError));
			Assert.AreEqual("EU", script.ReceivedRegion);
		}

		private sealed class LegacyScript : OrchestrationScript
		{
			public bool WasOrchestrated { get; private set; }

			public override void Orchestrate(IEngine engine)
			{
				WasOrchestrated = true;
			}

			public override IEnumerable<IOrchestrationParameters> GetParameters()
			{
				return new IOrchestrationParameters[0];
			}
		}

		private sealed class DynamicScript : DynamicOrchestrationScript
		{
			public string Endpoint { get; private set; }

			public IEngine ReceivedEngine { get; private set; }

			public string ReceivedRegion { get; private set; }

			public override void Orchestrate(IEngine engine, OrchestrationInputValues inputs)
			{
				Endpoint = inputs.GetString("General/Endpoint");
			}

			public override OrchestrationInputDefinition GetInputs(IEngine engine, OrchestrationInputValues providedValues)
			{
				ReceivedEngine = engine;

				if (TryGetMetadataValue("Region", out var region))
				{
					ReceivedRegion = region;
				}

				return new OrchestrationInputBuilder()
					.AddGroup("General", general => general.AddText("Endpoint", field =>
					{
						field.IsRequired = true;

						if (providedValues.HasValue("General/Endpoint", "BLOCKED"))
						{
							field.IsValid = false;
							field.ValidationMessage = "BLOCKED cannot be used.";
						}
					}))
					.Build();
			}
		}
	}
}
