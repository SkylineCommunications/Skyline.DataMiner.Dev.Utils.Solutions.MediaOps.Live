namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public sealed class ConnectionMonitor : IDisposable
	{
		private readonly MediaOpsLiveApi _api;
		private readonly LiteConnectivityInfoProvider _connectivityInfoProvider;

		public ConnectionMonitor(MediaOpsLiveApi api)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));

			_connectivityInfoProvider = new LiteConnectivityInfoProvider(api, subscribe: true);
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

			using var cts = new CancellationTokenSource(timeout);
			cts.Token.Register(() => tsc.TrySetResult(false));

			void ConnectionEventHandler(object s, ICollection<ApiObjectReference<Endpoint>> e)
			{
				if ((e.Contains(source) || e.Contains(destination)) &&
					_connectivityInfoProvider.IsConnected(source, destination))
				{
					tsc.TrySetResult(true);
				}
			}

			_connectivityInfoProvider.ConnectionsChanged += ConnectionEventHandler;

			try
			{
				if (_connectivityInfoProvider.IsConnected(source, destination))
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Result;
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

			var tsc = new TaskCompletionSource<bool>();

			using var cts = new CancellationTokenSource(timeout);
			cts.Token.Register(() => tsc.TrySetResult(false));

			void ConnectionEventHandler(object s, ICollection<ApiObjectReference<Endpoint>> e)
			{
				if (e.Contains(destination) && !_connectivityInfoProvider.IsConnected(destination))
				{
					tsc.TrySetResult(true);
				}
			}

			_connectivityInfoProvider.ConnectionsChanged += ConnectionEventHandler;

			try
			{
				if (!_connectivityInfoProvider.IsConnected(destination))
				{
					tsc.TrySetResult(true);
				}

				return tsc.Task.Result;
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
