namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests.Automation.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Moq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects;

	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;

	[TestClass]
	public class OrchestrationScriptTests
	{
		private const string DefinitionName = "Transport";
		private const string InstanceName = "Preset";
		private const string CapabilityName = "Location";
		private const string CapacityName = "Bandwidth";

		private readonly Parameter _capabilityParameter = new Parameter(Guid.NewGuid())
		{
			Name = CapabilityName,
			Type = Parameter.ParameterType.Discrete,
			InterpreteType = new InterpreteType { Type = InterpreteType.TypeEnum.String },
			Discretes = new List<string> { "North", "South" },
			DiscreetDisplayValues = new List<string> { "North", "South" },
		};

		private readonly Parameter _capacityParameter = new Parameter(Guid.NewGuid())
		{
			Name = CapacityName,
			Type = Parameter.ParameterType.Number,
			InterpreteType = new InterpreteType { Type = InterpreteType.TypeEnum.Double },
		};

		[TestMethod]
		public void OrchestrationScript_PerformOrchestration_WithCapabilityAndCapacityInstance_UsesUsageValues()
		{
			// Arrange
			Mock<IEngine> engine = CreateEngine();
			var script = new TestOrchestrationScript();

			// Act
			RequestScriptInfoOutput output = script.OnRequestScriptInfoRequest(engine.Object, CreatePerformOrchestrationInput(InstanceName));

			// Assert
			Assert.IsFalse(output.Data.TryGetValue(OrchestrationScriptConstants.ScriptOutputError, out string error), error);
			Assert.AreEqual("North", script.Values[CapabilityName]);
			Assert.AreEqual(50.0, script.Values[CapacityName]);
		}

		[TestMethod]
		public void OrchestrationScript_PerformOrchestration_WithLinkedValuesAndCapabilityAndCapacityPresets_DoesNotFail()
		{
			// Arrange
			Mock<IEngine> engine = CreateEngine();
			var script = new TestOrchestrationScript();
			var values = new Dictionary<string, object>
			{
				{ _capabilityParameter.ID.ToString(), "South" },
				{ _capacityParameter.ID.ToString(), 20.0 },
			};

			// Act
			RequestScriptInfoOutput output = script.OnRequestScriptInfoRequest(engine.Object, CreatePerformOrchestrationInput(String.Empty, values));

			// Assert
			Assert.IsFalse(output.Data.TryGetValue(OrchestrationScriptConstants.ScriptOutputError, out string error), error);
			Assert.AreEqual("South", script.Values[CapabilityName]);
			Assert.AreEqual(20.0, script.Values[CapacityName]);
		}

		[TestMethod]
		public void OrchestrationScript_PerformOrchestration_WithInstanceWithoutValues_DoesNotFail()
		{
			// Arrange
			Mock<IEngine> engine = CreateEngine(instance => instance.Values = null);
			var script = new TestOrchestrationScript();

			// Act
			RequestScriptInfoOutput output = script.OnRequestScriptInfoRequest(engine.Object, CreatePerformOrchestrationInput(InstanceName));

			// Assert
			Assert.IsFalse(output.Data.TryGetValue(OrchestrationScriptConstants.ScriptOutputError, out string error), error);
			Assert.IsNull(script.Values[CapabilityName]);
			Assert.IsNull(script.Values[CapacityName]);
		}

		private static RequestScriptInfoInput CreatePerformOrchestrationInput(string profileInstance, Dictionary<string, object> values = null)
		{
			var input = new OrchestrationScriptInput(values ?? new Dictionary<string, object>(), profileInstance);

			return new RequestScriptInfoInput
			{
				Data = new Dictionary<string, string>
				{
					{ OrchestrationScriptConstants.OrchestrationScriptActionRequestScriptInfoKey, OrchestrationScriptAction.PerformOrchestration.ToString() },
					{ OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey, JsonConvert.SerializeObject(input) },
				},
			};
		}

		private Mock<IEngine> CreateEngine(Action<ProfileInstance> configureInstance = null)
		{
			var definition = new ProfileDefinition(Guid.NewGuid()) { Name = DefinitionName };
			definition.Parameters.Add(_capabilityParameter);
			definition.Parameters.Add(_capacityParameter);

			var instance = new ProfileInstance(Guid.NewGuid())
			{
				Name = InstanceName,
				AppliesToID = definition.ID,
				Values = new[]
				{
					new ProfileParameterEntry
					{
						Parameter = _capabilityParameter,
						CapabilityUsageValue = new CapabilityUsageParameterValue { RequiredDiscreet = "North" },
					},
					new ProfileParameterEntry
					{
						Parameter = _capacityParameter,
						CapacityUsageValue = new CapacityUsageParameterValue { DecimalQuantity = 50m },
					},
				},
			};

			configureInstance?.Invoke(instance);

			var engine = new Mock<IEngine>();
			engine
				.Setup(e => e.SendSLNetMessages(It.IsAny<DMSMessage[]>()))
				.Returns((DMSMessage[] messages) => messages.Select(message => HandleMessage(message, definition, instance)).ToArray());

			return engine;
		}

		private static DMSMessage HandleMessage(DMSMessage message, ProfileDefinition definition, ProfileInstance instance)
		{
			switch (message)
			{
				case ManagerStoreStartPagingRequest<ProfileDefinition> request:
					return new ManagerStorePagingResponse<ProfileDefinition>
					{
						IsFinalPage = true,
						Objects = new[] { definition }.Where(request.Filter.Filter.getLambda()).ToList(),
					};

				case ManagerStoreStartPagingRequest<ProfileInstance> request:
					return new ManagerStorePagingResponse<ProfileInstance>
					{
						IsFinalPage = true,
						Objects = new[] { instance }.Where(request.Filter.Filter.getLambda()).ToList(),
					};

				default:
					throw new NotSupportedException($"Unexpected message {message.GetType().Name}");
			}
		}

		private sealed class TestOrchestrationScript : OrchestrationScript
		{
			public Dictionary<string, object> Values { get; } = new Dictionary<string, object>();

			public override IEnumerable<IOrchestrationParameters> GetParameters()
			{
				yield return new OrchestrationProfileDefinition(DefinitionName);
			}

			public override void Orchestrate(IEngine engine)
			{
				Values[CapabilityName] = GetParameterValue(CapabilityName);
				Values[CapacityName] = GetParameterValue(CapacityName);
			}
		}
	}
}
