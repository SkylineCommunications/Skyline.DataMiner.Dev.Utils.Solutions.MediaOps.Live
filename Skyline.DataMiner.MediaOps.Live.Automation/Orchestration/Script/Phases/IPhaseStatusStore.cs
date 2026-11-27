namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Keeps the recorded phase statuses of one node for one event.
	/// </summary>
	internal interface IPhaseStatusStore
	{
		IReadOnlyDictionary<string, PhaseStatus> Load();

		/// <param name="key">The key of the phase.</param>
		/// <param name="status">The new status of the phase.</param>
		/// <param name="executionStarted">The moment the phase was executed, or <see langword="null"/> when it was only evaluated.</param>
		void Record(string key, PhaseStatus status, DateTimeOffset? executionStarted);
	}
}
