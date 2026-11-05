namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;

	/// <summary>
	/// A phase that performs work, identified by a key that is unique within the orchestration script.
	/// </summary>
	public sealed class ExecutionPhase : OrchestrationPhase
	{
		internal ExecutionPhase(string key, Func<OrchestrationPhaseContext, PhaseStatus> execute)
		{
			if (String.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("The key of a phase cannot be empty.", nameof(key));
			}

			Key = key;
			ExecuteMethod = execute ?? throw new ArgumentNullException(nameof(execute));
		}

		/// <summary>
		/// Gets the key of the phase.
		/// </summary>
		public string Key { get; }

		internal Func<OrchestrationPhaseContext, PhaseStatus> ExecuteMethod { get; }

		internal Func<OrchestrationPhaseContext, PhaseStatus> StatusMethod { get; private set; }

		/// <summary>
		/// Declares the method that reports the actual state of the phase. It is called before the phase is executed,
		/// on every trigger and retrigger of the event, and must be cheap and free of side effects.
		/// Without a status method, a phase is executed unless it was recorded as completed.
		/// </summary>
		/// <param name="status">The status method.</param>
		/// <returns>This phase.</returns>
		public ExecutionPhase WithStatus(Func<OrchestrationPhaseContext, PhaseStatus> status)
		{
			StatusMethod = status ?? throw new ArgumentNullException(nameof(status));
			return this;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return $"Phase '{Key}'";
		}
	}
}
