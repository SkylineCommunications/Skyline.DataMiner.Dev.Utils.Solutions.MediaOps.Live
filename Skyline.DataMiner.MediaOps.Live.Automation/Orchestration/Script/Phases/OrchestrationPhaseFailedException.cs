namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;

	/// <summary>
	/// Thrown when a phase of an orchestration script failed. Its message is the reason meant for the operator.
	/// </summary>
	internal sealed class OrchestrationPhaseFailedException : Exception
	{
		internal OrchestrationPhaseFailedException(string message) : base(message)
		{
		}
	}
}
