namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script
{
	using System;

	/// <summary>
	/// Thrown by <see cref="OrchestrationScriptBase.OrchestrateNode"/> when the orchestration of a node failed.
	/// </summary>
	public sealed class OrchestrationNodeFailedException : Exception
	{
		internal OrchestrationNodeFailedException(string nodeId, string nodeDescription, string failureReason)
			: base($"Node {nodeDescription} failed: {failureReason}")
		{
			NodeId = nodeId;
			FailureReason = failureReason;
		}

		/// <summary>
		/// Gets the ID of the node that failed.
		/// </summary>
		public string NodeId { get; }

		/// <summary>
		/// Gets the reason why the node failed.
		/// </summary>
		public string FailureReason { get; }
	}
}
