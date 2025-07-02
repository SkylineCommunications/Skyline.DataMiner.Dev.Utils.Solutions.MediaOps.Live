namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Linq;
	using System.Threading.Tasks;

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

		public bool WaitUntilConnected(VirtualSignalGroup source, VirtualSignalGroup destination, TimeSpan timeout)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			return WaitForCondition(
				destination,
				d => _connectivityInfoProvider.GetConnectivity(d).ConnectedSources.Contains(source),
				(e, d) => e.VirtualSignalGroups.Any(connectivity =>
					connectivity.VirtualSignalGroup == d &&
					connectivity.ConnectedSources.Contains(source)),
				timeout);
		}

		public bool WaitUntilConnected(Endpoint source, Endpoint destination, TimeSpan timeout)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			return WaitForCondition(
				destination,
				d => _connectivityInfoProvider.GetConnectivity(d).ConnectedSource?.Endpoint == source,
				(e, d) => e.Endpoints.Any(connectivity =>
					connectivity.Endpoint == d &&
					connectivity.ConnectedSource?.Endpoint == source),
				timeout);
		}

		public bool WaitUntilDisconnected(VirtualSignalGroup destination, TimeSpan timeout)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			return WaitForCondition(
				destination,
				d => !_connectivityInfoProvider.GetConnectivity(d).IsConnected,
				(e, d) => e.VirtualSignalGroups.Any(connectivity =>
					connectivity.VirtualSignalGroup == d &&
					!connectivity.IsConnected),
				timeout);
		}

		public bool WaitUntilDisconnected(Endpoint destination, TimeSpan timeout)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			return WaitForCondition(
				destination,
				d => !_connectivityInfoProvider.GetConnectivity(d).IsConnected,
				(e, d) => e.Endpoints.Any(connectivity =>
					connectivity.Endpoint == d &&
					!connectivity.IsConnected),
				timeout);
		}

		private bool WaitForCondition<T>(T target, Func<T, bool> currentStateCheck, Func<ConnectionsUpdatedEvent, T, bool> eventStateCheck, TimeSpan timeout)
		{
			var tsc = new TaskCompletionSource<bool>();

			void ConnectionEventHandler(object s, ConnectionsUpdatedEvent e)
			{
				if (eventStateCheck(e, target))
				{
					tsc.TrySetResult(true);
				}
			}

			_connectivityInfoProvider.ConnectionsUpdated += ConnectionEventHandler;

			try
			{
				if (currentStateCheck(target))
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
