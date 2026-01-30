namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tools
{
	using System;

	public class ExpiringCache<T>
	{
		private readonly object _lock = new();
		private readonly TimeSpan _expiration;

		private T _cachedValue;
		private DateTime _expiryTime = DateTime.MinValue;

		public ExpiringCache(TimeSpan expiration)
		{
			_expiration = expiration;
		}

		/// <summary>
		/// Returns the cached value if it has not expired; otherwise, uses the provided factory to refresh and cache a new value.
		/// </summary>
		/// <param name="valueFactory">Factory method to produce a new value if the cache is expired.</param>
		/// <returns>The cached or newly computed value.</returns>
		public T GetOrRefresh(Func<T> valueFactory)
		{
			if (valueFactory == null)
			{
				throw new ArgumentNullException(nameof(valueFactory));
			}

			lock (_lock)
			{
				if (DateTime.UtcNow < _expiryTime)
				{
					return _cachedValue;
				}

				_cachedValue = valueFactory();
				_expiryTime = DateTime.UtcNow.Add(_expiration);
				return _cachedValue;
			}
		}

		/// <summary>
		/// Sets the cached value manually and resets the expiration timer.
		/// </summary>
		/// <param name="value">The new value to cache.</param>
		public void SetValue(T value)
		{
			lock (_lock)
			{
				_cachedValue = value;
				_expiryTime = DateTime.UtcNow.Add(_expiration);
			}
		}
	}
}
