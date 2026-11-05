namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;

	/// <summary>
	/// The nodes of an event, with the phase statuses they stored for that event.
	/// </summary>
	internal sealed class StoredEventNodes : IEventNodes
	{
		private readonly NodePhasesRepository _repository;
		private readonly Guid _eventId;
		private readonly string _currentNodeId;
		private readonly IReadOnlyCollection<NodeConfiguration> _nodes;

		internal StoredEventNodes(NodePhasesRepository repository, Guid eventId, string currentNodeId, IEnumerable<NodeConfiguration> nodes)
		{
			_repository = repository ?? throw new ArgumentNullException(nameof(repository));
			_eventId = eventId;
			_currentNodeId = currentNodeId;
			_nodes = nodes?.ToList() ?? new List<NodeConfiguration>();
		}

		public bool TryResolveNode(string reference, out EventNode node, out string failure)
		{
			NodeConfiguration match = _nodes.FirstOrDefault(x => String.Equals(x.NodeId, reference, StringComparison.Ordinal));

			if (match == null)
			{
				List<NodeConfiguration> byLabel = _nodes.Where(x => String.Equals(x.NodeLabel, reference, StringComparison.Ordinal)).ToList();

				if (byLabel.Count > 1)
				{
					node = null;
					failure = $"more than one node of the event has label '{reference}'";
					return false;
				}

				match = byLabel.FirstOrDefault();
			}

			if (match == null)
			{
				node = null;
				failure = $"node '{reference}' is not part of the event";
				return false;
			}

			node = new EventNode(match.NodeId, match.NodeLabel, String.Equals(match.NodeId, _currentNodeId, StringComparison.Ordinal));
			failure = null;
			return true;
		}

		public PhaseStatus GetStoredStatus(string nodeId, string phaseKey)
		{
			OrchestrationPhaseStatus stored = _repository.GetNodePhases(_eventId, nodeId)?.Phases
				.FirstOrDefault(x => String.Equals(x.Key, phaseKey, StringComparison.Ordinal));

			return stored == null ? null : NodePhasesStatusStore.ToPhaseStatus(stored);
		}
	}
}
