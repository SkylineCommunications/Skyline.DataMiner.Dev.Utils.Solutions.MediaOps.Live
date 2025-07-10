namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;

	public sealed class ConnectionMonitor : IDisposable
	{
		private readonly ICollection<ConnectionSubscription> _subscriptions = [];

		private readonly ConcurrentDictionary<ApiObjectReference<Endpoint>, Connection> _cache = new();

		public ConnectionMonitor(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			foreach (var element in api.MediationElements.AllElements)
			{
				var connectionSubscription = element.CreateConnectionSubscription();
				_subscriptions.Add(connectionSubscription);

				connectionSubscription.Changed += OnConnectionsChanged;
				connectionSubscription.Subscribe();
			}
		}

		private event EventHandler<ConnectionsChangedEvent> ConnectionsChanged;

		public bool WaitUntilConnected(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination, TimeSpan timeout)
		{
			if (source == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("Source cannot be empty.", nameof(source));
			}

			if (destination == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("Destination cannot be empty.", nameof(destination));
			}

			bool Condition(Connection connection) =>
				connection != null &&
				connection.Destination == destination &&
				connection.IsConnected &&
				connection.ConnectedSource == source;

			if (TryGetCachedConnection(destination, out var connection) &&
				Condition(connection))
			{
				return true;
			}

			using var mre = new ManualResetEventSlim(false);

			void ConnectionEventHandler(object s, ConnectionsChangedEvent e)
			{
				if (e.UpdatedConnections.Any(Condition))
				{
					mre.Set();
				}
			}

			try
			{
				ConnectionsChanged += ConnectionEventHandler;

				return mre.Wait(timeout);
			}
			finally
			{
				ConnectionsChanged -= ConnectionEventHandler;
			}
		}

		public bool WaitUntilDisconnected(ApiObjectReference<Endpoint> destination, TimeSpan timeout)
		{
			if (destination == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("Destination cannot be empty.", nameof(destination));
			}

			bool Condition(Connection connection) =>
				connection != null &&
				connection.Destination == destination &&
				!connection.IsConnected;

			if (TryGetCachedConnection(destination, out var connection) &&
				Condition(connection))
			{
				return true;
			}

			using var mre = new ManualResetEventSlim(false);

			void ConnectionEventHandler(object s, ConnectionsChangedEvent e)
			{
				if (e.DeletedConnections.Contains(destination) ||
					e.UpdatedConnections.Any(Condition))
				{
					mre.Set();
				}
			}

			try
			{
				ConnectionsChanged += ConnectionEventHandler;

				return mre.Wait(timeout);
			}
			finally
			{
				ConnectionsChanged -= ConnectionEventHandler;
			}
		}

		public void Dispose()
		{
			foreach (var subscription in _subscriptions)
			{
				subscription.Changed -= OnConnectionsChanged;
				subscription.Dispose();
			}

			_subscriptions.Clear();
		}

		private bool TryGetCachedConnection(ApiObjectReference<Endpoint> destination, out Connection connection)
		{
			return _cache.TryGetValue(destination, out connection) && connection != null;
		}

		private void OnConnectionsChanged(object sender, ConnectionsChangedEvent e)
		{
			foreach (var item in e.DeletedConnections)
			{
				_cache.TryRemove(item, out _);
			}

			foreach (var item in e.UpdatedConnections)
			{
				_cache[item.Destination] = item;
			}

			ConnectionsChanged?.Invoke(sender, e);
		}
	}
}
