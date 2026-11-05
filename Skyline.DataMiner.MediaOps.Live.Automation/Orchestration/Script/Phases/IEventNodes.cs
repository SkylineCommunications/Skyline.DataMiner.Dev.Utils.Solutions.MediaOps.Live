namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	/// <summary>
	/// Gives access to the nodes of the event and the phase statuses they stored.
	/// </summary>
	internal interface IEventNodes
	{
		/// <summary>
		/// Resolves a node by ID or label.
		/// </summary>
		/// <returns><see langword="false"/> when the node is not part of the event, or the label is ambiguous.</returns>
		bool TryResolveNode(string reference, out EventNode node, out string failure);

		/// <summary>
		/// Gets the status a node stored for one of its phases, or <see langword="null"/> when it stored none.
		/// </summary>
		PhaseStatus GetStoredStatus(string nodeId, string phaseKey);
	}
}
