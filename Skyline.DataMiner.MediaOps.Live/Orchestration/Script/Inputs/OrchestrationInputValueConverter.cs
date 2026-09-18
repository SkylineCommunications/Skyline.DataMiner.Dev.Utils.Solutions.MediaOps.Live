namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;
	using System.Globalization;

	/// <summary>
	/// Converts orchestration input values between the loosely typed representation that survives serialization and the types a script expects.
	/// </summary>
	public static class OrchestrationInputValueConverter
	{
		/// <summary>
		/// Converts the specified value to its string representation using the invariant culture.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The string representation, or <see langword="null"/> when the value is <see langword="null"/>.</returns>
		public static string ToStringValue(object value)
		{
			if (value == null)
			{
				return null;
			}

			if (value is string text)
			{
				return text;
			}

			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Attempts to convert the specified value to a double.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="result">When this method returns <see langword="true"/>, contains the converted value.</param>
		/// <returns><see langword="true"/> when the value could be converted; otherwise, <see langword="false"/>.</returns>
		public static bool TryToDouble(object value, out double result)
		{
			switch (value)
			{
				case null:
					result = default;
					return false;

				case double doubleValue:
					result = doubleValue;
					return true;

				case float floatValue:
					result = floatValue;
					return true;

				case decimal decimalValue:
					result = Convert.ToDouble(decimalValue);
					return true;

				case int intValue:
					result = intValue;
					return true;

				case long longValue:
					result = longValue;
					return true;

				case string stringValue:
					return Double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

				default:
					return Double.TryParse(ToStringValue(value), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
			}
		}

		/// <summary>
		/// Attempts to convert the specified value to a 32-bit integer.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="result">When this method returns <see langword="true"/>, contains the converted value.</param>
		/// <returns><see langword="true"/> when the value could be converted; otherwise, <see langword="false"/>.</returns>
		public static bool TryToInt32(object value, out int result)
		{
			if (!TryToDouble(value, out var doubleValue) || Double.IsNaN(doubleValue) || Double.IsInfinity(doubleValue))
			{
				result = default;
				return false;
			}

			var rounded = Math.Round(doubleValue, MidpointRounding.AwayFromZero);

			if (rounded < Int32.MinValue || rounded > Int32.MaxValue)
			{
				result = default;
				return false;
			}

			result = (int)rounded;
			return true;
		}

		/// <summary>
		/// Determines whether two orchestration input values represent the same value.
		/// Numeric values are compared numerically so that a value keeps matching after a serialization round trip.
		/// </summary>
		/// <param name="left">The first value.</param>
		/// <param name="right">The second value.</param>
		/// <returns><see langword="true"/> when both values are equal; otherwise, <see langword="false"/>.</returns>
		public static bool AreEqual(object left, object right)
		{
			if (left == null || right == null)
			{
				return left == null && right == null;
			}

			if (TryToDouble(left, out var leftNumber) && TryToDouble(right, out var rightNumber))
			{
				return Math.Abs(leftNumber - rightNumber) < Double.Epsilon;
			}

			return String.Equals(ToStringValue(left), ToStringValue(right), StringComparison.Ordinal);
		}
	}
}
