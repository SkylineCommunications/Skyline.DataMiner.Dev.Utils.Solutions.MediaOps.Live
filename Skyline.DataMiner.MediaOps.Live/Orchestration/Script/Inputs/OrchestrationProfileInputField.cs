namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	/// <summary>
	/// A field that is backed by a profile parameter. The profile parameter is the source of truth for the value type;
	/// the script can only narrow what it accepts for this occurrence.
	/// This field is replaced by the matching typed field once the profile parameter is resolved.
	/// </summary>
	public class OrchestrationProfileInputField : OrchestrationInputField
	{
		/// <inheritdoc/>
		public override string Kind => OrchestrationInputKind.Profile;

		/// <summary>
		/// Gets the text values this field accepts, narrowing the discretes of a text based profile parameter.
		/// Leave empty to accept every discrete of the profile parameter.
		/// </summary>
		[JsonProperty("allowedTextValues")]
		public List<string> AllowedTextValues { get; } = new List<string>();

		/// <summary>
		/// Gets the numeric values this field accepts, narrowing the discretes of a numeric profile parameter.
		/// Leave empty to accept every discrete of the profile parameter.
		/// </summary>
		[JsonProperty("allowedNumberValues")]
		public List<double> AllowedNumberValues { get; } = new List<double>();

		/// <summary>
		/// Gets or sets the smallest accepted value, narrowing the range of the profile parameter.
		/// </summary>
		[JsonProperty("minimum", NullValueHandling = NullValueHandling.Ignore)]
		public double? Minimum { get; set; }

		/// <summary>
		/// Gets or sets the largest accepted value, narrowing the range of the profile parameter.
		/// </summary>
		[JsonProperty("maximum", NullValueHandling = NullValueHandling.Ignore)]
		public double? Maximum { get; set; }

		/// <summary>
		/// Gets or sets the increment between two accepted values, overriding the step size of the profile parameter.
		/// </summary>
		[JsonProperty("stepSize", NullValueHandling = NullValueHandling.Ignore)]
		public double? StepSize { get; set; }

		/// <summary>
		/// Gets a value indicating whether the script narrowed the definition of the profile parameter.
		/// </summary>
		[JsonIgnore]
		public bool HasOverride => HasAllowedValues || Minimum.HasValue || Maximum.HasValue || StepSize.HasValue;

		/// <summary>
		/// Gets a value indicating whether the script narrowed the discretes of the profile parameter.
		/// </summary>
		[JsonIgnore]
		public bool HasAllowedValues => AllowedTextValues.Count > 0 || AllowedNumberValues.Count > 0;

		/// <summary>
		/// Narrows the discretes of the profile parameter to the specified text values.
		/// The display text keeps coming from the profile parameter, so only the internal values are listed here.
		/// </summary>
		/// <param name="values">The values this field accepts.</param>
		public void Allow(params string[] values)
		{
			Allow((IEnumerable<string>)values);
		}

		/// <summary>
		/// Narrows the discretes of the profile parameter to the specified text values.
		/// </summary>
		/// <param name="values">The values this field accepts.</param>
		public void Allow(IEnumerable<string> values)
		{
			if (values != null)
			{
				AllowedTextValues.AddRange(values);
			}
		}

		/// <summary>
		/// Narrows the discretes of the profile parameter to the specified numeric values.
		/// </summary>
		/// <param name="values">The values this field accepts.</param>
		public void Allow(params double[] values)
		{
			Allow((IEnumerable<double>)values);
		}

		/// <summary>
		/// Narrows the discretes of the profile parameter to the specified numeric values.
		/// </summary>
		/// <param name="values">The values this field accepts.</param>
		public void Allow(IEnumerable<double> values)
		{
			if (values != null)
			{
				AllowedNumberValues.AddRange(values);
			}
		}

		internal IEnumerable<OrchestrationInputValue> GetAllowedValues()
		{
			return AllowedTextValues.Select(OrchestrationInputValue.FromText)
				.Concat(AllowedNumberValues.Select(OrchestrationInputValue.FromNumber));
		}
	}
}
