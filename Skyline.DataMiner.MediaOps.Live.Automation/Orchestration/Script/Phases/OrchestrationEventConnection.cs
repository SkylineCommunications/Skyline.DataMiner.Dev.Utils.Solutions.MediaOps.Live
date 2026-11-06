namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	/// <summary>
	/// A connection between two nodes of the event the orchestration runs for.
	/// </summary>
	public sealed class OrchestrationEventConnection
	{
		internal OrchestrationEventConnection(string sourceNodeId, string destinationNodeId)
		{
			SourceNodeId = sourceNodeId;
			DestinationNodeId = destinationNodeId;
		}

		/// <summary>
		/// Gets the ID of the source node, or <see langword="null"/> when the connection has no source.
		/// </summary>
		public string SourceNodeId { get; }

		/// <summary>
		/// Gets the ID of the destination node, or <see langword="null"/> when the connection has no destination.
		/// </summary>
		public string DestinationNodeId { get; }
	}
}
