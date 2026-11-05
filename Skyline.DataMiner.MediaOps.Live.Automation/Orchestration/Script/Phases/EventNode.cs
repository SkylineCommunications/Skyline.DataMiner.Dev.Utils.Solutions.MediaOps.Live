namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	internal sealed class EventNode
	{
		internal EventNode(string nodeId, string label, bool isCurrentNode)
		{
			NodeId = nodeId;
			Label = label;
			IsCurrentNode = isCurrentNode;
		}

		internal string NodeId { get; }

		internal string Label { get; }

		internal bool IsCurrentNode { get; }

		/// <summary>
		/// Gets the name used in messages for the operator.
		/// </summary>
		internal string DisplayName => string.IsNullOrWhiteSpace(Label) ? NodeId : Label;
	}
}
