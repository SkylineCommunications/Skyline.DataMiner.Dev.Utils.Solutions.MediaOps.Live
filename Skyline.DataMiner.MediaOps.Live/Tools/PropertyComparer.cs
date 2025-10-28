namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Provides equality comparison based on a selected property of type T.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	/// <typeparam name="TKey">The type of the property used for comparison.</typeparam>
	public class PropertyComparer<T, TKey> : IEqualityComparer<T>
	{
		private readonly Func<T, TKey> _keySelector;
		private readonly IEqualityComparer<TKey> _keyComparer;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyComparer{T, TKey}"/> class.
		/// </summary>
		/// <param name="keySelector">The function to select the property for comparison.</param>
		/// <param name="keyComparer">Optional comparer for the selected property. If not specified, the default comparer is used.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="keySelector"/> is null.</exception>
		public PropertyComparer(Func<T, TKey> keySelector, IEqualityComparer<TKey> keyComparer = null)
		{
			_keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
			_keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
		}

		/// <summary>
		/// Determines whether the specified objects are equal based on their selected property.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns><c>true</c> if the specified objects are equal; otherwise, <c>false</c>.</returns>
		public bool Equals(T x, T y)
		{
			if (ReferenceEquals(x, y))
				return true;

			if (x == null || y == null)
				return false;

			return _keyComparer.Equals(_keySelector(x), _keySelector(y));
		}

		/// <summary>
		/// Returns a hash code for the specified object based on its selected property.
		/// </summary>
		/// <param name="obj">The object for which to get a hash code.</param>
		/// <returns>A hash code for the specified object.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
		public int GetHashCode(T obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			return _keyComparer.GetHashCode(_keySelector(obj));
		}
	}

	/// <summary>
	/// Provides a factory for creating property comparers.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	public static class PropertyComparer<T>
	{
		/// <summary>
		/// Creates a property comparer for the specified property selector.
		/// </summary>
		/// <typeparam name="TKey">The type of the property used for comparison.</typeparam>
		/// <param name="keySelector">The function to select the property for comparison.</param>
		/// <returns>A new <see cref="PropertyComparer{T, TKey}"/> instance.</returns>
		public static PropertyComparer<T, TKey> Create<TKey>(Func<T, TKey> keySelector) => new(keySelector);
	}
}
