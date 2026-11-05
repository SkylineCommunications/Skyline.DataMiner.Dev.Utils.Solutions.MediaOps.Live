namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Keeps the phase statuses for the duration of a single execution only, for scripts that do not run for a node of an event.
	/// </summary>
	internal sealed class InMemoryPhaseStatusStore : IPhaseStatusStore
	{
		private readonly Dictionary<string, PhaseStatus> _statuses;

		internal InMemoryPhaseStatusStore(IReadOnlyDictionary<string, PhaseStatus> initialStatuses = null)
		{
			_statuses = new Dictionary<string, PhaseStatus>(StringComparer.Ordinal);

			if (initialStatuses != null)
			{
				foreach (KeyValuePair<string, PhaseStatus> status in initialStatuses)
				{
					_statuses[status.Key] = status.Value;
				}
			}
		}

		public IReadOnlyDictionary<string, PhaseStatus> Load()
		{
			return new Dictionary<string, PhaseStatus>(_statuses, StringComparer.Ordinal);
		}

		public void Record(string key, PhaseStatus status, DateTimeOffset? executionStarted)
		{
			_statuses[key] = status;
		}
	}
}
