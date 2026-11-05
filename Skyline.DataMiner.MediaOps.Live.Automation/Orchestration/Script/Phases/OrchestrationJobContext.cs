namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;

	/// <summary>
	/// The job the orchestration runs for.
	/// </summary>
	public sealed class OrchestrationJobContext
	{
		internal OrchestrationJobContext(OrchestrationJobValues values)
		{
			Values = values ?? throw new ArgumentNullException(nameof(values));
		}

		/// <summary>
		/// Gets the job context values of the current node. They are kept for the lifetime of the job, so later events
		/// of the job can read them, for example the stop event reading what the start event provisioned.
		/// Only available when the script runs for a node of an event.
		/// </summary>
		public OrchestrationJobValues Values { get; }
	}
}
