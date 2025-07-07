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

	public sealed class ConnectionMonitor : IDisposable
	{
		private readonly ICollection<MediationElement> _mediationElements;

		public ConnectionMonitor(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			_mediationElements = MediationElement.GetAllMediationElements(api).ToList();

			foreach (var element in _mediationElements)
			{
				element.Subscribe(skipInitialEvents: true);
				element.ConnectionsChanged += Connections_OnChanged;
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

			if (IsConnected(source, destination))
			{
				return true;
			}

			var tsc = new TaskCompletionSource<bool>();

			using var cts = new CancellationTokenSource(timeout);
			cts.Token.Register(() => tsc.TrySetResult(false));

			void ConnectionEventHandler(object s, ConnectionsChangedEvent e)
			{
				if (e.UpdatedConnections.Any(x => x.Destination == destination && x.IsConnected && x.ConnectedSource == source))
				{
					tsc.TrySetResult(true);
				}
			}

			ConnectionsChanged += ConnectionEventHandler;

			try
			{
				if (IsConnected(source, destination))
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Result;
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

			if (!IsConnected(destination))
			{
				return true;
			}

			var tsc = new TaskCompletionSource<bool>();

			using var cts = new CancellationTokenSource(timeout);
			cts.Token.Register(() => tsc.TrySetResult(false));

			void ConnectionEventHandler(object s, ConnectionsChangedEvent e)
			{
				if (e.DeletedConnections.Contains(destination))
				{
					tsc.TrySetResult(true);
				}
				else if (e.UpdatedConnections.Any(x => x.Destination == destination && !x.IsConnected))
				{
					tsc.TrySetResult(true);
				}
			}

			ConnectionsChanged += ConnectionEventHandler;

			try
			{
				if (!IsConnected(destination))
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Result;
			}
			finally
			{
				ConnectionsChanged -= ConnectionEventHandler;
			}
		}

		public void Dispose()
		{
			foreach (var mediationElement in _mediationElements)
			{
				mediationElement.Dispose();
			}
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

		private void Connections_OnChanged(object sender, ConnectionsChangedEvent e)
		{
			ConnectionsChanged?.Invoke(sender, e);
		}
	}
}
