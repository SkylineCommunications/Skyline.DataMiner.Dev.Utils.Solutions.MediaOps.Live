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

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var tsc = new TaskCompletionSource<bool>();

			EventHandler<ConnectionsUpdatedEvent> connectionEventHandler = (s, e) =>
			{
				foreach (var connectivity in e.VirtualSignalGroups)
				{
					if (connectivity.VirtualSignalGroup == destination &&
						connectivity.ConnectedSources.Contains(source))
					{
						tsc.TrySetResult(true);
						return;
					}
				}
			};

			_connectivityInfoProvider.ConnectionsUpdated += connectionEventHandler;

			try
			{
				var currentConnectivity = _connectivityInfoProvider.GetConnectivity(destination);

				if (currentConnectivity.ConnectedSources.Contains(source))
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Wait(timeout);
			}
			finally
			{
				_connectivityInfoProvider.ConnectionsUpdated -= connectionEventHandler;
			}
		}

		public bool WaitUntilConnected(Endpoint source, Endpoint destination, TimeSpan timeout)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var tsc = new TaskCompletionSource<bool>();

			EventHandler<ConnectionsUpdatedEvent> connectionEventHandler = (s, e) =>
			{
				foreach (var connectivity in e.Endpoints)
				{
					if (connectivity.Endpoint == destination &&
						connectivity.ConnectedSource?.Endpoint == source)
					{
						tsc.TrySetResult(true);
						return;
					}
				}
			};

			_connectivityInfoProvider.ConnectionsUpdated += connectionEventHandler;

			try
			{
				var currentConnectivity = _connectivityInfoProvider.GetConnectivity(destination);

				if (currentConnectivity.ConnectedSource?.Endpoint == source)
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Wait(timeout);
			}
			finally
			{
				_connectivityInfoProvider.ConnectionsUpdated -= connectionEventHandler;
			}
		}

		public bool WaitUntilDisconnected(VirtualSignalGroup destination, TimeSpan timeout)
		{
			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var tsc = new TaskCompletionSource<bool>();

			EventHandler<ConnectionsUpdatedEvent> connectionEventHandler = (s, e) =>
			{
				foreach (var connectivity in e.VirtualSignalGroups)
				{
					if (connectivity.VirtualSignalGroup == destination &&
						!connectivity.IsConnected)
					{
						tsc.TrySetResult(true);
						return;
					}
				}
			};

			_connectivityInfoProvider.ConnectionsUpdated += connectionEventHandler;

			try
			{
				var currentConnectivity = _connectivityInfoProvider.GetConnectivity(destination);

				if (!currentConnectivity.IsConnected)
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Wait(timeout);
			}
			finally
			{
				_connectivityInfoProvider.ConnectionsUpdated -= connectionEventHandler;
			}
		}

		public bool WaitUntilDisconnected(Endpoint destination, TimeSpan timeout)
		{
			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var tsc = new TaskCompletionSource<bool>();

			EventHandler<ConnectionsUpdatedEvent> connectionEventHandler = (s, e) =>
			{
				foreach (var connectivity in e.Endpoints)
				{
					if (connectivity.Endpoint == destination &&
						!connectivity.IsConnected)
					{
						tsc.TrySetResult(true);
						return;
					}
				}
			};

			_connectivityInfoProvider.ConnectionsUpdated += connectionEventHandler;

			try
			{
				var currentConnectivity = _connectivityInfoProvider.GetConnectivity(destination);

				if (!currentConnectivity.IsConnected)
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Wait(timeout);
			}
			finally
			{
				_connectivityInfoProvider.ConnectionsUpdated -= connectionEventHandler;
			}
		}
	}
}
