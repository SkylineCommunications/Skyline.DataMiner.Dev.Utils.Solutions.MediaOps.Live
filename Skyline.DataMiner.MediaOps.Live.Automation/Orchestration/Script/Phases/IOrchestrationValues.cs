namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System.Collections.Generic;

	/// <summary>
	/// Read access to plain text values stored by orchestration phases.
	/// </summary>
	public interface IOrchestrationValues
	{
		/// <summary>
		/// Gets the keys that hold a value.
		/// </summary>
		IReadOnlyCollection<string> Keys { get; }

		/// <summary>
		/// Gets the value of the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>The value.</returns>
		/// <exception cref="KeyNotFoundException">No value is stored for the key.</exception>
		string Get(string key);

		/// <summary>
		/// Attempts to get the value of the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">When this method returns <see langword="true"/>, contains the value.</param>
		/// <returns><see langword="true"/> when a value is stored for the key.</returns>
		bool TryGet(string key, out string value);
	}
}
