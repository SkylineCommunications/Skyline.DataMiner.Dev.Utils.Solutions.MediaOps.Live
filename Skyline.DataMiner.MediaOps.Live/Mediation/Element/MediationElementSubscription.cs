namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Subscriptions;

	public sealed class MediationElementSubscription : IDisposable
	{
		private readonly object _lock = new();

		private readonly MediaOpsLiveApi _api;
		private readonly MediationElement _mediationElement;

		private bool _isSubscribed;
		private TableSubscription _subscriptionConnections;
		private TableSubscription _subscriptionPendingConnectionActions;

		internal MediationElementSubscription(MediaOpsLiveApi api, MediationElement mediationElement)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
			_mediationElement = mediationElement ?? throw new ArgumentNullException(nameof(mediationElement));
		}

		public event EventHandler<ConnectionsChangedEvent> ConnectionsChanged;

		public event EventHandler<PendingConnectionActionsChangedEvent> PendingConnectionActionsChanged;

		internal MediationElement MediationElement => _mediationElement;

		public void Subscribe(bool skipInitialEvents = true)
		{
			lock (_lock)
			{
				if (_isSubscribed)
					return;

				_subscriptionConnections = new TableSubscription(
					_api.Connection,
					_mediationElement.DmsElement,
					MediationElement.ConnectionsTableId,
					skipInitialEvents: skipInitialEvents);
				_subscriptionConnections.OnChanged += HandleChange_Connections;

				_subscriptionPendingConnectionActions = new TableSubscription(
					_api.Connection,
					_mediationElement.DmsElement,
					MediationElement.PendingConnectionActionsTableId,
					skipInitialEvents: skipInitialEvents);
				_subscriptionPendingConnectionActions.OnChanged += HandleChange_PendingConnectionActions;

				_isSubscribed = true;
			}
		}

		public void Unsubscribe()
		{
			lock (_lock)
			{
				if (!_isSubscribed)
					return;

				_subscriptionConnections.OnChanged -= HandleChange_Connections;
				_subscriptionConnections.Dispose();
				_subscriptionConnections = null;

				_subscriptionPendingConnectionActions.OnChanged -= HandleChange_PendingConnectionActions;
				_subscriptionPendingConnectionActions.Dispose();
				_subscriptionPendingConnectionActions = null;

				_isSubscribed = false;
			}
		}

		public void Dispose()
		{
			Unsubscribe();
		}

		private void HandleChange_Connections(object sender, TableValueChange e)
		{
			var updated = e.UpdatedRows.Values.Select(r => new Connection(_mediationElement, r));
			var deleted = e.DeletedRows.Values.Select(r => new Connection(_mediationElement, r));

			ConnectionsChanged?.Invoke(this, new ConnectionsChangedEvent(updated, deleted));
		}

		private void HandleChange_PendingConnectionActions(object sender, TableValueChange e)
		{
			var updated = e.UpdatedRows.Values.Select(r => new PendingConnectionAction(_mediationElement, r));
			var deleted = e.DeletedRows.Values.Select(r => new PendingConnectionAction(_mediationElement, r));

			PendingConnectionActionsChanged?.Invoke(this, new PendingConnectionActionsChangedEvent(updated, deleted));
		}
	}
}
