namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases.Values;

	/// <summary>
	/// The job context values of the current node, shared by all events of the job.
	/// A node writes only its own values, and can read the values of the other nodes of the job through <see cref="ForNode"/>.
	/// </summary>
	public sealed class OrchestrationJobValues : IOrchestrationWritableValues
	{
		private readonly IOrchestrationWritableValues _ownValues;
		private readonly Func<string, IOrchestrationValues> _forNode;

		private OrchestrationJobValues(IOrchestrationWritableValues ownValues, Func<string, IOrchestrationValues> forNode)
		{
			_ownValues = ownValues;
			_forNode = forNode;
		}

		/// <inheritdoc/>
		public IReadOnlyCollection<string> Keys => _ownValues.Keys;

		/// <inheritdoc/>
		public string Get(string key) => _ownValues.Get(key);

		/// <inheritdoc/>
		public bool TryGet(string key, out string value) => _ownValues.TryGet(key, out value);

		/// <inheritdoc/>
		public void Set(string key, string value) => _ownValues.Set(key, value);

		/// <inheritdoc/>
		public bool Remove(string key) => _ownValues.Remove(key);

		/// <summary>
		/// Gets a read-only view on the job context values of another node of the job.
		/// </summary>
		/// <param name="node">The ID or label of a node of the current event, or the ID of a node of another event of the job.</param>
		/// <returns>The values of that node.</returns>
		public IOrchestrationValues ForNode(string node)
		{
			if (String.IsNullOrWhiteSpace(node))
			{
				throw new ArgumentException("The node cannot be empty.", nameof(node));
			}

			return _forNode(node);
		}

		internal static OrchestrationJobValues Create(NodeContextValueRepository repository, Guid jobInfoId, string nodeId, IEventNodes eventNodes)
		{
			return new OrchestrationJobValues(
				new OrchestrationValues(new JobValueBackend(repository, jobInfoId, nodeId)),
				node =>
				{
					if (!eventNodes.TryResolveJobNode(node, out EventNode resolved, out string failure))
					{
						throw new ArgumentException($"Unable to read the job context values of node '{node}': {failure}.", nameof(node));
					}

					return new ReadOnlyOrchestrationValues(new JobValueBackend(repository, jobInfoId, resolved.NodeId));
				});
		}

		internal static OrchestrationJobValues Unavailable(string reason)
		{
			return new OrchestrationJobValues(
				new OrchestrationValues(new UnavailableValueBackend(reason)),
				_ => throw new InvalidOperationException(reason));
		}
	}
}
