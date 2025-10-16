namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;

	public sealed class ConnectivityInfoProvider : IDisposable
	{
		private readonly object _lock = new();

		private VirtualSignalGroupEndpointsCache _vsgCache;
		private LevelsCache _levelsCache;
		private LiteConnectivityInfoProvider _liteConnectivityInfoProvider;

		private bool _isDisposed;

		public ConnectivityInfoProvider(
			MediaOpsLiveApi api,
			LiteConnectivityInfoProvider liteConnectivityInfoProvider = null,
			VirtualSignalGroupEndpointsCache virtualSignalGroupsCache = null,
			LevelsCache levelsCache = null,
			bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));

			Initialize(liteConnectivityInfoProvider, virtualSignalGroupsCache, levelsCache, subscribe);
		}

		public event EventHandler<ConnectionsUpdatedEvent> ConnectionsUpdated;

		internal MediaOpsLiveApi Api { get; }

		public bool IsSubscribed { get; private set; }

		public bool IsConnected(ApiObjectReference<Endpoint> endpoint)
		{
			if (endpoint == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			lock (_lock)
			{
				return _liteConnectivityInfoProvider.IsConnected(endpoint);
			}
		}

		public bool IsConnected(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination)
		{
			if (source == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			lock (_lock)
			{
				return _liteConnectivityInfoProvider.IsConnected(source, destination);
			}
		}

		public ConnectionState IsConnected(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			lock (_lock)
			{
				bool anyConnected = false;
				bool anyDisconnected = false;

				foreach (var levelEndpoint in virtualSignalGroup.GetLevelEndpoints())
				{
					if (IsConnected(levelEndpoint.Endpoint))
						anyConnected = true;
					else
						anyDisconnected = true;

					// Early exit optimization: if both true, partial state confirmed
					if (anyConnected && anyDisconnected)
						return ConnectionState.Partial;
				}

				return anyConnected ? ConnectionState.Connected : ConnectionState.Disconnected;
			}
		}

		public IReadOnlyDictionary<Endpoint, bool> IsConnected(ICollection<Endpoint> endpoints)
		{
			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			lock (_lock)
			{
				return endpoints.ToDictionary(x => x, x => IsConnected(x));
			}
		}

		public IReadOnlyDictionary<VirtualSignalGroup, ConnectionState> IsConnected(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			lock (_lock)
			{
				return virtualSignalGroups.ToDictionary(x => x, IsConnected);
			}
		}

		public EndpointConnectivity GetConnectivity(Endpoint endpoint)
		{
			if (endpoint == null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			lock (_lock)
			{
				if (endpoint.IsDestination)
				{
					return GetConnectivityForDestination(endpoint);
				}
				else if (endpoint.IsSource)
				{
					return GetConnectivityForSource(endpoint);
				}
				else
				{
					throw new InvalidOperationException($"Endpoint has invalid role: {endpoint.Role}");
				}
			}
		}

		public EndpointConnectivity GetConnectivity(ApiObjectReference<Endpoint> endpointRef)
		{
			if (endpointRef == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentNullException(nameof(endpointRef));
			}

			lock (_lock)
			{
				if (!_vsgCache.TryGetEndpoint(endpointRef, out var endpoint))
				{
					throw new InvalidOperationException($"Endpoint {endpointRef.ID} not found");
				}

				return GetConnectivity(endpoint);
			}
		}

		public VirtualSignalGroupConnectivity GetConnectivity(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup == null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			lock (_lock)
			{
				var levelsConnectivity = new Dictionary<Level, EndpointConnectivity>();
				var connectedSources = new HashSet<VirtualSignalGroup>();
				var pendingConnectedSources = new HashSet<VirtualSignalGroup>();
				var connectedDestinations = new HashSet<VirtualSignalGroup>();
				var pendingConnectedDestinations = new HashSet<VirtualSignalGroup>();

				foreach (var levelEndpoint in virtualSignalGroup.GetLevelEndpoints())
				{
					if (!_vsgCache.TryGetEndpoint(levelEndpoint.Endpoint, out var endpoint))
					{
						throw new InvalidOperationException($"Endpoint {levelEndpoint.Endpoint.ID} not found for virtual signal group '{virtualSignalGroup.Name}'");
					}

					if (!_levelsCache.TryGetLevel(levelEndpoint.Level, out var level))
					{
						throw new InvalidOperationException($"Level {levelEndpoint.Level.ID} not found for virtual signal group '{virtualSignalGroup.Name}'");
					}

					var connectivity = GetConnectivity(endpoint);

					levelsConnectivity[level] = connectivity;

					if (connectivity.ConnectedSource != null)
					{
						var virtualSignalGroups = _vsgCache.GetVirtualSignalGroupsThatContainEndpoint(connectivity.ConnectedSource);
						connectedSources.UnionWith(virtualSignalGroups);
					}

					if (connectivity.PendingConnectedSource != null)
					{
						var virtualSignalGroups = _vsgCache.GetVirtualSignalGroupsThatContainEndpoint(connectivity.PendingConnectedSource);
						pendingConnectedSources.UnionWith(virtualSignalGroups);
					}

					if (connectivity.ConnectedDestinations.Any())
					{
						var virtualSignalGroups = connectivity.ConnectedDestinations.SelectMany(x => _vsgCache.GetVirtualSignalGroupsThatContainEndpoint(x));
						connectedDestinations.UnionWith(virtualSignalGroups);
					}

					if (connectivity.PendingConnectedDestinations.Any())
					{
						var virtualSignalGroups = connectivity.PendingConnectedDestinations.SelectMany(x => _vsgCache.GetVirtualSignalGroupsThatContainEndpoint(x));
						pendingConnectedDestinations.UnionWith(virtualSignalGroups);
					}
				}

				return new VirtualSignalGroupConnectivity(
					virtualSignalGroup,
					levelsConnectivity,
					connectedSources,
					pendingConnectedSources,
					connectedDestinations,
					pendingConnectedDestinations);
			}
		}

		public IReadOnlyDictionary<Endpoint, EndpointConnectivity> GetConnectivity(ICollection<Endpoint> endpoints)
		{
			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			lock (_lock)
			{
				return endpoints.ToDictionary(x => x, GetConnectivity);
			}
		}

		public IReadOnlyDictionary<Endpoint, EndpointConnectivity> GetConnectivity(ICollection<ApiObjectReference<Endpoint>> endpoints)
		{
			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			lock (_lock)
			{
				return endpoints
					.Select(GetConnectivity)
					.ToDictionary(x => x.Endpoint, x => x);
			}
		}

		public IReadOnlyDictionary<VirtualSignalGroup, VirtualSignalGroupConnectivity> GetConnectivity(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			lock (_lock)
			{
				return virtualSignalGroups.ToDictionary(x => x, GetConnectivity);
			}
		}

		public void Subscribe()
		{
			lock (_lock)
			{
				if (IsSubscribed)
				{
					return;
				}

				_vsgCache.Subscribe();
				_liteConnectivityInfoProvider.Subscribe();

				IsSubscribed = true;
			}
		}

		public void Unsubscribe()
		{
			lock (_lock)
			{
				if (!IsSubscribed)
				{
					return;
				}

				_vsgCache.Unsubscribe();
				_liteConnectivityInfoProvider.Unsubscribe();

				IsSubscribed = false;
			}
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_liteConnectivityInfoProvider.EndpointsImpacted -= Endpoints_Impacted;
			Unsubscribe();

			_isDisposed = true;
		}

		private void Initialize(LiteConnectivityInfoProvider liteConnectivityInfoProvider, VirtualSignalGroupEndpointsCache virtualSignalGroupsCache, LevelsCache levelsCache, bool subscribe)
		{
			lock (_lock)
			{
				_levelsCache = levelsCache ?? new LevelsCache(Api, subscribe);

				_vsgCache = virtualSignalGroupsCache ?? new VirtualSignalGroupEndpointsCache(Api, subscribe);

				_liteConnectivityInfoProvider = liteConnectivityInfoProvider ?? new LiteConnectivityInfoProvider(Api, subscribe);
				_liteConnectivityInfoProvider.EndpointsImpacted += Endpoints_Impacted;

				IsSubscribed = subscribe;
			}
		}

		private void Endpoints_Impacted(object sender, ICollection<ApiObjectReference<Endpoint>> impactedEndpoints)
		{
			Debug.WriteLine($"Endpoints impacted: {String.Join(", ", impactedEndpoints)}");

			RaiseConnectionsUpdated(impactedEndpoints);
		}

		private void RaiseConnectionsUpdated(ICollection<ApiObjectReference<Endpoint>> impactedEndpoints)
		{
			if (impactedEndpoints.Count <= 0)
			{
				return;
			}

			ConnectionsUpdatedEvent eventArgs;

			lock (_lock)
			{
				var impactedVirtualSignalGroups = impactedEndpoints
					.SelectMany(x => _vsgCache.GetVirtualSignalGroupsThatContainEndpoint(x))
					.Distinct()
					.ToList();

				eventArgs = new ConnectionsUpdatedEvent(
					impactedEndpoints.Select(GetConnectivity).ToList(),
					impactedVirtualSignalGroups.Select(GetConnectivity).ToList());
			}

			// Invoke event outside lock to prevent potential deadlocks
			// This could happen when event handlers try to call back into this class
			ConnectionsUpdated?.Invoke(this, eventArgs);
		}

		private EndpointConnectivity GetConnectivityForDestination(Endpoint endpoint)
		{
			if (endpoint == null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			if (!endpoint.IsDestination)
			{
				throw new ArgumentException("Endpoint must be a destination endpoint.", nameof(endpoint));
			}

			lock (_lock)
			{
				bool isConnected = false;
				bool isConnecting = false;
				bool isDisconnecting = false;
				Endpoint connectedSource = null;
				Endpoint pendingConnectedSource = null;

				if (_liteConnectivityInfoProvider.TryGetConnectionForDestination(endpoint, out var connection) &&
					connection.IsConnected)
				{
					isConnected = true;

					if (connection.ConnectedSource.HasValue &&
						_vsgCache.TryGetEndpoint(connection.ConnectedSource.Value, out Endpoint connectedSourceEndpoint))
					{
						connectedSource = connectedSourceEndpoint;
					}
				}

				if (_liteConnectivityInfoProvider.TryGetPendingConnectionActionForDestination(endpoint, out var pendingAction))
				{
					if (pendingAction.Action == PendingConnectionActionType.Connect)
					{
						if (pendingAction.PendingSource.HasValue &&
							_vsgCache.TryGetEndpoint(pendingAction.PendingSource.Value, out var pendingSource) &&
							pendingSource != connectedSource)
						{
							isConnecting = true;
							pendingConnectedSource = pendingSource;
						}
					}
					else if (pendingAction.Action == PendingConnectionActionType.Disconnect)
					{
						if (isConnected)
						{
							isDisconnecting = true;
						}
					}
				}

				var virtualSignalGroups = _vsgCache.GetVirtualSignalGroupsThatContainEndpoint(endpoint);

				return new EndpointConnectivity(
					endpoint,
					isConnected,
					isConnecting,
					isDisconnecting,
					connectedSource,
					pendingConnectedSource,
					virtualSignalGroups,
					destinationConnections: null);
			}
		}

		private EndpointConnectivity GetConnectivityForSource(Endpoint endpoint)
		{
			if (endpoint == null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			if (!endpoint.IsSource)
			{
				throw new ArgumentException("Endpoint must be a source endpoint.", nameof(endpoint));
			}

			lock (_lock)
			{
				var isConnected = false;
				var isConnecting = false;
				var isDisconnecting = false;
				var destinationStates = new Dictionary<Endpoint, EndpointConnectionState>();

				var connections = _liteConnectivityInfoProvider.GetConnectionsWithSource(endpoint);
				var pendingActions = _liteConnectivityInfoProvider.GetPendingConnectionActionsWithSource(endpoint);

				foreach (var connection in connections.Where(c => c.IsConnected))
				{
					isConnected = true;

					if (_vsgCache.TryGetEndpoint(connection.Destination, out var destination))
					{
						destinationStates[destination] = EndpointConnectionState.Connected;
					}
				}

				foreach (var pendingAction in pendingActions)
				{
					if (!_vsgCache.TryGetEndpoint(pendingAction.Destination, out var destination))
					{
						continue;
					}

					if (pendingAction.Action == PendingConnectionActionType.Connect &&
						!_liteConnectivityInfoProvider.IsConnected(endpoint, destination))
					{
						isConnecting = true;
						destinationStates[destination] = EndpointConnectionState.Connecting;
					}
					else if (pendingAction.Action == PendingConnectionActionType.Disconnect &&
						_liteConnectivityInfoProvider.IsConnected(endpoint, destination))
					{
						isDisconnecting = true;
						destinationStates[destination] = EndpointConnectionState.Disconnecting;
					}
				}

				var virtualSignalGroups = _vsgCache.GetVirtualSignalGroupsThatContainEndpoint(endpoint);
				var destinationConnections = destinationStates.Select(x => new EndpointConnection(x.Key, x.Value)).ToList();

				return new EndpointConnectivity(
					endpoint,
					isConnected,
					isConnecting,
					isDisconnecting,
					connectedSource: null,
					pendingConnectedSource: null,
					virtualSignalGroups,
					destinationConnections);
			}
		}
	}
}
