namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;
	using System.Collections.Generic;

	public class ServiceProvider
	{
		public static readonly ServiceProvider Instance = new();

		private readonly object _lock = new();
		private readonly Dictionary<Type, object> _services = new();

		public T GetService<T>() where T : class
		{
			lock (_lock)
			{
				if (_services.TryGetValue(typeof(T), out var service))
				{
					return (T)service;
				}

				return null;
			}
		}

		public bool TryGetService<T>(out T service) where T : class
		{
			lock (_lock)
			{
				if (_services.TryGetValue(typeof(T), out var obj))
				{
					service = (T)obj;
					return true;
				}

				service = null;
				return false;
			}
		}

		public T GetOrAddService<T>(Func<T> factory) where T : class
		{
			if (factory == null)
			{
				throw new ArgumentNullException(nameof(factory));
			}

			lock (_lock)
			{
				if (!_services.TryGetValue(typeof(T), out var service))
				{
					service = factory();
					_services[typeof(T)] = service;
				}

				return (T)service;
			}
		}

		public bool ContainsService<T>() where T : class
		{
			lock (_lock)
			{
				return _services.ContainsKey(typeof(T));
			}
		}

		public void RegisterService<T>(T service) where T : class
		{
			if (service == null)
			{
				throw new ArgumentNullException(nameof(service));
			}

			lock (_lock)
			{
				if (_services.ContainsKey(typeof(T)))
				{
					throw new InvalidOperationException($"Service of type {typeof(T)} is already registered.");
				}

				_services[typeof(T)] = service;
			}
		}

		public bool TryRegisterService<T>(T service) where T : class
		{
			if (service == null)
			{
				throw new ArgumentNullException(nameof(service));
			}

			lock (_lock)
			{
				if (!_services.ContainsKey(typeof(T)))
				{
					_services.Add(typeof(T), service);
					return true;
				}

				return false;
			}
		}

		public bool TryRemoveService<T>() where T : class
		{
			lock (_lock)
			{
				return _services.Remove(typeof(T));
			}
		}

		public void Clear()
		{
			lock (_lock)
			{
				_services.Clear();
			}
		}
	}
}
