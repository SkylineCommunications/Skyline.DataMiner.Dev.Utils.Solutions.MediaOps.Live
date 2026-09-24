namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Newtonsoft.Json;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationInputs
	{
		private const string NumberOfDestinationsPath = "General/Number of destinations";
		private const string RoutingModePath = "General/Routing mode";

		[TestMethod]
		public void OrchestrationInputBuilder_AddGroup_AssignsHierarchicalPaths()
		{
			var definition = new OrchestrationInputBuilder()
				.AddGroup("General", general =>
				{
					general.AddText("Name");
					general.AddGroup("Advanced", advanced => advanced.AddNumber("Retries"));
				})
				.Build();

			Assert.IsTrue(definition.TryGetField("General/Name", out _));
			Assert.IsTrue(definition.TryGetField("General/Advanced/Retries", out _));
			Assert.IsTrue(definition.TryGetGroup("General/Advanced", out _));
		}

		[TestMethod]
		public void OrchestrationInputBuilder_AddGroupInALoop_CreatesOneGroupPerRepetition()
		{
			var definition = BuildDestinationsDefinition(3);

			Assert.IsTrue(definition.TryGetGroup("Destinations", out var destinations));
			Assert.HasCount(3, destinations.Groups.ToList());

			var second = destinations.Groups.ElementAt(1);
			Assert.AreEqual("Destination 2", second.Name);
			Assert.AreEqual("Destinations/Destination 2", second.Path);
			Assert.IsTrue(definition.TryGetField("Destinations/Destination 2/Endpoint", out _));
		}

		[TestMethod]
		public void OrchestrationInputBuilder_AddGroupInALoop_KeepsValuesOfRepetitionsSeparate()
		{
			var definition = BuildDestinationsDefinition(2);

			definition.ApplyValues(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
			{
				["Destinations/Destination 1/Endpoint"] = "ENC-A",
				["Destinations/Destination 2/Endpoint"] = "ENC-B",
			}));

			Assert.IsTrue(definition.TryGetField("Destinations/Destination 1/Endpoint", out var first));
			Assert.IsTrue(definition.TryGetField("Destinations/Destination 2/Endpoint", out var second));
			Assert.AreEqual<OrchestrationInputValue>("ENC-A", first.Value);
			Assert.AreEqual<OrchestrationInputValue>("ENC-B", second.Value);
		}

		[TestMethod]
		public void OrchestrationInputBuilder_DuplicateName_Throws()
		{
			var builder = new OrchestrationInputBuilder();
			builder.AddText("Endpoint");

			Assert.ThrowsExactly<ArgumentException>(() => builder.AddNumber("Endpoint"));
		}

		[TestMethod]
		public void OrchestrationInputBuilder_NameWithSeparator_Throws()
		{
			var builder = new OrchestrationInputBuilder();

			Assert.ThrowsExactly<ArgumentException>(() => builder.AddText("Audio/Video"));
		}

		[TestMethod]
		public void OrchestrationInputBuilder_NestedLoops_AreAllowed()
		{
			var definition = new OrchestrationInputBuilder()
				.AddGroup("Destinations", destinations =>
				{
					for (var destination = 1; destination <= 2; destination++)
					{
						destinations.AddGroup($"Destination {destination}", d =>
						{
							for (var track = 1; track <= 2; track++)
							{
								d.AddGroup($"Audio {track}", audio => audio.AddText("Language"));
							}
						});
					}
				})
				.Build();

			Assert.IsTrue(definition.TryGetField("Destinations/Destination 2/Audio 2/Language", out _));
			Assert.HasCount(4, definition.GetAllFields().ToList());
		}

		[TestMethod]
		public void OrchestrationInputDefinition_GetParameters_ReevaluatesRepetitionsWhenControllingValueChanges()
		{
			var afterOneDestination = EvaluateExampleScript(new OrchestrationInputValues());
			Assert.IsTrue(afterOneDestination.TryGetGroup("Destinations", out var oneDestination));
			Assert.HasCount(1, oneDestination.Groups.ToList());

			var values = afterOneDestination.GetValues().ToDictionary();
			values[NumberOfDestinationsPath] = 3;

			var afterThreeDestinations = EvaluateExampleScript(new OrchestrationInputValues(values));
			Assert.IsTrue(afterThreeDestinations.TryGetGroup("Destinations", out var threeDestinations));
			Assert.HasCount(3, threeDestinations.Groups.ToList());
		}

		[TestMethod]
		public void OrchestrationInputDefinition_GetParameters_ShowsDependentGroupOnlyWhenRelevant()
		{
			var unicast = EvaluateExampleScript(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
			{
				[RoutingModePath] = "Unicast",
			}));

			Assert.IsFalse(unicast.TryGetGroup("Multicast settings", out _));

			var multicast = EvaluateExampleScript(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
			{
				[RoutingModePath] = "Multicast",
			}));

			Assert.IsTrue(multicast.TryGetGroup("Multicast settings", out _));
			Assert.IsTrue(multicast.TryGetField("Multicast settings/Multicast address", out _));
		}

		[TestMethod]
		public void OrchestrationInputDefinition_GetParameters_NarrowsOptionsBasedOnOtherValue()
		{
			var multicast = EvaluateExampleScript(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
			{
				[RoutingModePath] = "Multicast",
			}));

			Assert.IsTrue(multicast.TryGetField("Destinations/Destination 1/Format", out var format));

			var options = ((OrchestrationDiscreteInputField)format).Options.Select(x => x.Value.Text).ToList();
			Assert.Contains("SMPTE 2110", options);
			Assert.DoesNotContain("SDI", options);
		}

		[TestMethod]
		public void OrchestrationInputDefinition_ApplyValues_DropsValuesThatNoLongerHaveAField()
		{
			var definition = BuildDestinationsDefinition(1);

			definition.ApplyValues(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
			{
				["Destinations/Destination 1/Endpoint"] = "ENC-A",
				["Destinations/Destination 2/Endpoint"] = "ENC-B",
			}));

			Assert.IsFalse(definition.GetValues().Contains("Destinations/Destination 2/Endpoint"));
		}

		[TestMethod]
		public void OrchestrationInputDefinition_TryValidateValues_ReportsValueOutsideOfRange()
		{
			var definition = new OrchestrationInputBuilder()
				.AddNumber("Retries", field =>
				{
					field.Minimum = 1;
					field.Maximum = 5;
				})
				.Build();

			definition.ApplyValues(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue> { ["Retries"] = 9 }));

			Assert.IsFalse(definition.TryValidateValues(out var errors));
			Assert.HasCount(1, errors);
		}

		[TestMethod]
		public void OrchestrationInputDefinition_TryValidateValues_ReportsValueThatIsNoLongerAnOption()
		{
			var definition = new OrchestrationInputBuilder()
				.AddDiscrete("Format", "SMPTE 2110", "JPEG XS")
				.Build();

			definition.ApplyValues(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue> { ["Format"] = "SDI" }));

			Assert.IsFalse(definition.TryValidateValues(out _));
		}

		[TestMethod]
		public void OrchestrationInputDefinition_Serialization_RoundTripsGroupsFieldsAndValues()
		{
			var definition = BuildDestinationsDefinition(2);
			definition.ApplyValues(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
			{
				[NumberOfDestinationsPath] = 2,
				["Destinations/Destination 2/Endpoint"] = "ENC-B",
			}));

			var json = JsonConvert.SerializeObject(definition);
			var restored = JsonConvert.DeserializeObject<OrchestrationInputDefinition>(json);

			restored.ValidateStructure();

			Assert.AreEqual(OrchestrationInputDefinition.CurrentVersion, restored.Version);
			Assert.IsTrue(restored.TryGetGroup("Destinations", out var destinations));
			Assert.HasCount(2, destinations.Groups.ToList());

			Assert.IsTrue(restored.TryGetField("Destinations/Destination 2/Endpoint", out var endpoint));
			Assert.IsInstanceOfType<OrchestrationTextInputField>(endpoint);
			Assert.AreEqual<OrchestrationInputValue>("ENC-B", endpoint.Value);

			Assert.IsTrue(restored.TryGetField(NumberOfDestinationsPath, out var count));
			Assert.IsInstanceOfType<OrchestrationNumberInputField>(count);
			Assert.AreEqual(2, restored.GetValues().GetInt32(NumberOfDestinationsPath));
		}

		[TestMethod]
		public void OrchestrationInputValues_GetInt32_ClampsToTheProvidedBounds()
		{
			var values = new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue> { ["count"] = "42" });

			Assert.AreEqual(8, values.GetInt32("count", 1, 8));
			Assert.AreEqual(1, values.GetInt32("missing", 1, 8));
		}

		[TestMethod]
		public void OrchestrationDiscreteInputField_AddOption_SeparatesDisplayTextFromValue()
		{
			var definition = new OrchestrationInputBuilder()
				.AddDiscrete("Routing mode", field =>
				{
					field.AddOption("Unicast");
					field.AddOption("Multicast (SSM)", "MCAST");
					field.AddOption("Low latency", 1d);
				})
				.Build();

			Assert.IsTrue(definition.TryGetField("Routing mode", out var field));

			var options = ((OrchestrationDiscreteInputField)field).Options;
			Assert.AreEqual("Unicast", options[0].Display);
			Assert.AreEqual<OrchestrationInputValue>("Unicast", options[0].Value);
			Assert.AreEqual("Multicast (SSM)", options[1].Display);
			Assert.AreEqual<OrchestrationInputValue>("MCAST", options[1].Value);
			Assert.AreEqual("Low latency", options[2].Display);
			Assert.AreEqual<OrchestrationInputValue>(1d, options[2].Value);
		}

		[TestMethod]
		public void OrchestrationDiscreteInputField_GetSelectedDisplayValue_ReturnsTheDisplayTextOfTheStoredValue()
		{
			var definition = new OrchestrationInputBuilder()
				.AddDiscrete("Routing mode", field => field.AddOption("Multicast (SSM)", "MCAST"))
				.Build();

			definition.ApplyValues(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue> { ["Routing mode"] = "MCAST" }));

			Assert.IsTrue(definition.TryGetField("Routing mode", out var field));
			Assert.AreEqual("Multicast (SSM)", ((OrchestrationDiscreteInputField)field).GetSelectedDisplayValue());
		}

		[TestMethod]
		public void OrchestrationDiscreteInputField_IsValidValue_ChecksTheValueAndNotTheDisplayText()
		{
			var definition = new OrchestrationInputBuilder()
				.AddDiscrete("Routing mode", field => field.AddOption("Multicast (SSM)", "MCAST"))
				.Build();

			definition.ApplyValues(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue> { ["Routing mode"] = "Multicast (SSM)" }));

			Assert.IsFalse(definition.TryValidateValues(out _));
		}

		[TestMethod]
		public void OrchestrationInputDefinition_Serialization_RoundTripsOptionDisplayAndValue()
		{
			var definition = new OrchestrationInputBuilder()
				.AddDiscrete("Routing mode", field =>
				{
					field.AddOption("Multicast (SSM)", "MCAST");
					field.AddOption("Low latency", 1d);
				})
				.Build();

			var restored = JsonConvert.DeserializeObject<OrchestrationInputDefinition>(JsonConvert.SerializeObject(definition));

			Assert.IsTrue(restored.TryGetField("Routing mode", out var field));

			var options = ((OrchestrationDiscreteInputField)field).Options;
			Assert.AreEqual("Multicast (SSM)", options[0].Display);
			Assert.AreEqual<OrchestrationInputValue>("MCAST", options[0].Value);
			Assert.AreEqual("Low latency", options[1].Display);
			Assert.AreEqual<OrchestrationInputValue>(1d, options[1].Value);
		}

		[TestMethod]
		public void OrchestrationInputDefinition_Evaluate_AppliesDefaultOfControllingFieldToTheStructure()
		{
			var definition = OrchestrationInputDefinition.Evaluate(BuildDefinitionWithDefaultCount, OrchestrationInputValues.Empty);

			Assert.IsTrue(definition.TryGetGroup("Destinations", out var destinations));
			Assert.HasCount(3, destinations.Groups.ToList());
		}

		[TestMethod]
		public void OrchestrationInputDefinition_Evaluate_ProvidedValueOverridesTheDefault()
		{
			var providedValues = new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue> { [NumberOfDestinationsPath] = 1 });

			var definition = OrchestrationInputDefinition.Evaluate(BuildDefinitionWithDefaultCount, providedValues);

			Assert.IsTrue(definition.TryGetGroup("Destinations", out var destinations));
			Assert.HasCount(1, destinations.Groups.ToList());
		}

		[TestMethod]
		public void OrchestrationInputDefinition_Evaluate_DoesNotTurnADefaultIntoAnExplicitValue()
		{
			var definition = OrchestrationInputDefinition.Evaluate(BuildDefinitionWithDefaultCount, OrchestrationInputValues.Empty);

			Assert.IsTrue(definition.TryGetField(NumberOfDestinationsPath, out var field));
			Assert.IsNull(field.Value);
			Assert.AreEqual<OrchestrationInputValue>(3, field.GetEffectiveValue());
		}

		[TestMethod]
		public void OrchestrationInputDefinition_Evaluate_ReturnsNullForScriptsWithoutDynamicInputs()
		{
			Assert.IsNull(OrchestrationInputDefinition.Evaluate(_ => null, OrchestrationInputValues.Empty));
		}

		[TestMethod]
		public void OrchestrationInputDefinition_Evaluate_StopsWhenTheStructureKeepsChanging()
		{
			var evaluations = 0;

			// Deliberately unstable: the repetition count follows the number of evaluations so far.
			OrchestrationInputDefinition Unstable(OrchestrationInputValues values)
			{
				evaluations++;

				return new OrchestrationInputBuilder()
					.AddGroup("Items", items =>
					{
						for (var index = 1; index <= evaluations; index++)
						{
							var current = index;
							items.AddGroup($"Item {current}", item =>
								item.AddText("Name", field => field.DefaultValue = $"Item {current}"));
						}
					})
					.Build();
			}

			var definition = OrchestrationInputDefinition.Evaluate(Unstable, OrchestrationInputValues.Empty);

			Assert.IsNotNull(definition);
			Assert.IsLessThanOrEqualTo(5, evaluations);
		}

		private static OrchestrationInputDefinition BuildDefinitionWithDefaultCount(OrchestrationInputValues providedValues)
		{
			return new OrchestrationInputBuilder()
				.AddGroup("General", general =>
					general.AddNumber("Number of destinations", field =>
					{
						field.Minimum = 1;
						field.Maximum = 8;
						field.DefaultValue = 3;
					}))
				.AddGroup("Destinations", destinations =>
				{
					for (var index = 1; index <= providedValues.GetInt32(NumberOfDestinationsPath, 1, 8); index++)
					{
						destinations.AddGroup($"Destination {index}", destination =>
							destination.AddText("Endpoint"));
					}
				})
				.Build();
		}

		/// <summary>
		/// Mimics what an orchestration script implements in its <c>GetParameters</c> override.
		/// </summary>
		private static OrchestrationInputDefinition EvaluateExampleScript(OrchestrationInputValues providedValues)
		{
			var builder = new OrchestrationInputBuilder();

			builder.AddGroup("General", general =>
			{
				general.AddNumber("Number of destinations", field =>
				{
					field.Minimum = 1;
					field.Maximum = 8;
					field.DefaultValue = 1;
					field.IsRequired = true;
					field.TriggersReevaluation = true;
				});

				general.AddDiscrete("Routing mode", field => field.TriggersReevaluation = true, "Unicast", "Multicast");
			});

			if (providedValues.HasValue(RoutingModePath, "Multicast"))
			{
				builder.AddGroup("Multicast settings", multicast =>
					multicast.AddText("Multicast address", field => field.IsRequired = true));
			}

			var formats = providedValues.HasValue(RoutingModePath, "Multicast")
				? new[] { "SMPTE 2110", "JPEG XS" }
				: new[] { "SMPTE 2110", "JPEG XS", "SDI" };

			var destinationCount = providedValues.GetInt32(NumberOfDestinationsPath, 1, 8);

			builder.AddGroup("Destinations", destinations =>
			{
				for (var index = 1; index <= destinationCount; index++)
				{
					destinations.AddGroup($"Destination {index}", destination =>
					{
						destination.AddText("Endpoint", field => field.IsRequired = true);
						destination.AddDiscrete("Format", formats);
					});
				}
			});

			return builder.Build();
		}

		private static OrchestrationInputDefinition BuildDestinationsDefinition(int destinationCount)
		{
			return new OrchestrationInputBuilder()
				.AddGroup("General", general =>
					general.AddNumber("Number of destinations", field =>
					{
						field.Minimum = 1;
						field.Maximum = 8;
					}))
				.AddGroup("Destinations", destinations =>
				{
					for (var index = 1; index <= destinationCount; index++)
					{
						destinations.AddGroup($"Destination {index}", destination =>
							destination.AddText("Endpoint"));
					}
				})
				.Build();
		}
	}
}
