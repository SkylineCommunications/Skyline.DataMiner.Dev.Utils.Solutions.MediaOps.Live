namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases.Values
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;

	internal sealed class OrchestrationValues : IOrchestrationWritableValues
	{
		private readonly IValueBackend _backend;

		internal OrchestrationValues(IValueBackend backend)
		{
			_backend = backend ?? throw new ArgumentNullException(nameof(backend));
		}

		public IReadOnlyCollection<string> Keys => _backend.ReadAll().Keys.ToList();

		public string Get(string key)
		{
			if (!TryGet(key, out string value))
			{
				throw new KeyNotFoundException($"No value is stored for key '{key}'.");
			}

			return value;
		}

		public bool TryGet(string key, out string value)
		{
			ValidateKey(key);
			return _backend.TryGet(key, out value);
		}

		public void Set(string key, string value)
		{
			ValidateKey(key);

			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			// Keys and values are never trimmed: a truncated identifier is worse than a failure.
			if (key.Length > NodeContextValueRepository.MaxKeyLength)
			{
				throw new ArgumentException($"Key '{key}' exceeds {NodeContextValueRepository.MaxKeyLength} characters.", nameof(key));
			}

			if (value.Length > NodeContextValueRepository.MaxValueLength)
			{
				throw new ArgumentException($"Value of key '{key}' exceeds {NodeContextValueRepository.MaxValueLength} characters.", nameof(value));
			}

			_backend.Set(key, value);
		}

		public bool Remove(string key)
		{
			ValidateKey(key);
			return _backend.Remove(key);
		}

		private static void ValidateKey(string key)
		{
			if (String.IsNullOrEmpty(key))
			{
				throw new ArgumentException("The key cannot be null or empty.", nameof(key));
			}
		}
	}
}
