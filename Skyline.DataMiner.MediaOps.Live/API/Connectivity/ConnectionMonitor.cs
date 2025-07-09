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
		private readonly ICollection<MediationElement> _mediationElements;
		private readonly ICollection<ConnectionSubscription> _subscriptions = [];

		private readonly ConcurrentDictionary<ApiObjectReference<Endpoint>, Connection> _cache = new();

		public ConnectionMonitor(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			_mediationElements = MediationElement.GetAllMediationElements(api).ToList();

			foreach (var element in _mediationElements)
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

			if (TryGetCachedConnection(destination, out var connection) && connection.IsConnected && connection.ConnectedSource == source)
			{
				return true;
			}

			using var mre = new ManualResetEventSlim(false);

			void ConnectionEventHandler(object s, ConnectionsChangedEvent e)
			{
				if (e.UpdatedConnections.Any(x => x.Destination == destination && x.IsConnected && x.ConnectedSource == source))
				{
					mre.Set();
				}
			}

			try
			{
				ConnectionsChanged += ConnectionEventHandler;

				if (mre.Wait(250))
				{
					// Wait a bit for an event.
					return true;
				}

				// Fallback for when we missed the event.
				if (IsConnected(source, destination))
				{
					mre.Set();
				}

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

			if (TryGetCachedConnection(destination, out var connection) && !connection.IsConnected)
			{
				return true;
			}

			using var mre = new ManualResetEventSlim(false);

			void ConnectionEventHandler(object s, ConnectionsChangedEvent e)
			{
				if (e.DeletedConnections.Contains(destination) ||
					e.UpdatedConnections.Any(x => x.Destination == destination && !x.IsConnected))
				{
					mre.Set();
				}
			}

			try
			{
				ConnectionsChanged += ConnectionEventHandler;

				if (mre.Wait(250))
				{
					// Wait a bit for an event.
					return true;
				}

				// Fallback for when we missed the event.
				if (!IsConnected(destination))
				{
					mre.Set();
				}

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

		private bool IsConnected(ApiObjectReference<Endpoint> destination)
		{
			return TryGetConnection(destination, out var connection) &&
				connection.IsConnected;
		}

		private bool IsConnected(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination)
		{
			return TryGetConnection(destination, out var connection) &&
				connection.IsConnected &&
				connection.ConnectedSource == source;
		}

		private bool TryGetCachedConnection(ApiObjectReference<Endpoint> destination, out Connection connection)
		{
			return _cache.TryGetValue(destination, out connection) && connection != null;
		}

		private bool TryGetConnection(ApiObjectReference<Endpoint> destination, out Connection connection)
		{
			if (TryGetCachedConnection(destination, out connection))
			{
				return true;
			}

			foreach (var element in _mediationElements)
			{
				if (element.TryGetConnection(destination, out var foundConnection))
				{
					connection = foundConnection;
					return true;
				}
			}

			connection = null;
			return false;
		}

		private void OnConnectionsChanged(object sender, ConnectionsChangedEvent e)
		{
			foreach (var item in e.DeletedConnections)
			{
				_cache.TryRemove(item, out var _);
			}

			foreach (var item in e.UpdatedConnections)
			{
				_cache[item.Destination] = item;
			}

			ConnectionsChanged?.Invoke(sender, e);
		}
	}
}
