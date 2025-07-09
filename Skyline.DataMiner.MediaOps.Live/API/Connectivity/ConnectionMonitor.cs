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
			if (_cache.TryGetValue(destination, out var cachedConnection))
			{
				return cachedConnection.IsConnected;
			}

			foreach (var element in _mediationElements)
			{
				if (!element.TryGetConnection(destination, out var connection))
				{
					continue;
				}

				_cache[destination] = connection;

				if (connection.IsConnected)
				{
					return true;
				}
			}

			return false;
		}

		private bool IsConnected(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination)
		{
			if (_cache.TryGetValue(destination, out var cachedConnection))
			{
				return cachedConnection.IsConnected && cachedConnection.ConnectedSource == source;
			}

			foreach (var element in _mediationElements)
			{
				if (!element.TryGetConnection(destination, out var connection))
				{
					continue;
				}

				_cache[destination] = connection;

				if (connection.IsConnected && connection.ConnectedSource == source)
				{
					return true;
				}
			}

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
