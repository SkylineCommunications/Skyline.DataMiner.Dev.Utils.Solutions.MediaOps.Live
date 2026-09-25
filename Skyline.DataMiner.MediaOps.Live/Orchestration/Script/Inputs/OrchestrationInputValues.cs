namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The orchestration input values that were already provided, keyed by the path of the field they belong to.
	/// These are handed to the orchestration script so it can decide which input items to expose next.
	/// </summary>
	public class OrchestrationInputValues
	{
		private readonly Dictionary<string, OrchestrationInputValue> _values;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationInputValues"/> class without any value.
		/// </summary>
		public OrchestrationInputValues() : this(null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationInputValues"/> class.
		/// </summary>
		/// <param name="values">The already provided values, keyed by field path. Entries without a value are skipped.</param>
		public OrchestrationInputValues(IDictionary<string, OrchestrationInputValue> values)
		{
			_values = new Dictionary<string, OrchestrationInputValue>(StringComparer.OrdinalIgnoreCase);

			if (values == null)
			{
				return;
			}

			foreach (var value in values)
			{
				if (!String.IsNullOrEmpty(value.Key) && value.Value != null)
				{
					_values[value.Key] = value.Value;
				}
			}
		}

		/// <summary>
		/// Gets an empty set of values.
		/// </summary>
		public static OrchestrationInputValues Empty { get; } = new OrchestrationInputValues();

		/// <summary>
		/// Gets the number of provided values.
		/// </summary>
		public int Count => _values.Count;

		/// <summary>
		/// Determines whether a value was provided for the specified path.
		/// </summary>
		/// <param name="path">The path of the field.</param>
		/// <returns><see langword="true"/> when a value was provided; otherwise, <see langword="false"/>.</returns>
		public bool Contains(string path)
		{
			return !String.IsNullOrEmpty(path) && _values.ContainsKey(path);
		}

		/// <summary>
		/// Attempts to get the value that was provided for the specified path.
		/// </summary>
		/// <param name="path">The path of the field.</param>
		/// <param name="value">When this method returns <see langword="true"/>, contains the provided value.</param>
		/// <returns><see langword="true"/> when a value was provided; otherwise, <see langword="false"/>.</returns>
		public bool TryGetValue(string path, out OrchestrationInputValue value)
		{
			if (String.IsNullOrEmpty(path))
			{
				value = null;
				return false;
			}

			return _values.TryGetValue(path, out value);
		}

		/// <summary>
		/// Gets the provided value for the specified path as text.
		/// </summary>
		/// <param name="path">The path of the field.</param>
		/// <returns>The value as text, or <see langword="null"/> when no value was provided.</returns>
		public string GetString(string path)
		{
			return TryGetValue(path, out var value) ? value.ToString() : null;
		}

		/// <summary>
		/// Gets the provided value for the specified path as a whole number.
		/// </summary>
		/// <param name="path">The path of the field.</param>
		/// <returns>The value as a whole number, or <see langword="null"/> when no numeric value was provided.</returns>
		public int? GetInt32(string path)
		{
			return TryGetValue(path, out var value) && value.TryGetInt32(out var result)
				? result
				: (int?)null;
		}

		/// <summary>
		/// Gets the provided value for the specified path as a number.
		/// </summary>
		/// <param name="path">The path of the field.</param>
		/// <returns>The value as a number, or <see langword="null"/> when no numeric value was provided.</returns>
		public double? GetNumber(string path)
		{
			return TryGetValue(path, out var value) && value.TryGetNumber(out var result)
				? result
				: (double?)null;
		}

		/// <summary>
		/// Gets the provided value for the specified path as a date and time in UTC.
		/// </summary>
		/// <param name="path">The path of the field.</param>
		/// <returns>The value as a date and time in UTC, or <see langword="null"/> when no date and time was provided.</returns>
		public DateTime? GetDateTime(string path)
		{
			return TryGetValue(path, out var value) && value.TryGetDateTime(out var result)
				? result
				: (DateTime?)null;
		}

		/// <summary>
		/// Gets the provided value for the specified path as a duration.
		/// </summary>
		/// <param name="path">The path of the field.</param>
		/// <returns>The value as a duration, or <see langword="null"/> when no duration was provided.</returns>
		public TimeSpan? GetTimeSpan(string path)
		{
			return TryGetValue(path, out var value) && value.TryGetTimeSpan(out var result)
				? result
				: (TimeSpan?)null;
		}

		/// <summary>
		/// Gets the provided value for the specified path as a whole number, clamped to the specified bounds.
		/// This is the typical way to translate a value into the number of repetitions of a repeated group.
		/// </summary>
		/// <param name="path">The path of the field.</param>
		/// <param name="minimum">The lowest value to return.</param>
		/// <param name="maximum">The highest value to return.</param>
		/// <returns>The clamped value, or <paramref name="minimum"/> when no numeric value was provided.</returns>
		public int GetInt32(string path, int minimum, int maximum)
		{
			if (minimum > maximum)
			{
				throw new ArgumentOutOfRangeException(nameof(minimum), "The minimum cannot be larger than the maximum.");
			}

			var value = GetInt32(path) ?? minimum;

			return Math.Min(Math.Max(value, minimum), maximum);
		}

		/// <summary>
		/// Determines whether the value that was provided for the specified path equals the expected value.
		/// </summary>
		/// <param name="path">The path of the field.</param>
		/// <param name="expectedValue">The value to compare against.</param>
		/// <returns><see langword="true"/> when both values are equal; otherwise, <see langword="false"/>.</returns>
		public bool HasValue(string path, OrchestrationInputValue expectedValue)
		{
			return TryGetValue(path, out var value) && value == expectedValue;
		}

		/// <summary>
		/// Determines whether this set holds exactly the same values as the specified set.
		/// </summary>
		/// <param name="other">The set to compare against.</param>
		/// <returns><see langword="true"/> when both sets hold the same values; otherwise, <see langword="false"/>.</returns>
		public bool HasSameValues(OrchestrationInputValues other)
		{
			if (other == null || other.Count != Count)
			{
				return false;
			}

			foreach (var value in _values)
			{
				if (!other.TryGetValue(value.Key, out var otherValue) || value.Value != otherValue)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Gets the provided values as a dictionary.
		/// </summary>
		/// <returns>A copy of the provided values.</returns>
		public Dictionary<string, OrchestrationInputValue> ToDictionary()
		{
			return new Dictionary<string, OrchestrationInputValue>(_values, StringComparer.OrdinalIgnoreCase);
		}
	}
}
