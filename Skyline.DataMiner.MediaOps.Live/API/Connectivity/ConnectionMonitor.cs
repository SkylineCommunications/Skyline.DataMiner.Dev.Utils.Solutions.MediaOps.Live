namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public sealed class ConnectionMonitor : IDisposable
	{
		private readonly LiteConnectivityInfoProvider _connectivityInfoProvider;

		public ConnectionMonitor(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			_connectivityInfoProvider = new LiteConnectivityInfoProvider(api);
			_connectivityInfoProvider.Subscribe();
		}

		public async Task<bool> WaitUntilConnectedAsync(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination, CancellationToken cancellationToken)
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

			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

			void ConnectionEventHandler(object s, ICollection<ApiObjectReference<Endpoint>> changedEndpoints)
			{
				if (changedEndpoints.Any(e => e == source || e == destination) &&
					_connectivityInfoProvider.IsConnected(source, destination))
				{
					tcs.TrySetResult(true);
				}
			}

			_connectivityInfoProvider.EndpointsImpacted += ConnectionEventHandler;

			try
			{
				// Fallback for when we missed the event.
				if (_connectivityInfoProvider.IsConnected(source, destination))
				{
					tcs.TrySetResult(true);
				}

				return await tcs.Task;
			}
			finally
			{
				_connectivityInfoProvider.EndpointsImpacted -= ConnectionEventHandler;
			}
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

			try
			{
				using var cts = new CancellationTokenSource(timeout);

				var task = WaitUntilConnectedAsync(source, destination, cts.Token);
				return task.GetAwaiter().GetResult();
			}
			catch (OperationCanceledException)
			{
				return false;
			}
		}

		public async Task<bool> WaitUntilDisconnectedAsync(ApiObjectReference<Endpoint> destination, CancellationToken cancellationToken)
		{
			if (destination == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("Destination cannot be empty.", nameof(destination));
			}

			if (!_connectivityInfoProvider.IsConnected(destination))
			{
				return true;
			}

			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

			void ConnectionEventHandler(object s, ICollection<ApiObjectReference<Endpoint>> changedEndpoints)
			{
				if (changedEndpoints.Contains(destination) &&
					!_connectivityInfoProvider.IsConnected(destination))
				{
					tcs.TrySetResult(true);
				}
			}

			_connectivityInfoProvider.EndpointsImpacted += ConnectionEventHandler;

			try
			{
				// Fallback for when we missed the event.
				if (!_connectivityInfoProvider.IsConnected(destination))
				{
					tcs.TrySetResult(true);
				}

				return await tcs.Task;
			}
			finally
			{
				_connectivityInfoProvider.EndpointsImpacted -= ConnectionEventHandler;
			}
		}

		public bool WaitUntilDisconnected(ApiObjectReference<Endpoint> destination, TimeSpan timeout)
		{
			if (destination == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("Destination cannot be empty.", nameof(destination));
			}

			try
			{
				using var cts = new CancellationTokenSource(timeout);

				var task = WaitUntilDisconnectedAsync(destination, cts.Token);
				return task.GetAwaiter().GetResult();
			}
			catch (OperationCanceledException)
			{
				return false;
			}
		}

		public void Dispose()
		{
			_connectivityInfoProvider?.Dispose();
		}
	}
}
