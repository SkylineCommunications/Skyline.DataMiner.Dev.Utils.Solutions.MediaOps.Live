namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases.Values
{
	using System;
	using System.Collections.Generic;

	internal sealed class ReadOnlyOrchestrationValues : IOrchestrationValues
	{
		private readonly OrchestrationValues _values;

		internal ReadOnlyOrchestrationValues(IValueBackend backend)
		{
			_values = new OrchestrationValues(backend ?? throw new ArgumentNullException(nameof(backend)));
		}

		public IReadOnlyCollection<string> Keys => _values.Keys;

		public string Get(string key) => _values.Get(key);

		public bool TryGet(string key, out string value) => _values.TryGet(key, out value);
	}
}
