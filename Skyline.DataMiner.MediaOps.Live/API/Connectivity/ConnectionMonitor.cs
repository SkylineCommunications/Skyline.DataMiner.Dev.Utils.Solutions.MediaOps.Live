namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Threading;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public sealed class ConnectionMonitor : IDisposable
	{
		private readonly MediaOpsLiveApi _api;
		private readonly LiteConnectivityInfoProvider _connectivityInfoProvider;

		public ConnectionMonitor(MediaOpsLiveApi api)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));

			_connectivityInfoProvider = new LiteConnectivityInfoProvider(api);
			_connectivityInfoProvider.Subscribe();
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

			if (_connectivityInfoProvider.IsConnected(source, destination))
			{
				return true;
			}

			using var mre = new ManualResetEventSlim(false);

			void ConnectionEventHandler(object s, ICollection<ApiObjectReference<Endpoint>> e)
			{
				if ((e.Contains(source) || e.Contains(destination)) &&
					_connectivityInfoProvider.IsConnected(source, destination))
				{
					mre.Set();
				}
			}

			try
			{
				_connectivityInfoProvider.ConnectionsChanged += ConnectionEventHandler;

				// Fallback for when we missed the event.
				if (_connectivityInfoProvider.IsConnected(source, destination))
				{
					mre.Set();
				}

				return mre.Wait(timeout);
			}
			finally
			{
				_connectivityInfoProvider.ConnectionsChanged -= ConnectionEventHandler;
			}
		}

		public bool WaitUntilDisconnected(ApiObjectReference<Endpoint> destination, TimeSpan timeout)
		{
			if (destination == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("Destination cannot be empty.", nameof(destination));
			}

			if (!_connectivityInfoProvider.IsConnected(destination))
			{
				return true;
			}

			using var mre = new ManualResetEventSlim(false);

			void ConnectionEventHandler(object s, ICollection<ApiObjectReference<Endpoint>> e)
			{
				if (e.Contains(destination) &&
					!_connectivityInfoProvider.IsConnected(destination))
				{
					mre.Set();
				}
			}

			try
			{
				_connectivityInfoProvider.ConnectionsChanged += ConnectionEventHandler;

				// Fallback for when we missed the event.
				if (!_connectivityInfoProvider.IsConnected(destination))
				{
					return true;
				}

				return mre.Wait(timeout);
			}
			finally
			{
				_connectivityInfoProvider.ConnectionsChanged -= ConnectionEventHandler;
			}
		}

		public void Dispose()
		{
			_connectivityInfoProvider?.Dispose();
		}
	}
}
