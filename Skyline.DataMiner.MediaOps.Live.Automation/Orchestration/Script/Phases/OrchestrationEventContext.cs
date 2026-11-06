namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

	/// <summary>
	/// The definition of the event the orchestration runs for: its nodes and the connections between them.
	/// </summary>
	public sealed class OrchestrationEventContext
	{
		internal OrchestrationEventContext(
			Guid id,
			string name,
			EventType type,
			IEnumerable<OrchestrationEventNode> nodes,
			IEnumerable<OrchestrationEventConnection> connections)
		{
			Id = id;
			Name = name;
			Type = type;
			Nodes = (nodes ?? Enumerable.Empty<OrchestrationEventNode>()).ToList();
			Connections = (connections ?? Enumerable.Empty<OrchestrationEventConnection>()).ToList();
		}

		/// <summary>
		/// Gets the ID of the event.
		/// </summary>
		public Guid Id { get; }

		/// <summary>
		/// Gets the name of the event.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the type of the event.
		/// </summary>
		public EventType Type { get; }

		/// <summary>
		/// Gets the nodes of the event.
		/// </summary>
		public IReadOnlyList<OrchestrationEventNode> Nodes { get; }

		/// <summary>
		/// Gets the connections between the nodes of the event.
		/// </summary>
		public IReadOnlyList<OrchestrationEventConnection> Connections { get; }

		internal static OrchestrationEventContext From(OrchestrationEventConfiguration orchestrationEvent)
		{
			if (orchestrationEvent == null)
			{
				return null;
			}

			return new OrchestrationEventContext(
				orchestrationEvent.ID,
				orchestrationEvent.Name,
				orchestrationEvent.EventType,
				orchestrationEvent.Configuration?.NodeConfigurations.Select(x => new OrchestrationEventNode(x.NodeId, x.NodeLabel, x.OrchestrationScriptName)),
				orchestrationEvent.Configuration?.Connections.Select(x => new OrchestrationEventConnection(x.SourceNodeId, x.DestinationNodeId)));
		}
	}
}
