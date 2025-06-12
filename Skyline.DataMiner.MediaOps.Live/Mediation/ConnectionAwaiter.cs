namespace Skyline.DataMiner.MediaOps.Live.Mediation
{
	using System;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public static class ConnectionAwaiter
	{
		public static bool Wait(IEngine engine, Endpoint source, Endpoint destination, TimeSpan timeout)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			using (var watcher = new ConnectionWatcher())
			{
				return Wait(engine, watcher, source, destination, timeout);
			}
		}

		public static bool Wait(IEngine engine, ConnectionWatcher connectionWatcher, Endpoint source, Endpoint destination, TimeSpan timeout)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			if (connectionWatcher == null)
			{
				throw new ArgumentNullException(nameof(connectionWatcher));
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

			EventHandler<Connection> connectionEventHandler = (s, e) =>
			{
				if (e.ConnectedSource == source &&
				    e.Destination == destination)
				{
					tsc.TrySetResult(true);
				}
			};

			connectionWatcher.Changed += connectionEventHandler;

			try
			{
				if (connectionWatcher.IsConnected(source, destination))
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Wait(timeout);
			}
			finally
			{
				connectionWatcher.Changed -= connectionEventHandler;
			}
		}
	}
}