namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;
	using System.Globalization;

	using Newtonsoft.Json;

	/// <summary>
	/// An orchestration input field that accepts a number within an optional range.
	/// </summary>
	public class OrchestrationNumberInputField : OrchestrationInputField
	{
		/// <inheritdoc/>
		public override string Kind => OrchestrationInputKind.Number;

		/// <summary>
		/// Gets or sets the smallest accepted value, or <see langword="null"/> when there is no lower bound.
		/// </summary>
		[JsonProperty("minimum", NullValueHandling = NullValueHandling.Ignore)]
		public double? Minimum { get; set; }

		/// <summary>
		/// Gets or sets the largest accepted value, or <see langword="null"/> when there is no upper bound.
		/// </summary>
		[JsonProperty("maximum", NullValueHandling = NullValueHandling.Ignore)]
		public double? Maximum { get; set; }

		/// <summary>
		/// Gets or sets the increment between two accepted values.
		/// </summary>
		[JsonProperty("stepSize", NullValueHandling = NullValueHandling.Ignore)]
		public double? StepSize { get; set; }

		/// <summary>
		/// Gets or sets the number of decimals that is shown to the operator.
		/// </summary>
		[JsonProperty("decimals")]
		public int Decimals { get; set; }

		/// <summary>
		/// Gets or sets the unit that is shown next to the value.
		/// </summary>
		[JsonProperty("unit", NullValueHandling = NullValueHandling.Ignore)]
		public string Unit { get; set; }

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

			if (!value.TryGetNumber(out var number))
			{
				error = $"'{Name}' requires a numeric value.";
				return false;
			}

			if (Minimum.HasValue && number < Minimum.Value)
			{
				error = $"'{Name}' must be at least {Minimum.Value.ToString(CultureInfo.InvariantCulture)}.";
				return false;
			}

			if (Maximum.HasValue && number > Maximum.Value)
			{
				error = $"'{Name}' must be at most {Maximum.Value.ToString(CultureInfo.InvariantCulture)}.";
				return false;
			}

			return true;
		}

		/// <inheritdoc/>
		public override string FormatValue(OrchestrationInputValue value)
		{
			var text = base.FormatValue(value);

			return text == null || String.IsNullOrEmpty(Unit) ? text : $"{text} {Unit}";
		}

		internal override void ValidateDefinition()
		{
			if ((Minimum.HasValue && Double.IsNaN(Minimum.Value)) || (Maximum.HasValue && Double.IsNaN(Maximum.Value)))
			{
				ThrowUnusableDefinition(Path, "the range is not a number.");
			}

			if (Minimum.HasValue && Maximum.HasValue && Minimum.Value > Maximum.Value)
			{
				ThrowUnusableDefinition(Path, $"the minimum {Minimum.Value.ToString(CultureInfo.InvariantCulture)} is larger than the maximum {Maximum.Value.ToString(CultureInfo.InvariantCulture)}.");
			}

			if (StepSize.HasValue && !(StepSize.Value > 0))
			{
				ThrowUnusableDefinition(Path, "the step size must be larger than zero.");
			}

			if (Decimals < 0)
			{
				ThrowUnusableDefinition(Path, "the number of decimals cannot be negative.");
			}

			base.ValidateDefinition();
		}
	}
}
