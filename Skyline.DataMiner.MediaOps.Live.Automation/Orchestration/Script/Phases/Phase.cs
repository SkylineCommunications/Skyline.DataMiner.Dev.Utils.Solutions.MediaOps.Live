namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;

	/// <summary>
	/// Creates the phases of an orchestration script.
	/// </summary>
	public static class Phase
	{
		/// <summary>
		/// Creates a phase that performs work.
		/// </summary>
		/// <param name="key">The key of the phase, unique within the orchestration script. Keep it stable: it identifies the phase across executions.</param>
		/// <param name="execute">The work of the phase, returning the status of the phase after the work.</param>
		/// <returns>The phase.</returns>
		public static ExecutionPhase Execute(string key, Func<OrchestrationPhaseContext, PhaseStatus> execute)
		{
			return new ExecutionPhase(key, execute);
		}

		/// <summary>
		/// Creates a phase that waits until a phase defined earlier in the same script reports <see cref="PhaseState.Completed"/>.
		/// The phase that is waited for must declare a status method.
		/// </summary>
		/// <param name="phaseKey">The key of the phase to wait for.</param>
		/// <returns>The phase.</returns>
		public static WaitPhase WaitFor(string phaseKey)
		{
			return new WaitPhase(phaseKey);
		}

		/// <summary>
		/// Creates a phase that waits until a phase of another node in the same event reports <see cref="PhaseState.Completed"/>.
		/// The status that node stored is used, so that node must keep evaluating the phase, for example with a wait of its own.
		/// A phase that never reported a status is treated as <see cref="PhaseState.NotStarted"/>.
		/// </summary>
		/// <param name="node">The ID or label of the node.</param>
		/// <param name="phase">The key of the phase of that node to wait for.</param>
		/// <returns>The phase.</returns>
		public static WaitPhase WaitFor(string node, string phase)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			return new WaitPhase(phase, node);
		}
	}
}
