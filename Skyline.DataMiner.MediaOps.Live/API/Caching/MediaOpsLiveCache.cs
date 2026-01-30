namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching
{
	using System;
	using System.Threading;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Tools;

	public sealed class MediaOpsLiveCache : IDisposable
	{
		private static readonly object _lock = new();
		private static volatile MediaOpsLiveCache _instance;

		private readonly Lazy<VirtualSignalGroupEndpointsObserver> _lazyVirtualSignalGroupsObserver;
		private readonly Lazy<LevelsObserver> _lazyLevelsObserver;
		private readonly Lazy<TransportTypesObserver> _lazyTransportTypesObserver;

		private readonly Lazy<LiteConnectivityInfoProvider> _lazyLiteConnectivityInfoProvider;
		private readonly Lazy<ConnectivityInfoProvider> _lazyConnectivityInfoProvider;
		private readonly Lazy<ConnectionMonitor> _lazyConnectionMonitor;

		private bool _disposed;

		private MediaOpsLiveCache(IConnection connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));

			Api = new MediaOpsLiveApi(Connection);

			_lazyLevelsObserver = new(CreateLevelsObserver);
			_lazyTransportTypesObserver = new(CreateTransportTypesObserver);
			_lazyVirtualSignalGroupsObserver = new(CreateVirtualSignalGroupsObserver);

			_lazyLiteConnectivityInfoProvider = new(() => new LiteConnectivityInfoProvider(Api, subscribe: true));
			_lazyConnectivityInfoProvider = new(() => new ConnectivityInfoProvider(Api, LiteConnectivityInfoProvider, VirtualSignalGroupEndpointsObserver, LevelsObserver, subscribe: true));
			_lazyConnectionMonitor = new(() => new ConnectionMonitor(Api, LiteConnectivityInfoProvider));
		}

		private IConnection Connection { get; }

		private MediaOpsLiveApi Api { get; }

		public VirtualSignalGroupEndpointsObserver VirtualSignalGroupEndpointsObserver => _lazyVirtualSignalGroupsObserver.Value;

		public VirtualSignalGroupEndpointsCache VirtualSignalGroupEndpointsCache => VirtualSignalGroupEndpointsObserver.Cache;

		public LevelsObserver LevelsObserver => _lazyLevelsObserver.Value;

		public LevelsCache LevelsCache => LevelsObserver.Cache;

		public TransportTypesObserver TransportTypesObserver => _lazyTransportTypesObserver.Value;

		public TransportTypesCache TransportTypesCache => TransportTypesObserver.Cache;

		public LiteConnectivityInfoProvider LiteConnectivityInfoProvider => _lazyLiteConnectivityInfoProvider.Value;

		public ConnectivityInfoProvider ConnectivityInfoProvider => _lazyConnectivityInfoProvider.Value;

		public ConnectionMonitor ConnectionMonitor => _lazyConnectionMonitor.Value;

		public static MediaOpsLiveCache GetOrCreate(Func<IConnection> connectionFactory)
		{
			if (connectionFactory == null)
			{
				throw new ArgumentNullException(nameof(connectionFactory));
			}

			if (_instance == null)
			{
				lock (_lock)
				{
					if (_instance == null)
					{
						var connection = connectionFactory();
						_instance = GetOrCreate(connection);
					}
				}
			}

			return _instance;
		}

		public static MediaOpsLiveCache GetOrCreate(IConnection baseConnection)
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
						// Always clone the connection to ensure that the MediaOpsLiveCache has its own dedicated connection.
						// This prevents potential conflicts when the base connection would be closed or unsubscribed elsewhere.
						var connection = CloneConnection(baseConnection);

						_instance = new MediaOpsLiveCache(connection);
					}
				}
			}

			return _instance;
		}

		public static MediaOpsLiveCache Get()
		{
			lock (_lock)
			{
				if (_instance == null)
				{
					throw new InvalidOperationException($"The {nameof(MediaOpsLiveCache)} instance has not been created yet. Please call {nameof(GetOrCreate)} first.");
				}

				return _instance;
			}
		}

		/// <summary>
		/// Resets the singleton instance, disposing of the existing instance if necessary.
		/// For testing purposes only.
		/// </summary>
		public static void Reset()
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

			if (_lazyTransportTypesObserver.IsValueCreated)
			{
				_lazyTransportTypesObserver.Value.Dispose();
			}

			if (_lazyLevelsObserver.IsValueCreated)
			{
				_lazyLevelsObserver.Value.Dispose();
			}

			if (_lazyVirtualSignalGroupsObserver.IsValueCreated)
			{
				_lazyVirtualSignalGroupsObserver.Value.Dispose();
			}

			_disposed = true;
		}

		private VirtualSignalGroupEndpointsObserver CreateVirtualSignalGroupsObserver()
		{
			var observer = new VirtualSignalGroupEndpointsObserver(Api);
			observer.Subscribe();
			observer.LoadInitialData();
			return observer;
		}

		private LevelsObserver CreateLevelsObserver()
		{
			var observer = new LevelsObserver(Api);
			observer.Subscribe();
			observer.LoadInitialData();
			return observer;
		}

		private TransportTypesObserver CreateTransportTypesObserver()
		{
			var observer = new TransportTypesObserver(Api);
			observer.Subscribe();
			observer.LoadInitialData();
			return observer;
		}

		private static IConnection CloneConnection(IConnection baseConnection)
		{
			Exception exception;

			try
			{
				if (baseConnection.GetType().FullName == "Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting.SLNetConnectionMock")
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

				if (ConnectionHelper.TryCloneConnection(baseConnection, "MediaOps.Live - Connection", out var clonedConnection, out exception))
				{
					return clonedConnection;
				}
			}
			catch (Exception ex)
			{
				exception = ex;
			}

			throw new InvalidOperationException("Failed to clone the provided connection.", exception);
		}
	}
}
