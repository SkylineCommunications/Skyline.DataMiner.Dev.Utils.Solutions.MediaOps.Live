namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases.Values
{
	using System.Collections.Generic;

	/// <summary>
	/// Where a set of orchestration values is kept.
	/// </summary>
	internal interface IValueBackend
	{
		IReadOnlyDictionary<string, string> ReadAll();

		bool TryGet(string key, out string value);

		void Set(string key, string value);

		bool Remove(string key);
	}
}
