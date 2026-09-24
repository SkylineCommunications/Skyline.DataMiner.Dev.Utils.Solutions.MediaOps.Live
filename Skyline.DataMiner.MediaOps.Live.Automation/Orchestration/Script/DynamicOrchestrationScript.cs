namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	/// <summary>
	/// Base class for orchestration scripts whose inputs depend on the values that were already provided,
	/// so that items can appear or disappear, options and ranges can change, and groups can be repeated.
	/// </summary>
	public abstract class DynamicOrchestrationScript : OrchestrationScriptBase
	{
		/// <summary>
		/// Gets the input items of this script, evaluated for the values that are currently provided.
		/// </summary>
		public OrchestrationInputDefinition Inputs { get; private set; }

		/// <summary>
		/// Gets the resolved values of the input items of this script.
		/// </summary>
		public OrchestrationInputValues InputValues { get; private set; } = OrchestrationInputValues.Empty;

		/// <summary>
		/// Gets the input items this script requires, based on the values that were already provided.
		/// This method is called again every time a value changes that affects which items are relevant.
		/// </summary>
		/// <param name="providedValues">The values that were already provided, keyed by the path of the field they belong to.</param>
		/// <returns>The input items to expose.</returns>
		public abstract OrchestrationInputDefinition GetInputs(OrchestrationInputValues providedValues);

		/// <summary>
		/// Performs the orchestration with the values that were provided for the inputs of this script.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		/// <param name="inputs">The resolved values of the inputs of this script, addressed by path.</param>
		public abstract void Orchestrate(IEngine engine, OrchestrationInputValues inputs);

		/// <summary>
		/// Attempts to get the value of the input field with the specified path.
		/// </summary>
		/// <param name="path">The path of the field, for example <c>destinations/1/endpoint</c>.</param>
		/// <param name="value">When this method returns <see langword="true"/>, contains the value of the field.</param>
		/// <returns><see langword="true"/> when the field holds a value; otherwise, <see langword="false"/>.</returns>
		public bool TryGetInputValue(string path, out OrchestrationInputValue value)
		{
			return InputValues.TryGetValue(path, out value);
		}

		/// <summary>
		/// Gets the value of the input field with the specified path.
		/// </summary>
		/// <param name="path">The path of the field, for example <c>destinations/1/endpoint</c>.</param>
		/// <returns>The value of the field.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the field does not hold a value.</exception>
		public OrchestrationInputValue GetInputValue(string path)
		{
			if (!TryGetInputValue(path, out var value))
			{
				throw new InvalidOperationException($"Orchestration input '{path}' is missing");
			}

			return value;
		}

		internal override IEnumerable<IOrchestrationParameters> GetProfileParameters()
		{
			return Enumerable.Empty<IOrchestrationParameters>();
		}

		internal override OrchestrationInputDefinition EvaluateInputs(IEngine engine, OrchestrationInputValues providedValues)
		{
			OrchestrationInputValues values = providedValues ?? OrchestrationInputValues.Empty;
			var profileResolver = new OrchestrationInputProfileResolver(new ProfileHelper(engine.SendSLNetMessages));

			OrchestrationInputDefinition definition = OrchestrationInputDefinition.Evaluate(v => ResolveInputs(profileResolver, v), values);

			Inputs = definition;
			InputValues = definition.GetValues();

			return definition;
		}

		internal override void ExecuteOrchestration(IEngine engine)
		{
			Orchestrate(engine, InputValues);
		}

		private OrchestrationInputDefinition ResolveInputs(OrchestrationInputProfileResolver profileResolver, OrchestrationInputValues values)
		{
			OrchestrationInputDefinition definition = GetInputs(values)
				?? throw new InvalidOperationException($"'{GetType().Name}.{nameof(GetInputs)}' must return an input definition.");

			profileResolver.Resolve(definition);

			return definition;
		}
	}
}
