namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Newtonsoft.Json;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationInputRules
	{
		[TestMethod]
		public void OrchestrationInputBuilder_Build_AllowsTheMaximumDepth()
		{
			var definition = new OrchestrationInputBuilder()
				.AddGroup("L1", l1 => l1.AddGroup("L2", l2 => l2.AddGroup("L3", l3 => l3.AddGroup("L4", l4 => l4.AddText("L5")))))
				.Build();

			Assert.IsTrue(definition.TryGetField("L1/L2/L3/L4/L5", out _));
		}

		[TestMethod]
		public void OrchestrationInputBuilder_Build_RejectsItemsDeeperThanTheMaximumDepth()
		{
			var builder = new OrchestrationInputBuilder()
				.AddGroup("L1", l1 => l1.AddGroup("L2", l2 => l2.AddGroup("L3", l3 => l3.AddGroup("L4", l4 => l4.AddGroup("L5", l5 => l5.AddText("L6"))))));

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());

			StringAssert.Contains(exception.Message, "L1/L2/L3/L4/L5");
			StringAssert.Contains(exception.Message, "5 levels");
		}

		[TestMethod]
		public void OrchestrationInputDefinition_ValidateStructure_RejectsACycle()
		{
			var definition = new OrchestrationInputBuilder()
				.AddGroup("Loop", loop => loop.AddText("Name"))
				.Build();
			Assert.IsTrue(definition.TryGetGroup("Loop", out var group));

			group.Children.Add(group);

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => definition.ValidateStructure());

			StringAssert.Contains(exception.Message, "cycle");
		}

		[TestMethod]
		public void OrchestrationInputDefinition_Evaluate_RejectsACycle()
		{
			OrchestrationInputDefinition Cyclic(OrchestrationInputValues values)
			{
				var group = new OrchestrationInputGroup { Name = "Loop", Path = "Loop" };
				group.Children.Add(group);

				var definition = new OrchestrationInputDefinition();
				definition.Items.Add(group);
				return definition;
			}

			Assert.ThrowsExactly<InvalidOperationException>(() => OrchestrationInputDefinition.Evaluate(Cyclic, OrchestrationInputValues.Empty));
		}

		[TestMethod]
		public void OrchestrationNumberInputField_ValidateStructure_RejectsAnEmptyRange()
		{
			var builder = new OrchestrationInputBuilder()
				.AddNumber("Frequency", field =>
				{
					field.Minimum = 12;
					field.Maximum = 10;
				});

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());

			StringAssert.Contains(exception.Message, "'Frequency' has no usable definition");
		}

		[TestMethod]
		public void OrchestrationNumberInputField_ValidateStructure_RejectsANonPositiveStepSize()
		{
			var builder = new OrchestrationInputBuilder()
				.AddNumber("Frequency", field => field.StepSize = 0);

			Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
		}

		[TestMethod]
		public void OrchestrationNumberInputField_ValidateStructure_RejectsADefaultOutsideTheRange()
		{
			var builder = new OrchestrationInputBuilder()
				.AddNumber("Count", field =>
				{
					field.Minimum = 1;
					field.Maximum = 4;
					field.DefaultValue = 5;
				});

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());

			StringAssert.Contains(exception.Message, "default value");
		}

		[TestMethod]
		public void OrchestrationNumberInputField_ValidateStructure_RejectsADefaultOfTheWrongType()
		{
			var builder = new OrchestrationInputBuilder()
				.AddNumber("Count", field => field.DefaultValue = "many");

			Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
		}

		[TestMethod]
		public void OrchestrationDiscreteInputField_ValidateStructure_RejectsAFieldWithoutOptions()
		{
			var builder = new OrchestrationInputBuilder()
				.AddDiscrete("Satellite");

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());

			StringAssert.Contains(exception.Message, "no options");
		}

		[TestMethod]
		public void OrchestrationDiscreteInputField_ValidateStructure_RejectsDuplicateOptionValues()
		{
			var builder = new OrchestrationInputBuilder()
				.AddDiscrete(
					"Band",
					new[] { new OrchestrationInputOption("C band", "C"), new OrchestrationInputOption("Also C", "C") });

			Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
		}

		[TestMethod]
		public void OrchestrationDateTimeInputField_IsValidValue_ChecksTheRange()
		{
			var field = new OrchestrationDateTimeInputField
			{
				Name = "Start",
				Minimum = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				Maximum = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
			};

			Assert.IsTrue(field.IsValidValue(new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc), out _));
			Assert.IsFalse(field.IsValidValue(new DateTime(2025, 12, 31, 23, 0, 0, DateTimeKind.Utc), out var tooEarly));
			Assert.IsFalse(field.IsValidValue(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), out _));
			Assert.IsFalse(field.IsValidValue("not a date", out var notADate));

			StringAssert.Contains(tooEarly, "cannot be before");
			StringAssert.Contains(notADate, "requires a date and time");
		}

		[TestMethod]
		public void OrchestrationDateTimeInputField_ValidateStructure_RejectsAnEmptyRange()
		{
			var builder = new OrchestrationInputBuilder()
				.AddDateTime("Start", field =>
				{
					field.Minimum = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
					field.Maximum = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				});

			Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
		}

		[TestMethod]
		public void OrchestrationTimeSpanInputField_IsValidValue_ChecksTheRange()
		{
			var field = new OrchestrationTimeSpanInputField
			{
				Name = "Pre-roll",
				Minimum = TimeSpan.FromMinutes(1),
				Maximum = TimeSpan.FromMinutes(30),
			};

			Assert.IsTrue(field.IsValidValue(TimeSpan.FromMinutes(5), out _));
			Assert.IsFalse(field.IsValidValue(TimeSpan.FromSeconds(30), out var tooShort));
			Assert.IsFalse(field.IsValidValue(TimeSpan.FromHours(1), out _));

			StringAssert.Contains(tooShort, "must be at least 00:01:00");
		}

		[TestMethod]
		public void OrchestrationTimeSpanInputField_ValidateStructure_RejectsAnEmptyRange()
		{
			var builder = new OrchestrationInputBuilder()
				.AddTimeSpan("Pre-roll", field =>
				{
					field.Minimum = TimeSpan.FromMinutes(10);
					field.Maximum = TimeSpan.FromMinutes(5);
				});

			Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
		}

		[TestMethod]
		public void OrchestrationInputValue_FromDateTime_IsHeldAsUtcText()
		{
			var local = new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Local);

			OrchestrationInputValue value = local;

			Assert.IsTrue(value.IsText);
			Assert.IsTrue(value.TryGetDateTime(out var dateTime));
			Assert.AreEqual(DateTimeKind.Utc, dateTime.Kind);
			Assert.AreEqual(local.ToUniversalTime(), dateTime);
		}

		[TestMethod]
		public void OrchestrationInputValue_FromTimeSpan_IsHeldAsSeconds()
		{
			OrchestrationInputValue value = TimeSpan.FromMinutes(90);

			Assert.IsTrue(value.IsNumber);
			Assert.AreEqual(5400d, value.Number);
			Assert.IsTrue(value.TryGetTimeSpan(out var timeSpan));
			Assert.AreEqual(TimeSpan.FromMinutes(90), timeSpan);
		}

		[TestMethod]
		public void OrchestrationInputDefinition_Evaluate_TakesDefaultsFromAPresetDropdown()
		{
			OrchestrationInputDefinition GetInputs(OrchestrationInputValues values)
			{
				var isUhd = values.HasValue("Preset", "UHD");

				return new OrchestrationInputBuilder()
					.AddDiscrete("Preset", field =>
					{
						field.DefaultValue = "HD";
						field.TriggersReevaluation = true;
					}, "HD", "UHD")
					.AddNumber("Bitrate", field => field.DefaultValue = isUhd ? 50 : 20)
					.Build();
			}

			var preset = OrchestrationInputDefinition.Evaluate(GetInputs, new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue> { ["Preset"] = "UHD" }));
			var overridden = OrchestrationInputDefinition.Evaluate(GetInputs, new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue> { ["Preset"] = "UHD", ["Bitrate"] = 35 }));

			Assert.AreEqual(50d, preset.GetValues().GetNumber("Bitrate"));
			Assert.AreEqual(35d, overridden.GetValues().GetNumber("Bitrate"), "Expected an explicit value to win over the preset.");
		}

		[TestMethod]
		public void OrchestrationProfile_SetInputValues_RoundTripsDateTimeAndTimeSpan()
		{
			var start = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
			var profile = new OrchestrationProfile();

			profile.SetInputValues(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
			{
				["Start"] = start,
				["Duration"] = TimeSpan.FromSeconds(45),
			}));

			var restored = profile.GetInputValues();

			Assert.AreEqual(start, restored.GetDateTime("Start"));
			Assert.AreEqual(TimeSpan.FromSeconds(45), restored.GetTimeSpan("Duration"));
		}

		[TestMethod]
		public void OrchestrationProfile_SetInputValue_ReplacesAndRemovesASingleValue()
		{
			var profile = new OrchestrationProfile();

			profile.SetInputValue("Endpoint", "ENC-A");
			profile.SetInputValue("Endpoint", "ENC-B");
			profile.SetInputValue("Count", 2);

			Assert.HasCount(2, profile.Values);
			Assert.AreEqual("ENC-B", profile.GetInputValues().GetString("Endpoint"));

			profile.SetInputValue("Endpoint", null);

			Assert.IsFalse(profile.GetInputValues().Contains("Endpoint"));
			Assert.AreEqual(2, profile.GetInputValues().GetInt32("Count"));
		}

		[TestMethod]
		public void OrchestrationInputValues_GetDateTimeAndGetTimeSpan_ReadTypedValues()
		{
			var start = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
			var values = new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
			{
				["Start"] = start,
				["Duration"] = TimeSpan.FromHours(2),
				["Frequency"] = 11.5,
			});

			Assert.AreEqual(start, values.GetDateTime("Start"));
			Assert.AreEqual(TimeSpan.FromHours(2), values.GetTimeSpan("Duration"));
			Assert.AreEqual(11.5, values.GetNumber("Frequency"));
			Assert.IsNull(values.GetDateTime("Frequency"));
			Assert.IsNull(values.GetNumber("Missing"));
		}

		[TestMethod]
		public void OrchestrationInputField_TryValidate_ReportsTheValidationMessageOfTheScript()
		{
			var field = new OrchestrationTextInputField
			{
				Name = "Endpoint",
				Value = "ENC-A",
				IsValid = false,
				ValidationMessage = "ENC-A is in maintenance.",
			};

			Assert.IsFalse(field.TryValidate(out var error));
			Assert.AreEqual("ENC-A is in maintenance.", error);
		}

		[TestMethod]
		public void OrchestrationInputField_TryValidate_FallsBackToAGenericMessage()
		{
			var field = new OrchestrationTextInputField { Name = "Endpoint", IsValid = false };

			Assert.IsFalse(field.TryValidate(out var error));
			Assert.AreEqual("'Endpoint' is not valid.", error);
		}

		[TestMethod]
		public void OrchestrationInputDefinition_TryValidateValues_IncludesTheValidityOfTheScript()
		{
			var definition = new OrchestrationInputBuilder()
				.AddText("Endpoint", field =>
				{
					field.IsValid = false;
					field.ValidationMessage = "Not reachable.";
				})
				.Build();

			Assert.IsFalse(definition.TryValidateValues(out var errors));
			CollectionAssert.Contains(errors.ToList(), "Not reachable.");
		}

		[TestMethod]
		public void OrchestrationInputDefinition_Serialization_RoundTripsTheNewFieldProperties()
		{
			var start = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
			var definition = new OrchestrationInputBuilder()
				.AddDateTime("Start", field =>
				{
					field.Minimum = start;
					field.Precision = OrchestrationTimePrecision.Hour;
					field.Value = start.AddHours(1);
				})
				.AddTimeSpan("Duration", field =>
				{
					field.Maximum = TimeSpan.FromHours(4);
					field.Precision = OrchestrationTimePrecision.Second;
					field.DefaultValue = TimeSpan.FromHours(1);
				})
				.AddText("Comment", field =>
				{
					field.IsDisabled = true;
					field.IsValid = false;
					field.ValidationMessage = "Read only.";
				})
				.Build();

			var restored = JsonConvert.DeserializeObject<OrchestrationInputDefinition>(JsonConvert.SerializeObject(definition));

			Assert.IsTrue(restored.TryGetField("Start", out var startField));
			var restoredStart = (OrchestrationDateTimeInputField)startField;
			Assert.AreEqual(start, restoredStart.Minimum.Value.ToUniversalTime());
			Assert.AreEqual(OrchestrationTimePrecision.Hour, restoredStart.Precision);
			Assert.IsTrue(restoredStart.Value.TryGetDateTime(out var restoredValue));
			Assert.AreEqual(start.AddHours(1), restoredValue);

			Assert.IsTrue(restored.TryGetField("Duration", out var durationField));
			var restoredDuration = (OrchestrationTimeSpanInputField)durationField;
			Assert.AreEqual(TimeSpan.FromHours(4), restoredDuration.Maximum);
			Assert.AreEqual(OrchestrationTimePrecision.Second, restoredDuration.Precision);
			Assert.AreEqual(OrchestrationInputValue.FromTimeSpan(TimeSpan.FromHours(1)), restoredDuration.DefaultValue);

			Assert.IsTrue(restored.TryGetField("Comment", out var commentField));
			Assert.IsTrue(commentField.IsDisabled);
			Assert.IsFalse(commentField.IsValid);
			Assert.AreEqual("Read only.", commentField.ValidationMessage);
		}
	}
}
