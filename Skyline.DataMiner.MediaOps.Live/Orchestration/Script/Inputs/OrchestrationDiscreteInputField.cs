namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	/// <summary>
	/// An orchestration input field that is limited to a predefined set of options.
	/// </summary>
	public class OrchestrationDiscreteInputField : OrchestrationInputField
	{
		/// <inheritdoc/>
		public override string Kind => OrchestrationInputKind.Discrete;

		/// <summary>
		/// Gets the options the operator can choose from.
		/// </summary>
		[JsonProperty("options")]
		public List<OrchestrationInputOption> Options { get; } = new List<OrchestrationInputOption>();

		/// <summary>
		/// Adds an option whose display text equals its value.
		/// </summary>
		/// <param name="value">The value that is passed to the orchestration script.</param>
		/// <returns>The current field.</returns>
		public OrchestrationDiscreteInputField AddOption(string value)
		{
			Options.Add(new OrchestrationInputOption(value));
			return this;
		}

		/// <summary>
		/// Adds an option that shows one text and passes another value to the orchestration script.
		/// </summary>
		/// <param name="display">The text that is shown to the operator.</param>
		/// <param name="value">The value that is passed to the orchestration script.</param>
		/// <returns>The current field.</returns>
		public OrchestrationDiscreteInputField AddOption(string display, string value)
		{
			Options.Add(new OrchestrationInputOption(display, value));
			return this;
		}

		/// <summary>
		/// Adds an option that shows a text and passes a number to the orchestration script.
		/// </summary>
		/// <param name="display">The text that is shown to the operator.</param>
		/// <param name="value">The value that is passed to the orchestration script.</param>
		/// <returns>The current field.</returns>
		public OrchestrationDiscreteInputField AddOption(string display, double value)
		{
			Options.Add(new OrchestrationInputOption(display, value));
			return this;
		}

		/// <inheritdoc/>
		public override bool IsValidValue(OrchestrationInputValue value, out string error)
		{
			if (!base.IsValidValue(value, out error))
			{
				return false;
			}

			if (value == null)
			{
				return true;
			}

			if (!Options.Any(option => option.Value == value))
			{
				error = $"'{Name}' does not allow the value '{value}'.";
				return false;
			}

			return true;
		}

		/// <summary>
		/// Gets the display text of the option that matches the current value.
		/// </summary>
		/// <returns>The display text, or <see langword="null"/> when no option matches.</returns>
		public string GetSelectedDisplayValue()
		{
			var value = GetEffectiveValue();

			return Options.FirstOrDefault(option => option.Value == value)?.GetDisplayText();
		}

		/// <inheritdoc/>
		public override string FormatValue(OrchestrationInputValue value)
		{
			return value == null ? null : Options.FirstOrDefault(option => option.Value == value)?.GetDisplayText() ?? value.ToString();
		}

		internal override void ValidateDefinition()
		{
			if (Options.Count == 0)
			{
				ThrowUnusableDefinition(Path, "it has no options.");
			}

			if (Options.Any(option => option?.Value == null))
			{
				ThrowUnusableDefinition(Path, "an option has no value.");
			}

			var duplicate = Options.GroupBy(option => option.Value).FirstOrDefault(group => group.Count() > 1);

			if (duplicate != null)
			{
				ThrowUnusableDefinition(Path, $"the value '{duplicate.Key}' is used by more than one option.");
			}

			base.ValidateDefinition();
		}
	}
}
