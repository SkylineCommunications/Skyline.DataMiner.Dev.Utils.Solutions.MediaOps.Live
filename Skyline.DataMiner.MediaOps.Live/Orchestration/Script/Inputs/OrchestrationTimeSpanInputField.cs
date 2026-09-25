namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;
	using System.Globalization;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	/// <summary>
	/// An orchestration input field that accepts a duration within an optional range.
	/// </summary>
	public class OrchestrationTimeSpanInputField : OrchestrationInputField
	{
		/// <inheritdoc/>
		public override string Kind => OrchestrationInputKind.TimeSpan;

		/// <summary>
		/// Gets or sets the shortest accepted duration, or <see langword="null"/> when there is no lower bound.
		/// </summary>
		[JsonProperty("minimum", NullValueHandling = NullValueHandling.Ignore)]
		public TimeSpan? Minimum { get; set; }

		/// <summary>
		/// Gets or sets the longest accepted duration, or <see langword="null"/> when there is no upper bound.
		/// </summary>
		[JsonProperty("maximum", NullValueHandling = NullValueHandling.Ignore)]
		public TimeSpan? Maximum { get; set; }

		/// <summary>
		/// Gets or sets the smallest unit the operator picks.
		/// </summary>
		[JsonProperty("precision")]
		[JsonConverter(typeof(StringEnumConverter))]
		public OrchestrationTimePrecision Precision { get; set; } = OrchestrationTimePrecision.Minute;

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

			if (!value.TryGetTimeSpan(out var timeSpan))
			{
				error = $"'{Name}' requires a duration.";
				return false;
			}

			if (Minimum.HasValue && timeSpan < Minimum.Value)
			{
				error = $"'{Name}' must be at least {Format(Minimum.Value)}.";
				return false;
			}

			if (Maximum.HasValue && timeSpan > Maximum.Value)
			{
				error = $"'{Name}' must be at most {Format(Maximum.Value)}.";
				return false;
			}

			return true;
		}

		/// <inheritdoc/>
		public override string FormatValue(OrchestrationInputValue value)
		{
			return value != null && value.TryGetTimeSpan(out var timeSpan) ? Format(timeSpan) : base.FormatValue(value);
		}

		internal override void ValidateDefinition()
		{
			if (Minimum.HasValue && Maximum.HasValue && Minimum.Value > Maximum.Value)
			{
				ThrowUnusableDefinition(Path, $"the minimum {Format(Minimum.Value)} is longer than the maximum {Format(Maximum.Value)}.");
			}

			base.ValidateDefinition();
		}

		private static string Format(TimeSpan timeSpan)
		{
			return timeSpan.ToString("c", CultureInfo.InvariantCulture);
		}
	}
}
