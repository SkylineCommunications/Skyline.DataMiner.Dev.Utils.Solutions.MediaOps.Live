namespace Skyline.DataMiner.MediaOps.Live.Mediation
{
	using System;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Automation;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	public static class ConnectionAwaiter
	{
		public static bool Wait(IEngine engine, EndpointInstance source, EndpointInstance destination, TimeSpan timeout)
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

		public static bool Wait(IEngine engine, ConnectionWatcher connectionWatcher, EndpointInstance source, EndpointInstance destination, TimeSpan timeout)
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

			EventHandler<ConnectionInstance> connectionEventHandler = (s, e) =>
			{
				if (e.ConnectionInfo.ConnectedSource == source.ID.Id &&
					e.ConnectionInfo.Destination == destination.ID.Id)
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