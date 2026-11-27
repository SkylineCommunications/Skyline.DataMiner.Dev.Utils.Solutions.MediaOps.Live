namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases.Values;

	/// <summary>
	/// The node context values of the current node, shared by all events of the job.
	/// A node writes only its own values, and can read the values of the other nodes of the job through <see cref="ForNode"/>.
	/// The global script of an event has no values of its own, but reads the values of the nodes through <see cref="ForNode"/>.
	/// </summary>
	internal sealed class OrchestrationNodeValues
	{
		private readonly IOrchestrationValues _values;
		private readonly IOrchestrationWritableValues _writableValues;
		private readonly Func<string, IOrchestrationValues> _forNode;

		private OrchestrationNodeValues(
			IOrchestrationValues values,
			IOrchestrationWritableValues writableValues,
			Func<string, IOrchestrationValues> forNode)
		{
			_values = values;
			_writableValues = writableValues;
			_forNode = forNode;
		}

		internal IOrchestrationValues Values => _values;

		internal IOrchestrationWritableValues WritableValues => _writableValues;

		internal IOrchestrationValues ForNode(string node)
		{
			if (String.IsNullOrWhiteSpace(node))
			{
				throw new ArgumentException("The node cannot be empty.", nameof(node));
			}

			return _forNode(node);
		}

		internal static OrchestrationNodeValues Create(NodeContextValueRepository repository, Guid jobInfoId, string nodeId, IEventNodes eventNodes)
		{
			var values = new OrchestrationValues(new JobValueBackend(repository, jobInfoId, nodeId));
			return new OrchestrationNodeValues(
				values,
				values,
				ForNodeResolver(repository, jobInfoId, eventNodes));
		}

		internal static OrchestrationNodeValues Unavailable(string reason)
		{
			var values = new ReadOnlyOrchestrationValues(new UnavailableValueBackend(reason));
			return new OrchestrationNodeValues(
				values,
				writableValues: null,
				_ => throw new InvalidOperationException(reason));
		}

		/// <summary>
		/// Values for a script that runs for an event but not for one of its nodes (the global script): it has no values of its own,
		/// but reads the values of the nodes of the job through <see cref="ForNode"/>.
		/// </summary>
		internal static OrchestrationNodeValues ReadOnlyForEvent(NodeContextValueRepository repository, Guid jobInfoId, IEventNodes eventNodes, string unavailableReason)
		{
			var values = new ReadOnlyOrchestrationValues(new UnavailableValueBackend(unavailableReason));
			return new OrchestrationNodeValues(
				values,
				writableValues: null,
				ForNodeResolver(repository, jobInfoId, eventNodes));
		}

		private static Func<string, IOrchestrationValues> ForNodeResolver(NodeContextValueRepository repository, Guid jobInfoId, IEventNodes eventNodes)
		{
			return node =>
			{
				if (!eventNodes.TryResolveJobNode(node, out EventNode resolved, out string failure))
				{
					throw new ArgumentException($"Unable to read the job context values of node '{node}': {failure}.", nameof(node));
				}

				return new ReadOnlyOrchestrationValues(new JobValueBackend(repository, jobInfoId, resolved.NodeId));
			};
		}
	}
}
