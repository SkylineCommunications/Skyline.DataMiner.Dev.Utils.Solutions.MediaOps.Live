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
		private readonly Lazy<HashSet<string>> _jobNodeIds;

		/// <param name="loadJobNodes">Loads the nodes of all events of the job; only called for a node that is not part of this event.</param>
		internal StoredEventNodes(
			NodePhasesRepository repository,
			Guid eventId,
			string currentNodeId,
			IEnumerable<NodeConfiguration> nodes,
			Func<IEnumerable<NodeConfiguration>> loadJobNodes = null)
		{
			_repository = repository ?? throw new ArgumentNullException(nameof(repository));
			_eventId = eventId;
			_currentNodeId = currentNodeId;
			_nodes = nodes?.ToList() ?? new List<NodeConfiguration>();
			_jobNodeIds = new Lazy<HashSet<string>>(() => new HashSet<string>(
				(loadJobNodes?.Invoke() ?? Enumerable.Empty<NodeConfiguration>()).Select(x => x.NodeId).Where(x => x != null),
				StringComparer.Ordinal));
		}

		public bool TryResolveJobNode(string reference, out EventNode node, out string failure)
		{
			if (TryResolveNode(reference, out node, out failure))
			{
				return true;
			}

			// Nodes of other events are only addressed by ID: their labels can differ per event.
			if (!IsAmbiguousLabel(reference) && _jobNodeIds.Value.Contains(reference))
			{
				node = new EventNode(reference, null, String.Equals(reference, _currentNodeId, StringComparison.Ordinal));
				failure = null;
				return true;
			}

			if (!IsAmbiguousLabel(reference))
			{
				failure = $"node '{reference}' is not part of the job";
			}

			return false;
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
					failure = AmbiguousLabelFailure(reference);
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

		private static string AmbiguousLabelFailure(string label) => $"more than one node of the event has label '{label}'";

		private bool IsAmbiguousLabel(string reference)
		{
			return !_nodes.Any(x => String.Equals(x.NodeId, reference, StringComparison.Ordinal))
				&& _nodes.Count(x => String.Equals(x.NodeLabel, reference, StringComparison.Ordinal)) > 1;
		}
	}
}
