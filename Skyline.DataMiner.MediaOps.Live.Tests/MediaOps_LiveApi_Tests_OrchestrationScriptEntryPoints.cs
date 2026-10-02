namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Moq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	using OrchestrationScriptInfo = Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects.OrchestrationScriptInfo;
	using OrchestrationScriptInput = Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects.OrchestrationScriptInput;
	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;

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
		public void OrchestrationScript_OnRequestScriptInfoRequest_ReturnsTheProfileParametersAndNoInputDefinition()
		{
			var script = new ProfileParameterScript();

			var output = script.OnRequestScriptInfoRequest(Mock.Of<IEngine>(), CreateRequest(OrchestrationScriptAction.OrchestrationScriptInfo, new OrchestrationScriptInput()));

			Assert.IsFalse(output.Data.ContainsKey(OrchestrationScriptConstants.ScriptOutputError));
			var info = JsonConvert.DeserializeObject<OrchestrationScriptInfo>(output.Data[OrchestrationScriptConstants.OrchestrationScriptInfoRequestScriptInfoKey]);
			Assert.HasCount(2, info.ProfileParametersIdByName);
			Assert.AreEqual(ProfileParameterScript.EndpointId, info.ProfileParametersIdByName["Endpoint"]);
			Assert.AreEqual(ProfileParameterScript.BitrateId, info.ProfileParametersIdByName["Bitrate"]);
			Assert.IsNull(info.InputDefinition);
		}

		[TestMethod]
		public void OrchestrationScript_OnRequestScriptInfoRequest_PerformOrchestrationProvidesTheProfileParameterValues()
		{
			var script = new ProfileParameterScript();
			var input = new OrchestrationScriptInput
			{
				ProfileParameterValues = new Dictionary<string, object>
				{
					["Endpoint"] = "ENC-A",
					[ProfileParameterScript.BitrateId.ToString()] = 7.5,
				},
			};

			var output = script.OnRequestScriptInfoRequest(Mock.Of<IEngine>(), CreateRequest(OrchestrationScriptAction.PerformOrchestration, input));

			Assert.IsFalse(output.Data.ContainsKey(OrchestrationScriptConstants.ScriptOutputError), output.Data.TryGetValue(OrchestrationScriptConstants.ScriptOutputError, out var error) ? error : null);
			Assert.AreEqual("ENC-A", script.Endpoint);
			Assert.AreEqual(7.5, script.Bitrate);
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

		private static RequestScriptInfoInput CreateRequest(OrchestrationScriptAction action, OrchestrationScriptInput input)
		{
			return new RequestScriptInfoInput
			{
				Data = new Dictionary<string, string>
				{
					[OrchestrationScriptConstants.OrchestrationScriptActionRequestScriptInfoKey] = action.ToString(),
					[OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey] = JsonConvert.SerializeObject(input),
				},
			};
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

		private sealed class ProfileParameterScript : OrchestrationScript
		{
			public static readonly Guid EndpointId = Guid.NewGuid();

			public static readonly Guid BitrateId = Guid.NewGuid();

			public string Endpoint { get; private set; }

			public object Bitrate { get; private set; }

			public override void Orchestrate(IEngine engine)
			{
				Endpoint = (string)GetParameterValue("Endpoint");
				Bitrate = GetParameterValue("Bitrate");
			}

			public override IEnumerable<IOrchestrationParameters> GetParameters()
			{
				return new IOrchestrationParameters[]
				{
					new FixedParameter(new Parameter(EndpointId)
					{
						Name = "Endpoint",
						Type = Parameter.ParameterType.Text,
						InterpreteType = new InterpreteType { Type = InterpreteType.TypeEnum.String },
					}),
					new FixedParameter(new Parameter(BitrateId)
					{
						Name = "Bitrate",
						Type = Parameter.ParameterType.Number,
						InterpreteType = new InterpreteType { Type = InterpreteType.TypeEnum.Double },
						RangeMin = 0,
						RangeMax = 100,
						Decimals = 1,
					}),
				};
			}
		}

		// Stands in for OrchestrationProfileParameter, which reads the parameter from SLNet.
		private sealed class FixedParameter : IOrchestrationParameters
		{
			private readonly Parameter _parameter;

			public FixedParameter(Parameter parameter)
			{
				_parameter = parameter;
			}

			public IDictionary<string, Guid> GetParameterInformation(IEngine engine)
			{
				return new Dictionary<string, Guid> { [_parameter.Name] = _parameter.ID };
			}

			public IDictionary<string, Parameter> GetParameterReferences(IEngine engine)
			{
				return new Dictionary<string, Parameter> { [_parameter.Name] = _parameter };
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
