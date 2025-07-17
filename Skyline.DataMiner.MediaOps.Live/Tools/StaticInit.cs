namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;

	public class StaticInit<T>
	{
		private readonly object _lock = new object();

		private T _value;
		private bool _initialized;

		public bool IsInitialized
		{
			get
			{
				lock (_lock)
				{
					return _initialized;
				}
			}
		}

		public T GetOrInitialize(Func<T> factory)
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

		public void Initialize(T value)
		{
			lock (_lock)
			{
				if (_initialized)
				{
					throw new InvalidOperationException("Value has already been initialized.");
				}

				_value = value;
				_initialized = true;
			}
		}

		public void Reset()
		{
			lock (_lock)
			{
				_initialized = false;
				_value = default!;
			}
		}
	}
}
