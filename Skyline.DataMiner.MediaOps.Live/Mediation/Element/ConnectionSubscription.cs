namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Subscriptions;

	public sealed class ConnectionSubscription : IDisposable
	{
		private readonly object _lock = new object();

		private readonly MediaOpsLiveApi _api;
		private readonly MediationElement _mediationElement;

		private TableSubscription _subscription;

		public ConnectionSubscription(MediaOpsLiveApi api, MediationElement mediationElement)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
			_mediationElement = mediationElement ?? throw new ArgumentNullException(nameof(mediationElement));
		}

		public event EventHandler<ConnectionsChangedEvent> Changed;

		public void Subscribe(bool skipInitialEvents = false)
		{
			lock (_lock)
			{
				if (_subscription != null)
					return;

				_subscription = new TableSubscription(
					_api.Connection,
					_mediationElement.DmsElement,
					MediationElement.ConnectionsTableId,
					skipInitialEvents: skipInitialEvents);
				_subscription.OnChanged += HandleChange;
			}
		}

		public void Unsubscribe()
		{
			lock (_lock)
			{
				if (_subscription == null)
					return;

				_subscription.OnChanged -= HandleChange;
				_subscription.Dispose();
				_subscription = null;
			}
		}

		private void HandleChange(object sender, TableValueChange e)
		{
			var updated = e.UpdatedRows.Values.Select(r => new Connection(r));
			var deleted = e.DeletedRows.Select(id => Guid.Parse(id));

			Changed?.Invoke(this, new ConnectionsChangedEvent(updated, deleted));
		}

		public void Dispose() => Unsubscribe();
	}
}
