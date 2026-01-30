namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tools
{
	using System;
	using System.Collections.Generic;

	public class PropertyComparer<T, TKey> : IEqualityComparer<T>
	{
		private readonly Func<T, TKey> _keySelector;
		private readonly IEqualityComparer<TKey> _keyComparer;

		public PropertyComparer(Func<T, TKey> keySelector, IEqualityComparer<TKey> keyComparer = null)
		{
			_keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
			_keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
		}

		public bool Equals(T x, T y)
		{
			if (ReferenceEquals(x, y))
				return true;

			if (x == null || y == null)
				return false;

			return _keyComparer.Equals(_keySelector(x), _keySelector(y));
		}

		public int GetHashCode(T obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			return _keyComparer.GetHashCode(_keySelector(obj));
		}
	}

	public static class PropertyComparer<T>
	{
		public static PropertyComparer<T, TKey> Create<TKey>(Func<T, TKey> keySelector) => new(keySelector);
	}
}
