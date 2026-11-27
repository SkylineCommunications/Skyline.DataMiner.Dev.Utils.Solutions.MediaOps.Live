namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases.Values
{
	using System;
	using System.Collections.Generic;

	internal sealed class UnavailableValueBackend : IValueBackend
	{
		private readonly string _reason;

		internal UnavailableValueBackend(string reason)
		{
			_reason = reason;
		}

		public IReadOnlyDictionary<string, string> ReadAll() => throw new InvalidOperationException(_reason);

		public bool TryGet(string key, out string value) => throw new InvalidOperationException(_reason);

		public void Set(string key, string value) => throw new InvalidOperationException(_reason);

		public bool Remove(string key) => throw new InvalidOperationException(_reason);
	}
}
