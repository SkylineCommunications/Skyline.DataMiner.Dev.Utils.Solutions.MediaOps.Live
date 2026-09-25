namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;
	using System.Globalization;

	using Newtonsoft.Json;

	/// <summary>
	/// The value of an orchestration input field, which is either text or a number.
	/// A date and time is held as round-trip text in UTC and a duration as a number of seconds.
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

		/// <summary>
		/// Converts a date and time to an orchestration input value.
		/// </summary>
		/// <param name="dateTime">The date and time.</param>
		public static implicit operator OrchestrationInputValue(DateTime dateTime)
		{
			return FromDateTime(dateTime);
		}

		/// <summary>
		/// Converts a duration to an orchestration input value.
		/// </summary>
		/// <param name="timeSpan">The duration.</param>
		public static implicit operator OrchestrationInputValue(TimeSpan timeSpan)
		{
			return FromTimeSpan(timeSpan);
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
		/// Creates a date and time value. A value without a kind is taken as UTC.
		/// </summary>
		/// <param name="dateTime">The date and time.</param>
		/// <returns>The value, held as round-trip text in UTC.</returns>
		public static OrchestrationInputValue FromDateTime(DateTime dateTime)
		{
			return FromText(ToUniversal(dateTime).ToString("o", CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Creates a duration value.
		/// </summary>
		/// <param name="timeSpan">The duration.</param>
		/// <returns>The value, held as a number of seconds.</returns>
		public static OrchestrationInputValue FromTimeSpan(TimeSpan timeSpan)
		{
			return FromNumber(timeSpan.TotalSeconds);
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

		/// <summary>
		/// Attempts to get this value as a date and time in UTC.
		/// </summary>
		/// <param name="dateTime">When this method returns <see langword="true"/>, contains the date and time in UTC.</param>
		/// <returns><see langword="true"/> when this value represents a date and time; otherwise, <see langword="false"/>.</returns>
		public bool TryGetDateTime(out DateTime dateTime)
		{
			if (IsText && DateTime.TryParse(_text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateTime))
			{
				return true;
			}

			dateTime = default;
			return false;
		}

		/// <summary>
		/// Attempts to get this value as a duration. A number is taken as seconds; text is parsed in the constant ("c") format.
		/// </summary>
		/// <param name="timeSpan">When this method returns <see langword="true"/>, contains the duration.</param>
		/// <returns><see langword="true"/> when this value represents a duration; otherwise, <see langword="false"/>.</returns>
		public bool TryGetTimeSpan(out TimeSpan timeSpan)
		{
			if (IsNumber)
			{
				if (Double.IsNaN(_number) || Double.IsInfinity(_number) || Math.Abs(_number) > TimeSpan.MaxValue.TotalSeconds)
				{
					timeSpan = default;
					return false;
				}

				timeSpan = TimeSpan.FromTicks((long)Math.Round(_number * TimeSpan.TicksPerSecond));
				return true;
			}

			return TimeSpan.TryParseExact(_text, "c", CultureInfo.InvariantCulture, out timeSpan);
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

		internal static DateTime ToUniversal(DateTime dateTime)
		{
			return dateTime.Kind == DateTimeKind.Unspecified
				? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
				: dateTime.ToUniversalTime();
		}
	}
}
