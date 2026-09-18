namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationProfileInputs
	{
		private const string NumberParameter = "IndividualProfileParam_Int";
		private const string TextParameter = "IndividualProfileParam_String";
		private const string SatelliteParameter = "Satellite";

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_TakesDefinitionFromTheProfileParameter()
		{
			var definition = Resolve(builder => builder.AddProfileParameter("Frequency", NumberParameter));

			Assert.IsTrue(definition.TryGetField("Frequency", out var field));
			Assert.IsInstanceOfType<OrchestrationNumberInputField>(field);
			Assert.IsTrue(field.IsProfileBacked);
			Assert.AreEqual(new Guid("986528dc-78af-4b09-b1c1-11dac21744b1"), field.ProfileParameterId);

			var number = (OrchestrationNumberInputField)field;
			Assert.AreEqual(0, number.Minimum);
			Assert.AreEqual(1000, number.Maximum);
			Assert.AreEqual(0.01, number.StepSize);
			Assert.AreEqual(2, number.Decimals);
			Assert.AreEqual("Units", number.Unit);
		}

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_AppliesTheNarrowedRange()
		{
			var definition = Resolve(builder => builder.AddProfileParameter("Frequency", NumberParameter, field =>
			{
				field.Minimum = 10;
				field.Maximum = 20;
			}));

			Assert.IsTrue(definition.TryGetField("Frequency", out var field));

			var number = (OrchestrationNumberInputField)field;
			Assert.AreEqual(10, number.Minimum);
			Assert.AreEqual(20, number.Maximum);
		}

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_RejectsARangeThatIsWiderThanTheProfileParameter()
		{
			var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
				Resolve(builder => builder.AddProfileParameter("Frequency", NumberParameter, field => field.Maximum = 5000)));

			Assert.Contains("only narrow", exception.Message);
		}

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_NarrowsTheDiscretesOfTheProfileParameter()
		{
			var definition = Resolve(builder =>
				builder.AddProfileParameter(SatelliteParameter, field => field.Allow(GetTrackableSatellites())));

			Assert.IsTrue(definition.TryGetField(SatelliteParameter, out var field));
			Assert.IsInstanceOfType<OrchestrationDiscreteInputField>(field);

			var options = ((OrchestrationDiscreteInputField)field).Options;
			Assert.HasCount(2, options);
			Assert.AreEqual("ASTRA 1M", options[0].Value);
			Assert.IsFalse(String.IsNullOrEmpty(options[0].Display));
			Assert.DoesNotContain("EUTELSAT 7B", options.Select(x => x.Value).ToList());
		}

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_TakesEveryDiscreteWhenNothingIsNarrowed()
		{
			var definition = Resolve(builder => builder.AddProfileParameter(SatelliteParameter));

			Assert.IsTrue(definition.TryGetField(SatelliteParameter, out var field));
			Assert.HasCount(3, ((OrchestrationDiscreteInputField)field).Options);
		}

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_RejectsAValueTheProfileParameterDoesNotDefine()
		{
			Assert.ThrowsExactly<InvalidOperationException>(() =>
				Resolve(builder => builder.AddProfileParameter(SatelliteParameter, field => field.Allow("ASTRA 9Z"))));
		}

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_RejectsTextAndNumericNarrowingAtTheSameTime()
		{
			var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
				Resolve(builder => builder.AddProfileParameter(SatelliteParameter, field =>
				{
					field.Allow("ASTRA 1M");
					field.Allow(1d);
				})));

			Assert.Contains("both text and numeric", exception.Message);
		}

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_RejectsARangeOnADiscreteProfileParameter()
		{
			Assert.ThrowsExactly<InvalidOperationException>(() =>
				Resolve(builder => builder.AddProfileParameter(SatelliteParameter, field => field.Minimum = 1)));
		}

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_ResolvesTextParameters()
		{
			var definition = Resolve(builder => builder.AddProfileParameter("Label", TextParameter));

			Assert.IsTrue(definition.TryGetField("Label", out var field));
			Assert.IsInstanceOfType<OrchestrationTextInputField>(field);
			Assert.IsTrue(field.IsProfileBacked);
		}

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_RejectsAnUnknownProfileParameter()
		{
			Assert.ThrowsExactly<InvalidOperationException>(() =>
				Resolve(builder => builder.AddProfileParameter("Missing", "DoesNotExist")));
		}

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_LeavesScriptLocalFieldsAlone()
		{
			var definition = Resolve(builder => builder.AddNumber("Number of destinations", field => field.Maximum = 8));

			Assert.IsTrue(definition.TryGetField("Number of destinations", out var field));
			Assert.IsFalse(field.IsProfileBacked);
			Assert.IsNull(field.ProfileParameterId);
		}

		[TestMethod]
		public void OrchestrationInputProfileResolver_Resolve_ResolvesFieldsInsideGroups()
		{
			var definition = Resolve(builder =>
				builder.AddGroup("Destinations", destinations =>
					destinations.AddGroup("Destination 1", destination =>
						destination.AddProfileParameter("Frequency", NumberParameter))));

			Assert.IsTrue(definition.TryGetField("Destinations/Destination 1/Frequency", out var field));
			Assert.IsTrue(field.IsProfileBacked);
			Assert.AreEqual("Destinations/Destination 1/Frequency", field.Path);
		}

		private static IEnumerable<string> GetTrackableSatellites()
		{
			return new[] { "ASTRA 1M", "ASTRA 3B" };
		}

		private static OrchestrationInputDefinition Resolve(Action<OrchestrationInputBuilder> configure)
		{
			var simulation = new MediaOpsLiveSimulation();

			simulation.Dms.AddDiscreteProfileParameter(
				SatelliteParameter,
				new Guid("3d4dec12-bc5b-4f7f-a9f1-bdacbbcb3d33"),
				InterpreteType.TypeEnum.String,
				new[]
				{
					new KeyValuePair<string, string>("ASTRA 1M", "Astra 1M"),
					new KeyValuePair<string, string>("ASTRA 3B", "Astra 3B"),
					new KeyValuePair<string, string>("EUTELSAT 7B", "Eutelsat 7B"),
				});

			var builder = new OrchestrationInputBuilder();
			configure(builder);
			var definition = builder.Build();

			new OrchestrationInputProfileResolver(new ProfileHelper(simulation.Api.Connection.HandleMessages)).Resolve(definition);

			return definition;
		}
	}
}
