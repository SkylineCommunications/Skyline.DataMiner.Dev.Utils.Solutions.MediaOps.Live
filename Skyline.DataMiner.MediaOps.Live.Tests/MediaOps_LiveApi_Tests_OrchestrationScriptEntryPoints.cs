namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationScriptEntryPoints
	{
		[TestMethod]
		public void OrchestrationScript_Orchestrate_ForwardsToTheLegacyOverloadOfExistingScripts()
		{
			var script = new LegacyScript();

			script.Orchestrate(null, OrchestrationInputValues.Empty);

			Assert.IsTrue(script.WasOrchestrated);
		}

		[TestMethod]
		public void OrchestrationScript_Orchestrate_PassesTheInputValuesToScriptsThatWantThem()
		{
			var script = new DynamicScript();
			var inputs = new OrchestrationInputValues(new Dictionary<string, object> { ["General/Endpoint"] = "ENC-A" });

			script.Orchestrate(null, inputs);

			Assert.AreEqual("ENC-A", script.Endpoint);
		}

		[TestMethod]
		public void OrchestrationScript_Orchestrate_ThrowsWhenNeitherOverloadIsImplemented()
		{
			var script = new ScriptWithoutOrchestrate();

			var exception = Assert.ThrowsExactly<NotImplementedException>(() => script.Orchestrate(null, OrchestrationInputValues.Empty));

			Assert.Contains(nameof(ScriptWithoutOrchestrate), exception.Message);
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

		private sealed class DynamicScript : OrchestrationScript
		{
			public string Endpoint { get; private set; }

			public override void Orchestrate(IEngine engine, OrchestrationInputValues inputs)
			{
				Endpoint = inputs.GetString("General/Endpoint");
			}

			public override IEnumerable<IOrchestrationParameters> GetParameters()
			{
				return new IOrchestrationParameters[0];
			}

			public override OrchestrationInputDefinition GetParameters(OrchestrationInputValues providedValues)
			{
				return new OrchestrationInputBuilder()
					.AddGroup("General", general => general.AddText("Endpoint"))
					.Build();
			}
		}

		private sealed class ScriptWithoutOrchestrate : OrchestrationScript
		{
			public override IEnumerable<IOrchestrationParameters> GetParameters()
			{
				return new IOrchestrationParameters[0];
			}
		}
	}
}
