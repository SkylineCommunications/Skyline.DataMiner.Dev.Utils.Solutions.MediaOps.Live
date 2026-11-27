namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Subscriptions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

	/// <summary>
	/// The phases of a node in an orchestration event that changed.
	/// </summary>
	public sealed class OrchestrationNodePhasesChange
	{
		internal OrchestrationNodePhasesChange(Guid eventId, string nodeId, IReadOnlyList<OrchestrationEventPhase> phases)
		{
			EventId = eventId;
			NodeId = nodeId;
			Phases = phases ?? Array.Empty<OrchestrationEventPhase>();
		}

		/// <summary>
		/// Gets the ID of the orchestration event.
		/// </summary>
		public Guid EventId { get; }

		/// <summary>
		/// Gets the ID of the node.
		/// </summary>
		public string NodeId { get; }

		/// <summary>
		/// Gets all current phases of the node, ordered as in its orchestration script. Phases that are no longer listed were removed.
		/// </summary>
		public IReadOnlyList<OrchestrationEventPhase> Phases { get; }

		/// <summary>
		/// Gets a value indicating whether the phases of the node were removed, for example because the event was deleted.
		/// </summary>
		public bool IsRemoved => Phases.Count == 0;

		/// <inheritdoc/>
		public override string ToString()
		{
			return $"{EventId}/{NodeId}: {Phases.Count} phases";
		}
	}
}
