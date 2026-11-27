namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script
{
	/// <summary>
	/// The outcome of orchestrating a node from a global orchestration script.
	/// </summary>
	public sealed class NodeOrchestrationResult
	{
		private NodeOrchestrationResult(string nodeId, bool isExecuted, string failureReason)
		{
			NodeId = nodeId;
			IsExecuted = isExecuted;
			FailureReason = failureReason;
		}

		/// <summary>
		/// Gets the ID of the node.
		/// </summary>
		public string NodeId { get; }

		/// <summary>
		/// Gets a value indicating whether the orchestration script of the node was run.
		/// It isn't run when the node has no orchestration script, or when the script doesn't run as the global script of an event.
		/// </summary>
		public bool IsExecuted { get; }

		/// <summary>
		/// Gets a value indicating whether the node did not fail.
		/// </summary>
		public bool IsSuccess => FailureReason == null;

		/// <summary>
		/// Gets the reason why the node failed, or <see langword="null"/> when it did not fail.
		/// </summary>
		public string FailureReason { get; }

		internal static NodeOrchestrationResult NotExecuted(string nodeId) => new NodeOrchestrationResult(nodeId, false, null);

		internal static NodeOrchestrationResult Succeeded(string nodeId) => new NodeOrchestrationResult(nodeId, true, null);

		internal static NodeOrchestrationResult Failed(string nodeId, string failureReason) => new NodeOrchestrationResult(nodeId, true, failureReason);
	}
}
