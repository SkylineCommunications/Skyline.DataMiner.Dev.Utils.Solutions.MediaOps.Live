namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Linq;
	using System.Threading.Tasks;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public static class ConnectionWaiter
	{
		public static bool WaitUntilConnected(ConnectivityInfoProvider connectivityInfoProvider, VirtualSignalGroup source, VirtualSignalGroup destination, TimeSpan timeout)
		{
			if (connectivityInfoProvider == null)
			{
				throw new ArgumentNullException(nameof(connectivityInfoProvider));
			}

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

			connectivityInfoProvider.Subscribe();
			connectivityInfoProvider.ConnectionsUpdated += connectionEventHandler;

			try
			{
				var currentConnectivity = connectivityInfoProvider.GetConnectivity(destination);

				if (currentConnectivity.ConnectedSources.Contains(source))
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Wait(timeout);
			}
			finally
			{
				connectivityInfoProvider.ConnectionsUpdated -= connectionEventHandler;
			}
		}

		public static bool WaitUntilConnected(ConnectivityInfoProvider connectivityInfoProvider, Endpoint source, Endpoint destination, TimeSpan timeout)
		{
			if (connectivityInfoProvider == null)
			{
				throw new ArgumentNullException(nameof(connectivityInfoProvider));
			}

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

			connectivityInfoProvider.Subscribe();
			connectivityInfoProvider.ConnectionsUpdated += connectionEventHandler;

			try
			{
				var currentConnectivity = connectivityInfoProvider.GetConnectivity(destination);

				if (currentConnectivity.ConnectedSource?.Endpoint == source)
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Wait(timeout);
			}
			finally
			{
				connectivityInfoProvider.ConnectionsUpdated -= connectionEventHandler;
			}
		}

		public static bool WaitUntilDisconnected(ConnectivityInfoProvider connectivityInfoProvider, VirtualSignalGroup destination, TimeSpan timeout)
		{
			if (connectivityInfoProvider == null)
			{
				throw new ArgumentNullException(nameof(connectivityInfoProvider));
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
						!connectivity.IsConnected)
					{
						tsc.TrySetResult(true);
						return;
					}
				}
			};

			connectivityInfoProvider.Subscribe();
			connectivityInfoProvider.ConnectionsUpdated += connectionEventHandler;

			try
			{
				var currentConnectivity = connectivityInfoProvider.GetConnectivity(destination);

				if (!currentConnectivity.IsConnected)
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Wait(timeout);
			}
			finally
			{
				connectivityInfoProvider.ConnectionsUpdated -= connectionEventHandler;
			}
		}

		public static bool WaitUntilDisconnected(ConnectivityInfoProvider connectivityInfoProvider, Endpoint destination, TimeSpan timeout)
		{
			if (connectivityInfoProvider == null)
			{
				throw new ArgumentNullException(nameof(connectivityInfoProvider));
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
						!connectivity.IsConnected)
					{
						tsc.TrySetResult(true);
						return;
					}
				}
			};

			connectivityInfoProvider.Subscribe();
			connectivityInfoProvider.ConnectionsUpdated += connectionEventHandler;

			try
			{
				var currentConnectivity = connectivityInfoProvider.GetConnectivity(destination);

				if (!currentConnectivity.IsConnected)
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Wait(timeout);
			}
			finally
			{
				connectivityInfoProvider.ConnectionsUpdated -= connectionEventHandler;
			}
		}
	}
}
