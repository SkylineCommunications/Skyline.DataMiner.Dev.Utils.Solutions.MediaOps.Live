namespace Skyline.DataMiner.MediaOps.Live.API.Caching
{
	using System;
	using System.Threading;

	using Skyline.DataMiner.MediaOps.Live.API;

	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Net;

	public sealed class StaticMediaOpsLiveCache : IDisposable
	{
		private static readonly object _lock = new();
		private static volatile StaticMediaOpsLiveCache _instance;

		private readonly Lazy<MediaOpsLiveApi> _lazyApi;
		private readonly Lazy<VirtualSignalGroupEndpointsCache> _lazyVirtualSignalGroupsCache;
		private readonly Lazy<LevelsCache> _lazyLevelsCache;
		private readonly Lazy<LiteConnectivityInfoProvider> _lazyLiteConnectivityInfoProvider;
		private readonly Lazy<ConnectivityInfoProvider> _lazyConnectivityInfoProvider;
		private readonly Lazy<ConnectionMonitor> _lazyConnectionMonitor;

		private bool _disposed;

		private StaticMediaOpsLiveCache(IConnection connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));

			_lazyApi = new(() => new MediaOpsLiveApi(Connection));
			_lazyVirtualSignalGroupsCache = new(() => new VirtualSignalGroupEndpointsCache(Api, subscribe: true));
			_lazyLevelsCache = new(() => new LevelsCache(Api, subscribe: true));
			_lazyLiteConnectivityInfoProvider = new(() => new LiteConnectivityInfoProvider(Api, subscribe: true));
			_lazyConnectivityInfoProvider = new(() => new ConnectivityInfoProvider(Api, LiteConnectivityInfoProvider, VirtualSignalGroupsCache, LevelsCache, subscribe: true));
			_lazyConnectionMonitor = new(() => new ConnectionMonitor(Api, LiteConnectivityInfoProvider));
		}

		internal IConnection Connection { get; }

		internal MediaOpsLiveApi Api => _lazyApi.Value;

		public VirtualSignalGroupEndpointsCache VirtualSignalGroupsCache => _lazyVirtualSignalGroupsCache.Value;

		public LevelsCache LevelsCache => _lazyLevelsCache.Value;

		public LiteConnectivityInfoProvider LiteConnectivityInfoProvider => _lazyLiteConnectivityInfoProvider.Value;

		public ConnectivityInfoProvider ConnectivityInfoProvider => _lazyConnectivityInfoProvider.Value;

		public ConnectionMonitor ConnectionMonitor => _lazyConnectionMonitor.Value;

		internal static StaticMediaOpsLiveCache GetOrCreate(Func<IConnection> connectionFactory)
		{
			if (connectionFactory == null)
			{
				throw new ArgumentNullException(nameof(connectionFactory));
			}

			if (_instance == null)
			{
				lock (_lock)
				{
					_instance ??= GetOrCreate(connectionFactory());
				}
			}

			return _instance;
		}

		internal static StaticMediaOpsLiveCache GetOrCreate(IConnection baseConnection)
		{
			if (baseConnection is null)
			{
				throw new ArgumentNullException(nameof(baseConnection));
			}

			if (_instance == null)
			{
				lock (_lock)
				{
					if (_instance == null)
					{
						// Always clone the connection to ensure that the StaticMediaOpsLiveCache has its own dedicated connection.
						// This prevents potential conflicts when the base connection would be closed or unsubscribed elsewhere.
						var connection = CloneConnection(baseConnection);

						_instance = new StaticMediaOpsLiveCache(connection);
					}
				}
			}

			return _instance;
		}

		internal static StaticMediaOpsLiveCache Get()
		{
			if (_instance == null)
			{
				throw new InvalidOperationException($"The {nameof(StaticMediaOpsLiveCache)} instance has not been created yet. Please call {nameof(GetOrCreate)} first.");
			}

			return _instance;
		}

		/// <summary>
		/// Resets the singleton instance, disposing of the existing instance if necessary.
		/// For testing purposes only.
		/// </summary>
		internal static void Reset()
		{
			// Replace the instance with null in a thread-safe manner
			var oldInstance = Interlocked.Exchange(ref _instance, null);

			// Dispose the old instance if it exists
			oldInstance?.Dispose();
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			if (_lazyConnectionMonitor.IsValueCreated)
			{
				_lazyConnectionMonitor.Value.Dispose();
			}

			if (_lazyConnectivityInfoProvider.IsValueCreated)
			{
				_lazyConnectivityInfoProvider.Value.Dispose();
			}

			if (_lazyLiteConnectivityInfoProvider.IsValueCreated)
			{
				_lazyLiteConnectivityInfoProvider.Value.Dispose();
			}

			if (_lazyLevelsCache.IsValueCreated)
			{
				_lazyLevelsCache.Value.Dispose();
			}

			if (_lazyVirtualSignalGroupsCache.IsValueCreated)
			{
				_lazyVirtualSignalGroupsCache.Value.Dispose();
			}

			if (Connection is IDisposable disposableConnection)
			{
				disposableConnection.Dispose();
			}

			_disposed = true;
		}

		private static IConnection CloneConnection(IConnection baseConnection)
		{
			if (baseConnection.GetType().FullName == "Skyline.DataMiner.MediaOps.Live.UnitTesting.SLNetConnectionMock")
			{
				// If the connection is a mock connection used for unit testing, use the existing connection directly.
				// Such connection cannot be cloned.
				return baseConnection;
			}

			if (ConnectionHelper.IsManagedDataMinerModule(baseConnection))
			{
				// If the connection is a managed DataMiner module (e.g. Engine.SLNetRaw), use the existing connection directly.
				return baseConnection;
			}
			else if (ConnectionHelper.TryCloneConnection(baseConnection, "MediaOps.Live - Connection", out var clonedConnection))
			{
				return clonedConnection;
			}
			else
			{
				throw new InvalidOperationException("Failed to create a connection.");
			}
		}
	}
}
