namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;
	using System.Globalization;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	/// <summary>
	/// An orchestration input field that accepts a date and time within an optional range. Values are handled in UTC.
	/// </summary>
	public class OrchestrationDateTimeInputField : OrchestrationInputField
	{
		/// <inheritdoc/>
		public override string Kind => OrchestrationInputKind.DateTime;

		/// <summary>
		/// Gets or sets the earliest accepted date and time, or <see langword="null"/> when there is no lower bound.
		/// </summary>
		[JsonProperty("minimum", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? Minimum { get; set; }

		/// <summary>
		/// Gets or sets the latest accepted date and time, or <see langword="null"/> when there is no upper bound.
		/// </summary>
		[JsonProperty("maximum", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? Maximum { get; set; }

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

			if (!value.TryGetDateTime(out var dateTime))
			{
				error = $"'{Name}' requires a date and time.";
				return false;
			}

			if (Minimum.HasValue && dateTime < OrchestrationInputValue.ToUniversal(Minimum.Value))
			{
				error = $"'{Name}' cannot be before {Format(Minimum.Value)}.";
				return false;
			}

			if (Maximum.HasValue && dateTime > OrchestrationInputValue.ToUniversal(Maximum.Value))
			{
				error = $"'{Name}' cannot be after {Format(Maximum.Value)}.";
				return false;
			}

			return true;
		}

		/// <inheritdoc/>
		public override string FormatValue(OrchestrationInputValue value)
		{
			if (value == null || !value.TryGetDateTime(out var dateTime))
			{
				return base.FormatValue(value);
			}

			switch (Precision)
			{
				case OrchestrationTimePrecision.Day:
					return dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

				case OrchestrationTimePrecision.Second:
					return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";

				default:
					return dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";
			}
		}

		internal override void ValidateDefinition()
		{
			if (Minimum.HasValue && Maximum.HasValue && OrchestrationInputValue.ToUniversal(Minimum.Value) > OrchestrationInputValue.ToUniversal(Maximum.Value))
			{
				ThrowUnusableDefinition(Path, $"the minimum {Format(Minimum.Value)} is later than the maximum {Format(Maximum.Value)}.");
			}

			base.ValidateDefinition();
		}

		private static string Format(DateTime dateTime)
		{
			return OrchestrationInputValue.ToUniversal(dateTime).ToString("u", CultureInfo.InvariantCulture);
		}
	}
}
