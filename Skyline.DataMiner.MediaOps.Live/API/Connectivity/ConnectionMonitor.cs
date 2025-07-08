namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.MediaOps.Live.Subscriptions;
	using Skyline.DataMiner.Net.SLDataGateway.Helpers;

	public sealed class ConnectionMonitor : IDisposable
	{
		private readonly ApiObjectReference<Endpoint> _destination;
		private readonly ICollection<MediationElement> _mediationElements;
		private readonly ICollection<TableSubscription> _subscriptions = new List<TableSubscription>();

		public ConnectionMonitor(MediaOpsLiveApi api, ApiObjectReference<Endpoint> destination)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			_destination = destination;

			_mediationElements = MediationElement.GetAllMediationElements(api).ToList();

			foreach (var element in _mediationElements)
			{
				var subscription = new TableSubscription(api.Connection, element.DmsElement, 5000, Convert.ToString(destination.ID));
				_subscriptions.Add(subscription);

				subscription.OnChanged += OnChanged;
			}
		}

		private event EventHandler<ConnectionsChangedEvent> ConnectionsChanged;

		public bool WaitUntilConnected(ApiObjectReference<Endpoint> source, TimeSpan timeout)
		{
			if (source == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("Source cannot be empty.", nameof(source));
			}

			var tsc = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			using var cts = new CancellationTokenSource(timeout);
			cts.Token.Register(() => tsc.TrySetResult(false));

			void ConnectionEventHandler(object s, ConnectionsChangedEvent e)
			{
				if (e.UpdatedConnections.Any(x => x.Destination == _destination && x.IsConnected && x.ConnectedSource == source))
				{
					tsc.TrySetResult(true);
				}
			}

			ConnectionsChanged += ConnectionEventHandler;

			try
			{
				Task.Run(() =>
				{
					if (IsConnected(source, _destination))
					{
						tsc.TrySetResult(true);
					}
				}).Forget();

				return tsc.Task.GetAwaiter().GetResult();
			}
			finally
			{
				ConnectionsChanged -= ConnectionEventHandler;
			}
		}

		public bool WaitUntilDisconnected(TimeSpan timeout)
		{
			var tsc = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			using var cts = new CancellationTokenSource(timeout);
			cts.Token.Register(() => tsc.TrySetResult(false));

			void ConnectionEventHandler(object s, ConnectionsChangedEvent e)
			{
				if (e.DeletedConnections.Contains(_destination) ||
					e.UpdatedConnections.Any(x => x.Destination == _destination && !x.IsConnected))
				{
					tsc.TrySetResult(true);
				}
			}

			ConnectionsChanged += ConnectionEventHandler;

			try
			{
				Task.Run(() =>
				{
					if (!IsConnected(_destination))
					{
						tsc.TrySetResult(true);
					}
				}).Forget();

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
				subscription.OnChanged -= OnChanged;
				subscription.Dispose();
			}

			_subscriptions.Clear();
		}

		private bool IsConnected(ApiObjectReference<Endpoint> destination)
		{
			return _mediationElements
				.Any(element =>
				{
					return element.TryGetConnection(destination, out var connection) &&
						connection.IsConnected;
				});
		}

		private bool IsConnected(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination)
		{
			return _mediationElements
				.Any(element =>
				{
					return element.TryGetConnection(destination, out var connection) &&
						connection.IsConnected &&
						connection.ConnectedSource == source;
				});
		}

		private void OnChanged(object sender, TableValueChange e)
		{
			var e2 = new ConnectionsChangedEvent(
				e.UpdatedRows.Values.Select(r => new Connection(r)),
				e.DeletedRows.Select(id => Guid.Parse(id)));

			ConnectionsChanged?.Invoke(sender, e2);
		}
	}
}
