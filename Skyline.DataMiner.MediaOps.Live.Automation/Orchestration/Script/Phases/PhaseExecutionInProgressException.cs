namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;

	/// <summary>
	/// Thrown when another execution already runs the phases of the same node for the same event.
	/// </summary>
	internal sealed class PhaseExecutionInProgressException : Exception
	{
		internal PhaseExecutionInProgressException(string message) : base(message)
		{
		}
	}
}
