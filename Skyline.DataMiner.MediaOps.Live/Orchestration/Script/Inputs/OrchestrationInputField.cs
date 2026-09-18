namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;

	using Newtonsoft.Json;

	/// <summary>
	/// Base class for every orchestration input item that holds a value.
	/// </summary>
	public abstract class OrchestrationInputField : OrchestrationInputItem
	{
		/// <summary>
		/// Gets or sets a value indicating whether a value must be provided before the event can be confirmed.
		/// </summary>
		[JsonProperty("isRequired")]
		public bool IsRequired { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether a change of this value requires the input definition to be reevaluated.
		/// </summary>
		[JsonProperty("triggersReevaluation")]
		public bool TriggersReevaluation { get; set; }

		/// <summary>
		/// Gets or sets the name of the profile parameter this field is backed by, or <see langword="null"/> when the field is script local.
		/// </summary>
		[JsonProperty("profileParameterName", NullValueHandling = NullValueHandling.Ignore)]
		public string ProfileParameterName { get; set; }

		/// <summary>
		/// Gets or sets the identifier of the profile parameter this field is backed by. It is filled in when the field is resolved.
		/// </summary>
		[JsonProperty("profileParameterId", NullValueHandling = NullValueHandling.Ignore)]
		public Guid? ProfileParameterId { get; set; }

		/// <summary>
		/// Gets a value indicating whether the value of this field is stored as a profile parameter value.
		/// Script local fields only mean something to the script itself and never take part in capability or capacity matching.
		/// </summary>
		[JsonIgnore]
		public bool IsProfileBacked => ProfileParameterId.HasValue;

		/// <summary>
		/// Gets or sets the value of this field.
		/// </summary>
		[JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
		public object Value { get; set; }

		/// <summary>
		/// Gets or sets the value that is used when the operator did not provide one.
		/// </summary>
		[JsonProperty("defaultValue", NullValueHandling = NullValueHandling.Ignore)]
		public object DefaultValue { get; set; }

		/// <summary>
		/// Gets the value of this field, falling back to the default value when no value is set.
		/// </summary>
		/// <returns>The effective value of this field.</returns>
		public object GetEffectiveValue()
		{
			return Value ?? DefaultValue;
		}

		/// <summary>
		/// Determines whether the specified value is acceptable for this field.
		/// </summary>
		/// <param name="value">The value to verify.</param>
		/// <param name="error">When this method returns <see langword="false"/>, contains the reason why the value is not acceptable.</param>
		/// <returns><see langword="true"/> when the value is acceptable; otherwise, <see langword="false"/>.</returns>
		public virtual bool IsValidValue(object value, out string error)
		{
			if (value == null)
			{
				if (IsRequired)
				{
					error = $"'{Name}' requires a value.";
					return false;
				}

				error = null;
				return true;
			}

			error = null;
			return true;
		}
	}
}
