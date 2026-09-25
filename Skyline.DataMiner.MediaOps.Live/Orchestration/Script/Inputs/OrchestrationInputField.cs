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
		/// Gets or sets a value indicating whether the operator cannot edit this field.
		/// </summary>
		[JsonProperty("isDisabled")]
		public bool IsDisabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the script considers the current value of this field valid.
		/// Use this for checks the field definition cannot express, and explain the problem in <see cref="ValidationMessage"/>.
		/// </summary>
		[JsonProperty("isValid")]
		public bool IsValid { get; set; } = true;

		/// <summary>
		/// Gets or sets the message the script shows next to this field, typically why the current value is not valid.
		/// </summary>
		[JsonProperty("validationMessage", NullValueHandling = NullValueHandling.Ignore)]
		public string ValidationMessage { get; set; }

		/// <summary>
		/// Gets or sets the value of this field.
		/// </summary>
		[JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
		public OrchestrationInputValue Value { get; set; }

		/// <summary>
		/// Gets or sets the value that is used when the operator did not provide one.
		/// </summary>
		[JsonProperty("defaultValue", NullValueHandling = NullValueHandling.Ignore)]
		public OrchestrationInputValue DefaultValue { get; set; }

		/// <summary>
		/// Gets the value of this field, falling back to the default value when no value is set.
		/// </summary>
		/// <returns>The effective value of this field.</returns>
		public OrchestrationInputValue GetEffectiveValue()
		{
			return Value ?? DefaultValue;
		}

		/// <summary>
		/// Determines whether the specified value is acceptable for this field.
		/// </summary>
		/// <param name="value">The value to verify.</param>
		/// <param name="error">When this method returns <see langword="false"/>, contains the reason why the value is not acceptable.</param>
		/// <returns><see langword="true"/> when the value is acceptable; otherwise, <see langword="false"/>.</returns>
		public virtual bool IsValidValue(OrchestrationInputValue value, out string error)
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

		/// <summary>
		/// Formats the specified value the way it is shown to the operator.
		/// </summary>
		/// <param name="value">The value to format.</param>
		/// <returns>The text to show, or <see langword="null"/> when <paramref name="value"/> is <see langword="null"/>.</returns>
		public virtual string FormatValue(OrchestrationInputValue value)
		{
			return value?.ToString();
		}

		/// <summary>
		/// Determines whether the effective value of this field is acceptable, both for the field definition and for the script.
		/// </summary>
		/// <param name="error">When this method returns <see langword="false"/>, contains the reason why the value is not acceptable.</param>
		/// <returns><see langword="true"/> when the value is acceptable; otherwise, <see langword="false"/>.</returns>
		public bool TryValidate(out string error)
		{
			if (!IsValidValue(GetEffectiveValue(), out error))
			{
				return false;
			}

			if (!IsValid)
			{
				error = String.IsNullOrWhiteSpace(ValidationMessage) ? $"'{Name}' is not valid." : ValidationMessage;
				return false;
			}

			return true;
		}

		/// <summary>
		/// Verifies that this field can hold at least one value and that its default value fits the definition.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when the definition of this field is not usable.</exception>
		internal virtual void ValidateDefinition()
		{
			if (DefaultValue != null && !IsValidValue(DefaultValue, out var error))
			{
				throw new InvalidOperationException($"The default value of orchestration input '{Path}' is not valid. {error}");
			}
		}

		private protected static void ThrowUnusableDefinition(string path, string reason)
		{
			throw new InvalidOperationException($"Orchestration input '{path}' has no usable definition: {reason}");
		}
	}
}
