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
	}
}
