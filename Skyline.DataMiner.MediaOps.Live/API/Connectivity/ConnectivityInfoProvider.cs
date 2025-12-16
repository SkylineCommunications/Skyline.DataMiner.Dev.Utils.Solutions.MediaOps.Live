namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;

	public sealed class ConnectivityInfoProvider : IDisposable
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<Endpoint>, EndpointConnectivity> _endpointConnectivityCache = new();
		private readonly Dictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroupConnectivity> _vsgConnectivityCache = new();

		private VirtualSignalGroupEndpointsObserver _vsgObserver;
		private VirtualSignalGroupEndpointsCache _vsgCache;
		private bool _ownsVsgObserver;

		private LevelsObserver _levelsObserver;
		private LevelsCache _levelsCache;
		private bool _ownsLevelsObserver;

		private LiteConnectivityInfoProvider _liteConnectivityInfoProvider;
		private bool _ownsLiteConnectivityInfoProvider;

		private bool _isDisposed;

		public ConnectivityInfoProvider(
			MediaOpsLiveApi api,
			LiteConnectivityInfoProvider liteConnectivityInfoProvider = null,
			VirtualSignalGroupEndpointsObserver virtualSignalGroupsObserver = null,
			LevelsObserver levelsObserver = null,
			bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));

			Initialize(liteConnectivityInfoProvider, virtualSignalGroupsObserver, levelsObserver, subscribe);
		}

		public event EventHandler<ConnectionsUpdatedEvent> ConnectionsUpdated;

		internal MediaOpsLiveApi Api { get; }

		public bool IsSubscribed { get; private set; }

		public bool IsConnected(ApiObjectReference<Endpoint> endpoint)
		{
			if (endpoint == ApiObjectReference<Endpoint>.Empty)
			{
				return false;
			}

			lock (_lock)
			{
				return _liteConnectivityInfoProvider.IsConnected(endpoint);
			}
		}

		public bool IsConnected(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination)
		{
			if (source == ApiObjectReference<Endpoint>.Empty || destination == ApiObjectReference<Endpoint>.Empty)
			{
				return false;
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
				if (!_endpointConnectivityCache.TryGetValue(endpoint, out var connectivity))
				{
					connectivity = BuildEndpointConnectivity(endpoint);
					_endpointConnectivityCache[endpoint] = connectivity;
				}

				return connectivity;
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
				if (!_vsgConnectivityCache.TryGetValue(virtualSignalGroup, out var connectivity))
				{
					connectivity = BuildVirtualSignalGroupConnectivity(virtualSignalGroup);
					_vsgConnectivityCache[virtualSignalGroup] = connectivity;
				}

				return connectivity;
			}
		}

		public VirtualSignalGroupConnectivity GetConnectivity(ApiObjectReference<VirtualSignalGroup> virtualSignalGroupRef)
		{
			if (virtualSignalGroupRef == ApiObjectReference<VirtualSignalGroup>.Empty)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroupRef));
			}

			lock (_lock)
			{
				if (!_vsgCache.TryGetVirtualSignalGroup(virtualSignalGroupRef, out var virtualSignalGroup))
				{
					throw new InvalidOperationException($"VSG {virtualSignalGroupRef.ID} not found");
				}

				return GetConnectivity(virtualSignalGroup);
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

				_levelsObserver.LevelsChanged += LevelsObserver_LevelsChanged;
				_vsgObserver.VirtualSignalGroupsChanged += VsgObserver_VirtualSignalGroupsChanged;
				_liteConnectivityInfoProvider.EndpointsImpacted += Endpoints_Impacted;

				if (_ownsLevelsObserver)
					_levelsObserver.Subscribe();

				if (_ownsVsgObserver)
					_vsgObserver.Subscribe();

				if (_ownsLiteConnectivityInfoProvider)
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

				_levelsObserver.LevelsChanged -= LevelsObserver_LevelsChanged;
				_vsgObserver.VirtualSignalGroupsChanged -= VsgObserver_VirtualSignalGroupsChanged;
				_liteConnectivityInfoProvider.EndpointsImpacted -= Endpoints_Impacted;

				if (_ownsLevelsObserver)
					_levelsObserver.Unsubscribe();

				if (_ownsVsgObserver)
					_vsgObserver.Unsubscribe();

				if (_ownsLiteConnectivityInfoProvider)
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

			Unsubscribe();

			if (_ownsLevelsObserver)
			{
				_levelsObserver?.Dispose();
				_levelsObserver = null;
			}

			if (_ownsVsgObserver)
			{
				_vsgObserver?.Dispose();
				_vsgObserver = null;
			}

			if (_ownsLiteConnectivityInfoProvider)
			{
				_liteConnectivityInfoProvider?.Dispose();
				_liteConnectivityInfoProvider = null;
			}

			_isDisposed = true;
		}

		private void Initialize(
			LiteConnectivityInfoProvider liteConnectivityInfoProvider,
			VirtualSignalGroupEndpointsObserver virtualSignalGroupsObserver,
			LevelsObserver levelsObserver,
			bool subscribe)
		{
			lock (_lock)
			{
				if (liteConnectivityInfoProvider == null)
				{
					liteConnectivityInfoProvider = new LiteConnectivityInfoProvider(Api, subscribe);
					_ownsLiteConnectivityInfoProvider = true;
				}

				if (virtualSignalGroupsObserver == null)
				{
					virtualSignalGroupsObserver = new VirtualSignalGroupEndpointsObserver(Api);
					_ownsVsgObserver = true;
				}

				if (levelsObserver == null)
				{
					levelsObserver = new LevelsObserver(Api);
					_ownsLevelsObserver = true;
				}

				_liteConnectivityInfoProvider = liteConnectivityInfoProvider;

				_levelsObserver = levelsObserver;
				_levelsCache = levelsObserver.Cache;

				_vsgObserver = virtualSignalGroupsObserver;
				_vsgCache = virtualSignalGroupsObserver.Cache;

				if (subscribe)
				{
					Subscribe();
				}

				if (_ownsLevelsObserver)
					_levelsObserver.LoadInitialData();

				if (_ownsVsgObserver)
					_vsgObserver.LoadInitialData();
			}
		}

		private void Endpoints_Impacted(object sender, ICollection<ApiObjectReference<Endpoint>> impactedEndpoints)
		{
			Debug.WriteLine($"Endpoints impacted: {String.Join(", ", impactedEndpoints)}");

			RaiseConnectionsUpdated(impactedEndpoints);
		}

		private void VsgObserver_VirtualSignalGroupsChanged(object sender, ApiObjectsChangedEvent<VirtualSignalGroup> e)
		{
			Debug.WriteLine($"VSGs changed: Created={String.Join(", ", e.Created)}, Updated={String.Join(", ", e.Updated)}, Deleted={String.Join(", ", e.Deleted)}");

			var allUpdatedVirtualSignalGroups = e.Created.Concat(e.Updated).Concat(e.Deleted)
				.Select(x => x.Reference)
				.ToList();

			RaiseConnectionsUpdated(allUpdatedVirtualSignalGroups);
		}

		private void LevelsObserver_LevelsChanged(object sender, ApiObjectsChangedEvent<Level> e)
		{
			Debug.WriteLine($"Levels changed: Created={String.Join(", ", e.Created)}, Updated={String.Join(", ", e.Updated)}, Deleted={String.Join(", ", e.Deleted)}");

			var allUpdatedLevels = e.Created.Concat(e.Updated).Concat(e.Deleted)
				.ToList();

			ICollection<ApiObjectReference<VirtualSignalGroup>> impactedVirtualSignalGroups;

			lock (_lock)
			{
				// gather all virtual signal groups that contain any of the updated levels
				impactedVirtualSignalGroups = _vsgCache.VirtualSignalGroups
					.GetAllVirtualSignalGroups()
					.Where(vsg => allUpdatedLevels.Any(level => vsg.ContainsLevel(level)))
					.Select(x => x.Reference)
					.Distinct()
					.ToList();
			}

			// Make sure to raise the event outside the lock
			RaiseConnectionsUpdated(impactedVirtualSignalGroups);
		}

		private void RaiseConnectionsUpdated(ICollection<ApiObjectReference<Endpoint>> impactedEndpoints)
		{
			if (impactedEndpoints.Count == 0)
			{
				return;
			}

			var context = new InvalidationContext();

			lock (_lock)
			{
				foreach (var endpointRef in impactedEndpoints)
				{
					if (_vsgCache.TryGetEndpoint(endpointRef, out var endpoint))
					{
						InvalidateConnectivity(endpoint, context);
					}
				}
			}

			// Invoke event outside lock to prevent potential deadlocks
			RaiseConnectionsUpdated(context);
		}

		private void RaiseConnectionsUpdated(ICollection<ApiObjectReference<VirtualSignalGroup>> impactedVirtualSignalGroups)
		{
			if (impactedVirtualSignalGroups.Count == 0)
			{
				return;
			}

			var context = new InvalidationContext();

			lock (_lock)
			{
				foreach (var vsgRef in impactedVirtualSignalGroups)
				{
					if (_vsgCache.TryGetVirtualSignalGroup(vsgRef, out var virtualSignalGroup))
					{
						InvalidateConnectivity(virtualSignalGroup, context);
					}
				}
			}

			// Invoke event outside lock to prevent potential deadlocks
			RaiseConnectionsUpdated(context);
		}

		private void RaiseConnectionsUpdated(InvalidationContext context)
		{
			if (!context.HasChanges)
			{
				return;
			}

			// Ensure we are not holding the lock when raising events
			// This could lead to deadlocks if event handlers try to call back into this class
			Debug.Assert(!Monitor.IsEntered(_lock), "Lock must not be held when raising events to prevent deadlocks from event handlers calling back into this class");

			var eventArgs = new ConnectionsUpdatedEvent(
				context.ChangedEndpoints.Values.Select(x => x.New),
				context.ChangedVirtualSignalGroups.Values.Select(x => x.New));

			ConnectionsUpdated?.Invoke(this, eventArgs);
		}

		private void InvalidateConnectivity(Endpoint endpoint, InvalidationContext context)
		{
			if (!context.VisitedEndpoints.Add(endpoint))
			{
				return;
			}

			var newConnectivity = BuildEndpointConnectivity(endpoint);

			if (_endpointConnectivityCache.TryGetValue(endpoint, out var oldConnectivity) &&
				oldConnectivity == newConnectivity)
			{
				// No changes, exit early
				return;
			}

			// Update cache and add to list
			_endpointConnectivityCache[endpoint] = newConnectivity;
			context.ChangedEndpoints[endpoint] = (oldConnectivity, newConnectivity);

			// Also invalidate all endpoints that are linked to this endpoint
			var linkedEndpoints = GetLinkedEndpoints(oldConnectivity)
				.Union(GetLinkedEndpoints(newConnectivity))
				.Where(x => x != endpoint);

			foreach (var linkedEndpoint in linkedEndpoints)
			{
				InvalidateConnectivity(linkedEndpoint, context);
			}

			// Invalidate all virtual signal groups that contain this endpoint
			var virtualSignalGroups = GetLinkedVirtualSignalGroups(oldConnectivity)
				.Union(GetLinkedVirtualSignalGroups(newConnectivity));

			foreach (var vsg in virtualSignalGroups)
			{
				InvalidateConnectivity(vsg, context);
			}
		}

		private void InvalidateConnectivity(VirtualSignalGroup virtualSignalGroup, InvalidationContext context)
		{
			if (!context.VisitedVirtualSignalGroups.Add(virtualSignalGroup))
			{
				return;
			}

			// First make sure all endpoints in this virtual signal group are up to date
			foreach (var levelEndpoint in virtualSignalGroup.GetLevelEndpoints())
			{
				if (_vsgCache.TryGetEndpoint(levelEndpoint.Endpoint, out var endpoint))
				{
					InvalidateConnectivity(endpoint, context);
				}
			}

			// Rebuild connectivity
			var newConnectivity = BuildVirtualSignalGroupConnectivity(virtualSignalGroup);

			if (_vsgConnectivityCache.TryGetValue(virtualSignalGroup, out var oldConnectivity) &&
				oldConnectivity == newConnectivity)
			{
				// No changes, exit early
				return;
			}

			// Update cache and add to list
			_vsgConnectivityCache[virtualSignalGroup] = newConnectivity;
			context.ChangedVirtualSignalGroups[virtualSignalGroup] = (oldConnectivity, newConnectivity);

			// Also invalidate all virtual signal groups that are linked to this virtual signal group
			var linkedVirtualSignalGroups = GetLinkedVirtualSignalGroups(oldConnectivity)
				.Union(GetLinkedVirtualSignalGroups(newConnectivity))
				.Where(x => x != virtualSignalGroup);

			foreach (var linkedVsg in linkedVirtualSignalGroups)
			{
				InvalidateConnectivity(linkedVsg, context);
			}
		}

		/// <summary>
		/// Gets all endpoints that are linked to the given connectivity.
		/// Linked endpoints are those that are connected to or from the endpoint in the connectivity.
		/// </summary>
		private IEnumerable<Endpoint> GetLinkedEndpoints(EndpointConnectivity connectivity)
		{
			if (connectivity == null)
			{
				return [];
			}

			var linkedEndpoints = new HashSet<Endpoint>();

			if (connectivity.ConnectedSource != null)
			{
				linkedEndpoints.Add(connectivity.ConnectedSource);
			}

			if (connectivity.PendingConnectedSource != null)
			{
				linkedEndpoints.Add(connectivity.PendingConnectedSource);
			}

			linkedEndpoints.UnionWith(connectivity.DestinationConnections.Select(x => x.Endpoint));

			return linkedEndpoints;
		}

		private IEnumerable<Endpoint> GetLinkedEndpoints(VirtualSignalGroupConnectivity connectivity)
		{
			if (connectivity == null)
			{
				return [];
			}

			return connectivity.Levels.Values.Select(x => x.Endpoint);
		}

		/// <summary>
		/// Gets all virtual signal groups that are linked to the given connectivity.
		/// </summary>
		private IEnumerable<VirtualSignalGroup> GetLinkedVirtualSignalGroups(EndpointConnectivity connectivity)
		{
			if (connectivity == null)
			{
				return [];
			}

			return connectivity.VirtualSignalGroups;
		}

		/// <summary>
		/// Gets all virtual signal groups that are linked to the given connectivity.
		/// Linked virtual signal groups are those that are connected to or from the virtual signal group in the connectivity.
		/// </summary>
		private IEnumerable<VirtualSignalGroup> GetLinkedVirtualSignalGroups(VirtualSignalGroupConnectivity connectivity)
		{
			if (connectivity == null)
			{
				return [];
			}

			var linkedVirtualSignalGroups = new HashSet<VirtualSignalGroup>();

			linkedVirtualSignalGroups.UnionWith(connectivity.ConnectedSources);
			linkedVirtualSignalGroups.UnionWith(connectivity.PendingConnectedSources);
			linkedVirtualSignalGroups.UnionWith(connectivity.ConnectedDestinations);
			linkedVirtualSignalGroups.UnionWith(connectivity.PendingConnectedDestinations);

			return linkedVirtualSignalGroups;
		}

		private EndpointConnectivity BuildEndpointConnectivity(Endpoint endpoint)
		{
			if (endpoint.IsSource)
			{
				return BuildEndpointConnectivityForSource(endpoint);
			}
			else if (endpoint.IsDestination)
			{
				return BuildEndpointConnectivityForDestination(endpoint);
			}
			else
			{
				throw new InvalidOperationException($"Endpoint has invalid role: {endpoint.Role}");
			}
		}

		private EndpointConnectivity BuildEndpointConnectivityForSource(Endpoint endpoint)
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
					virtualSignalGroups,
					isConnected,
					isConnecting,
					isDisconnecting,
					connectedSource: null,
					pendingConnectedSource: null,
					destinationConnections);
			}
		}

		private EndpointConnectivity BuildEndpointConnectivityForDestination(Endpoint endpoint)
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
					virtualSignalGroups,
					isConnected,
					isConnecting,
					isDisconnecting,
					connectedSource,
					pendingConnectedSource,
					destinationConnections: null);
			}
		}

		private VirtualSignalGroupConnectivity BuildVirtualSignalGroupConnectivity(VirtualSignalGroup virtualSignalGroup)
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

				var endpointConnectivity = GetConnectivity(endpoint);

				levelsConnectivity[level] = endpointConnectivity;

				if (endpointConnectivity.ConnectedSource != null)
				{
					var virtualSignalGroups = _vsgCache.GetVirtualSignalGroupsThatContainEndpoint(endpointConnectivity.ConnectedSource);
					connectedSources.UnionWith(virtualSignalGroups);
				}

				if (endpointConnectivity.PendingConnectedSource != null)
				{
					var virtualSignalGroups = _vsgCache.GetVirtualSignalGroupsThatContainEndpoint(endpointConnectivity.PendingConnectedSource);
					pendingConnectedSources.UnionWith(virtualSignalGroups);
				}

				var connectedVsgs = endpointConnectivity.ConnectedDestinations.SelectMany(x => _vsgCache.GetVirtualSignalGroupsThatContainEndpoint(x));
				connectedDestinations.UnionWith(connectedVsgs);

				var pendingVsgs = endpointConnectivity.PendingConnectedDestinations.SelectMany(x => _vsgCache.GetVirtualSignalGroupsThatContainEndpoint(x));
				pendingConnectedDestinations.UnionWith(pendingVsgs);
			}

			return new VirtualSignalGroupConnectivity(
				virtualSignalGroup,
				levelsConnectivity,
				connectedSources,
				pendingConnectedSources,
				connectedDestinations,
				pendingConnectedDestinations);
		}

		private sealed class InvalidationContext
		{
			public HashSet<Endpoint> VisitedEndpoints { get; } = new();

			public HashSet<VirtualSignalGroup> VisitedVirtualSignalGroups { get; } = new();

			public Dictionary<Endpoint, (EndpointConnectivity Old, EndpointConnectivity New)> ChangedEndpoints { get; } = new();

			public Dictionary<VirtualSignalGroup, (VirtualSignalGroupConnectivity Old, VirtualSignalGroupConnectivity New)> ChangedVirtualSignalGroups { get; } = new();

			public bool HasChanges =>
				ChangedEndpoints.Count > 0 ||
				ChangedVirtualSignalGroups.Count > 0;
		}
	}
}
