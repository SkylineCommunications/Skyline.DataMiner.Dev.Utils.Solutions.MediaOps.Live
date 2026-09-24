namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;
	using System.Globalization;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Net.Profiles;

	/// <summary>
	/// The value of an orchestration input field, which is either text or a number.
	/// </summary>
	[JsonConverter(typeof(OrchestrationInputValueJsonConverter))]
	public sealed class OrchestrationInputValue : IEquatable<OrchestrationInputValue>
	{
		private readonly string _text;
		private readonly double _number;

		private OrchestrationInputValue(string text)
		{
			Type = OrchestrationInputValueType.Text;
			_text = text;
		}

		private OrchestrationInputValue(double number)
		{
			Type = OrchestrationInputValueType.Number;
			_number = number;
		}

		/// <summary>
		/// Gets the type of this value.
		/// </summary>
		public OrchestrationInputValueType Type { get; }

		/// <summary>
		/// Gets a value indicating whether this value is text.
		/// </summary>
		public bool IsText => Type == OrchestrationInputValueType.Text;

		/// <summary>
		/// Gets a value indicating whether this value is a number.
		/// </summary>
		public bool IsNumber => Type == OrchestrationInputValueType.Number;

		/// <summary>
		/// Gets the text of this value.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when this value is not text.</exception>
		public string Text => IsText ? _text : throw new InvalidOperationException($"The orchestration input value '{this}' is not text.");

		/// <summary>
		/// Gets the number of this value.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when this value is not a number.</exception>
		public double Number => IsNumber ? _number : throw new InvalidOperationException($"The orchestration input value '{this}' is not a number.");

		/// <summary>
		/// Converts text to an orchestration input value.
		/// </summary>
		/// <param name="text">The text, or <see langword="null"/> for no value.</param>
		public static implicit operator OrchestrationInputValue(string text)
		{
			return text == null ? null : FromText(text);
		}

		/// <summary>
		/// Converts a number to an orchestration input value.
		/// </summary>
		/// <param name="number">The number.</param>
		public static implicit operator OrchestrationInputValue(double number)
		{
			return FromNumber(number);
		}

		public static bool operator ==(OrchestrationInputValue left, OrchestrationInputValue right)
		{
			return left is null ? right is null : left.Equals(right);
		}

		public static bool operator !=(OrchestrationInputValue left, OrchestrationInputValue right)
		{
			return !(left == right);
		}

		/// <summary>
		/// Creates a text value.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>The value.</returns>
		public static OrchestrationInputValue FromText(string text)
		{
			return new OrchestrationInputValue(text ?? throw new ArgumentNullException(nameof(text)));
		}

		/// <summary>
		/// Creates a numeric value.
		/// </summary>
		/// <param name="number">The number.</param>
		/// <returns>The value.</returns>
		public static OrchestrationInputValue FromNumber(double number)
		{
			return new OrchestrationInputValue(number);
		}

		/// <summary>
		/// Creates a value from a profile parameter value.
		/// </summary>
		/// <param name="value">The profile parameter value.</param>
		/// <returns>The value, or <see langword="null"/> when <paramref name="value"/> holds no value.</returns>
		/// <exception cref="NotSupportedException">Thrown when the profile parameter value is neither text nor a number.</exception>
		public static OrchestrationInputValue FromParameterValue(ParameterValue value)
		{
			if (value == null)
			{
				return null;
			}

			switch (value.Type)
			{
				case ParameterValue.ValueType.Double:
					return Double.IsNaN(value.DoubleValue) ? null : FromNumber(value.DoubleValue);

				case ParameterValue.ValueType.String:
					return value.StringValue == null ? null : FromText(value.StringValue);

				default:
					throw new NotSupportedException($"Profile parameter values of type {value.Type} are not supported as orchestration input values.");
			}
		}

		/// <summary>
		/// Converts this value to a profile parameter value.
		/// </summary>
		/// <returns>The profile parameter value.</returns>
		public ParameterValue ToParameterValue()
		{
			return IsNumber
				? new ParameterValue { Type = ParameterValue.ValueType.Double, DoubleValue = _number }
				: new ParameterValue { Type = ParameterValue.ValueType.String, StringValue = _text };
		}

		/// <summary>
		/// Attempts to get this value as a number. Text is parsed using the invariant culture.
		/// </summary>
		/// <param name="number">When this method returns <see langword="true"/>, contains the number.</param>
		/// <returns><see langword="true"/> when this value represents a number; otherwise, <see langword="false"/>.</returns>
		public bool TryGetNumber(out double number)
		{
			if (IsNumber)
			{
				number = _number;
				return true;
			}

			return Double.TryParse(_text, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
		}

		/// <summary>
		/// Attempts to get this value as a whole number, rounding half away from zero.
		/// </summary>
		/// <param name="number">When this method returns <see langword="true"/>, contains the whole number.</param>
		/// <returns><see langword="true"/> when this value represents a number that fits a 32-bit integer; otherwise, <see langword="false"/>.</returns>
		public bool TryGetInt32(out int number)
		{
			if (!TryGetNumber(out var value) || Double.IsNaN(value) || Double.IsInfinity(value))
			{
				number = default;
				return false;
			}

			var rounded = Math.Round(value, MidpointRounding.AwayFromZero);

			if (rounded < Int32.MinValue || rounded > Int32.MaxValue)
			{
				number = default;
				return false;
			}

			number = (int)rounded;
			return true;
		}

		/// <inheritdoc/>
		public bool Equals(OrchestrationInputValue other)
		{
			if (other is null || other.Type != Type)
			{
				return false;
			}

			return IsNumber
				? _number.Equals(other._number)
				: String.Equals(_text, other._text, StringComparison.Ordinal);
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return Equals(obj as OrchestrationInputValue);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return IsNumber ? _number.GetHashCode() : StringComparer.Ordinal.GetHashCode(_text);
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return IsNumber ? _number.ToString(CultureInfo.InvariantCulture) : _text;
		}
	}
}
