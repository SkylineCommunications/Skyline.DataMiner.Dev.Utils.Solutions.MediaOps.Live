namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Subscriptions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;

	/// <summary>
	/// Reports changes to the phases of orchestration events as they are recorded.
	/// </summary>
	/// <remarks>
	/// <b>Warning:</b> The subscription must be disposed when no longer needed to avoid resource leaks.
	/// </remarks>
	public sealed class OrchestrationEventPhasesSubscription : IDisposable
	{
		private readonly NodePhasesRepository _repository;
		private readonly RepositorySubscription<OrchestrationNodePhases> _subscription;

		internal OrchestrationEventPhasesSubscription(NodePhasesRepository repository, Guid? eventId)
		{
			_repository = repository ?? throw new ArgumentNullException(nameof(repository));
			_subscription = repository.SubscribeOnPhases(eventId);
			_subscription.Changed += Subscription_Changed;
		}

		/// <summary>
		/// Raised when the phases of one or more nodes changed, with the current phases of each of those nodes.
		/// </summary>
		public event EventHandler<IReadOnlyList<OrchestrationNodePhasesChange>> Changed;

		/// <inheritdoc/>
		public void Dispose()
		{
			_subscription.Changed -= Subscription_Changed;
			_subscription.Dispose();
			Changed = null;
		}

		private void Subscription_Changed(object sender, ApiObjectsChangedEvent<OrchestrationNodePhases> e)
		{
			EventHandler<IReadOnlyList<OrchestrationNodePhasesChange>> handler = Changed;
			if (handler == null)
			{
				return;
			}

			// The stored phases of a node are read again, since a node can briefly have duplicate instances that are cleaned up on write.
			List<OrchestrationNodePhasesChange> changes = e.Created.Concat(e.Updated).Concat(e.Deleted)
				.Select(x => new { EventId = x.EventReference.ID, x.NodeId })
				.Distinct()
				.Select(x =>
				{
					OrchestrationNodePhases current = _repository.GetNodePhases(x.EventId, x.NodeId);
					return new OrchestrationNodePhasesChange(x.EventId, x.NodeId, current != null ? NodePhasesRepository.ToEventPhases(current) : null);
				})
				.ToList();

			if (changes.Count > 0)
			{
				handler(this, changes);
			}
		}
	}
}
