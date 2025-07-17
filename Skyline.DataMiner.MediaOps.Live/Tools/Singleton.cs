namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;

	public static class Singleton<T>
	{
		private static readonly object _lock = new();

		private static T _value;
		private static volatile bool _initialized;

		public static bool IsInitialized
		{
			get
			{
				lock (_lock)
				{
					return _initialized;
				}
			}
		}

		public static T GetOrInitialize(Func<T> factory)
		{
			if (!_initialized)
			{
				lock (_lock)
				{
					if (!_initialized)
					{
						_value = factory();
						_initialized = true;
					}
				}
			}

			return _value;
		}

		public static void SetValue(T value)
		{
			lock (_lock)
			{
				_value = value;
				_initialized = true;
			}
		}

		public static void Reset()
		{
			lock (_lock)
			{
				_initialized = false;
				_value = default!;
			}
		}
	}
}
