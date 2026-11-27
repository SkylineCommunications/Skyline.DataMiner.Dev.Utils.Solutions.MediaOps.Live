namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	/// <summary>
	/// Read and write access to plain text values stored by orchestration phases.
	/// Values are stored as plain text and are not meant for secrets.
	/// A key is at most 128 characters, a value at most 4,096 characters, and at most 50 keys are stored.
	/// </summary>
	public interface IOrchestrationWritableValues : IOrchestrationValues
	{
		/// <summary>
		/// Stores the value of the specified key, replacing any previous value.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		void Set(string key, string value);

		/// <summary>
		/// Removes the value of the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns><see langword="true"/> when a value was removed.</returns>
		bool Remove(string key);
	}
}
