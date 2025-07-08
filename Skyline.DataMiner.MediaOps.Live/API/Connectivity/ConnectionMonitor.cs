namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.MediaOps.Live.Tools;

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

				connectionSubscription.Changed += Connections_OnChanged;
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

			var tsc = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			using var cts = new CancellationTokenSource(timeout);
			using var tokenRegistration = cts.Token.Register(() => tsc.TrySetResult(false));

			void ConnectionEventHandler(object s, ConnectionsChangedEvent e)
			{
				if (e.UpdatedConnections.Any(x => x.Destination == destination && x.IsConnected && x.ConnectedSource == source))
				{
					tsc.TrySetResult(true);
				}
			}

			try
			{
				ConnectionsChanged += ConnectionEventHandler;

				Task.Run(() =>
					{
						if (IsConnected(source, destination))
						{
							tsc.TrySetResult(true);
						}
					})
					.FireAndForget();

				return tsc.Task.GetAwaiter().GetResult();
			}
			finally
			{
				ConnectionsChanged -= ConnectionEventHandler;
				cts.Cancel();
			}
		}

		public bool WaitUntilDisconnected(ApiObjectReference<Endpoint> destination, TimeSpan timeout)
		{
			if (destination == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("Destination cannot be empty.", nameof(destination));
			}

			var tsc = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			using var cts = new CancellationTokenSource(timeout);
			using var tokenRegistration = cts.Token.Register(() => tsc.TrySetResult(false));

			void ConnectionEventHandler(object s, ConnectionsChangedEvent e)
			{
				if (e.DeletedConnections.Contains(destination) ||
					e.UpdatedConnections.Any(x => x.Destination == destination && !x.IsConnected))
				{
					tsc.TrySetResult(true);
				}
			}

			try
			{
				ConnectionsChanged += ConnectionEventHandler;

				Task.Run(() =>
					{
						if (!IsConnected(destination))
						{
							tsc.TrySetResult(true);
						}
					})
					.FireAndForget();

				return tsc.Task.GetAwaiter().GetResult();
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
				subscription.Changed -= Connections_OnChanged;
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

			return _mediationElements
				.Any(element =>
				{
					return element.TryGetConnection(destination, out var connection) &&
						connection.IsConnected;
				});
		}

		private bool IsConnected(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination)
		{
			if (_cache.TryGetValue(destination, out var cachedConnection))
			{
				return cachedConnection.IsConnected && cachedConnection.ConnectedSource == source;
			}

			return _mediationElements
				.Any(element =>
				{
					return element.TryGetConnection(destination, out var connection) &&
						connection.IsConnected &&
						connection.ConnectedSource == source;
				});
		}

		private void Connections_OnChanged(object sender, ConnectionsChangedEvent e)
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
