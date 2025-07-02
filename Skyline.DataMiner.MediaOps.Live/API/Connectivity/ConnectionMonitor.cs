namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Linq;
	using System.Threading.Tasks;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class ConnectionMonitor
	{
		private readonly ConnectivityInfoProvider _connectivityInfoProvider;

		public ConnectionMonitor(ConnectivityInfoProvider connectivityInfoProvider)
		{
			if (connectivityInfoProvider == null)
			{
				throw new ArgumentNullException(nameof(connectivityInfoProvider));
			}

			if (!connectivityInfoProvider.IsSubscribed)
			{
				throw new InvalidOperationException("ConnectivityInfoProvider must be subscribed before using ConnectionMonitor.");
			}

			_connectivityInfoProvider = connectivityInfoProvider;
		}

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

			var tsc = new TaskCompletionSource<bool>();

			void ConnectionEventHandler(object s, ConnectionsUpdatedEvent e)
			{
				if (e.Endpoints.Any(connectivity => connectivity.Endpoint == destination &&
													connectivity.ConnectedSource?.Endpoint == source))
				{
					tsc.TrySetResult(true);
				}
			}

			_connectivityInfoProvider.ConnectionsUpdated += ConnectionEventHandler;

			try
			{
				if (_connectivityInfoProvider.GetConnectivity(destination).ConnectedSource?.Endpoint == source)
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Wait(timeout);
			}
			finally
			{
				_connectivityInfoProvider.ConnectionsUpdated -= ConnectionEventHandler;
			}
		}

		public bool WaitUntilDisconnected(ApiObjectReference<Endpoint> destination, TimeSpan timeout)
		{
			if (destination == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("Destination cannot be empty.", nameof(destination));
			}

			var tsc = new TaskCompletionSource<bool>();

			void ConnectionEventHandler(object s, ConnectionsUpdatedEvent e)
			{
				if (e.Endpoints.Any(connectivity => connectivity.Endpoint == destination && !connectivity.IsConnected))
				{
					tsc.TrySetResult(true);
				}
			}

			_connectivityInfoProvider.ConnectionsUpdated += ConnectionEventHandler;

			try
			{
				if (!_connectivityInfoProvider.GetConnectivity(destination).IsConnected)
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Wait(timeout);
			}
			finally
			{
				_connectivityInfoProvider.ConnectionsUpdated -= ConnectionEventHandler;
			}
		}
	}
}
