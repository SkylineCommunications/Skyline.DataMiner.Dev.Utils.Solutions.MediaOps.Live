namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;

	using Skyline.DataMiner.MediaOps.Live.API;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;

	public sealed class ConnectivityInfoProvider : IDisposable
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<Endpoint>, Endpoint> _endpoints = new();
		private readonly Dictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> _virtualSignalGroups = new();

		private readonly VirtualSignalGroupEndpointsMapping _virtualSignalGroupEndpointsMapping = new();
		private readonly ConnectionEndpointsMapping _connectionEndpointsMapping = new();
		private readonly PendingConnectionActionMapping _pendingConnectionActionsMapping = new();

		private readonly ICollection<ConnectionSubscription> _connectionSubscriptions = [];
		private readonly ICollection<PendingConnectionActionSubscription> _pendingActionSubscriptions = [];

		private RepositorySubscription<Endpoint> _subscriptionEndpoints;
		private RepositorySubscription<VirtualSignalGroup> _subscriptionVirtualSignalGroups;

		public ConnectivityInfoProvider(MediaOpsLiveApi api, bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));

			Initialize(subscribe);
		}

		public event EventHandler<ConnectionsUpdatedEvent> ConnectionsUpdated;

		public MediaOpsLiveApi Api { get; }

		public bool IsSubscribed { get; private set; }

		public bool IsConnected(Endpoint endpoint)
		{
			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			lock (_lock)
			{
				LoadData(endpoint);

				return _connectionEndpointsMapping.GetConnections(endpoint).Any(c => c.IsConnected);
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
				LoadData(virtualSignalGroup);

				bool anyConnected = false;
				bool anyDisconnected = false;

				foreach (var levelEndpoint in virtualSignalGroup.GetLevelEndpoints())
				{
					var endpoint = _endpoints[levelEndpoint.Endpoint];

					if (IsConnected(endpoint))
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
				LoadData(endpoints.Select(x => x.Reference));

				return endpoints.ToDictionary(x => x, IsConnected);
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
				LoadData(virtualSignalGroups.Select(x => x.Reference));

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
				LoadData(endpoint);

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
				LoadData(endpointRef);

				if (!_endpoints.TryGetValue(endpointRef, out var endpoint))
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
				LoadData(virtualSignalGroup);

				var levelsConnectivity = new Dictionary<ApiObjectReference<Level>, EndpointConnectivity>();
				var connectedSources = new HashSet<VirtualSignalGroup>();
				var pendingConnectedSources = new HashSet<VirtualSignalGroup>();
				var connectedDestinations = new HashSet<VirtualSignalGroup>();
				var pendingConnectedDestinations = new HashSet<VirtualSignalGroup>();

				foreach (var levelEndpoint in virtualSignalGroup.GetLevelEndpoints())
				{
					if (!_endpoints.TryGetValue(levelEndpoint.Endpoint, out var endpoint))
					{
						throw new InvalidOperationException($"Endpoint {levelEndpoint.Endpoint.ID} not found for virtual signal group {virtualSignalGroup.ID}");
					}

					var connectivity = GetConnectivity(endpoint);

					levelsConnectivity[levelEndpoint.Level] = connectivity;

					if (connectivity.ConnectedSource != null)
					{
						var virtualSignalGroups = _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(connectivity.ConnectedSource);
						connectedSources.UnionWith(virtualSignalGroups);
					}

					if (connectivity.PendingConnectedSource != null)
					{
						var virtualSignalGroups = _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(connectivity.PendingConnectedSource);
						pendingConnectedSources.UnionWith(virtualSignalGroups);
					}

					if (connectivity.ConnectedDestinations.Any())
					{
						var virtualSignalGroups = connectivity.ConnectedDestinations.SelectMany(x => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(x));
						connectedDestinations.UnionWith(virtualSignalGroups);
					}

					if (connectivity.PendingConnectedDestinations.Any())
					{
						var virtualSignalGroups = connectivity.PendingConnectedDestinations.SelectMany(x => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(x));
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
				LoadData(endpoints.Select(x => x.Reference));

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
				LoadData(endpoints);

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
				LoadData(virtualSignalGroups.Select(x => x.Reference));

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

				_subscriptionEndpoints = Api.Endpoints.Subscribe();
				_subscriptionVirtualSignalGroups = Api.VirtualSignalGroups.Subscribe();

				_subscriptionEndpoints.Changed += Endpoints_Changed;
				_subscriptionVirtualSignalGroups.Changed += VirtualSignalGroups_Changed;

				foreach (var element in Api.MediationElements.AllElements)
				{
					var connectionSubscription = element.CreateConnectionSubscription();
					connectionSubscription.Changed += Connections_OnChanged;
					connectionSubscription.Subscribe();
					_connectionSubscriptions.Add(connectionSubscription);

					var pendingActionSubscription = element.CreatePendingActionSubscription();
					pendingActionSubscription.Changed += PendingConnectionActions_OnChanged;
					pendingActionSubscription.Subscribe();
					_pendingActionSubscriptions.Add(pendingActionSubscription);
				}

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

				_subscriptionEndpoints.Changed -= Endpoints_Changed;
				_subscriptionVirtualSignalGroups.Changed -= VirtualSignalGroups_Changed;

				_subscriptionEndpoints.Dispose();
				_subscriptionVirtualSignalGroups.Dispose();

				foreach (var subscription in _connectionSubscriptions)
				{
					subscription.Unsubscribe();
					subscription.Changed -= Connections_OnChanged;
				}

				foreach (var subscription in _pendingActionSubscriptions)
				{
					subscription.Unsubscribe();
					subscription.Changed -= PendingConnectionActions_OnChanged;
				}

				_connectionSubscriptions.Clear();
				_pendingActionSubscriptions.Clear();

				IsSubscribed = false;
			}
		}

		public void LoadData(params IEnumerable<ApiObjectReference<Endpoint>> endpoints)
		{
			lock (_lock)
			{
				var endpointIds = new HashSet<ApiObjectReference<Endpoint>>();

				// Extend the endpoint IDs with the endpoints in linked connections and pending actions
				foreach (var endpoint in endpoints)
				{
					endpointIds.Add(endpoint);

					var connections = _connectionEndpointsMapping.GetConnections(endpoint);
					var pendingActions = _pendingConnectionActionsMapping.GetPendingConnectionActions(endpoint);

					endpointIds.UnionWith(connections.SelectMany(x => x.GetEndpoints()));
					endpointIds.UnionWith(pendingActions.SelectMany(x => x.GetEndpoints()));
				}

				// Load the data for the endpoints
				LoadEndpoints(endpointIds);
				LoadVirtualSignalGroupsThatContainEndpoints(endpointIds);
			}
		}

		public void LoadData(params IEnumerable<ApiObjectReference<VirtualSignalGroup>> virtualSignalGroups)
		{
			lock (_lock)
			{
				LoadVirtualSignalGroups(virtualSignalGroups);

				var endpoints = new HashSet<ApiObjectReference<Endpoint>>();

				foreach (var virtualSignalGroupRef in virtualSignalGroups)
				{
					if (_virtualSignalGroups.TryGetValue(virtualSignalGroupRef, out var virtualSignalGroup))
					{
						endpoints.UnionWith(virtualSignalGroup.GetLevelEndpoints().Select(x => x.Endpoint));
					}
				}

				LoadData(endpoints);
			}
		}

		public void Dispose()
		{
			Unsubscribe();
		}

		private void Initialize(bool subscribe)
		{
			lock (_lock)
			{
				if (subscribe)
				{
					Subscribe();
				}

				LoadDataFromMediationElements();
			}
		}

		private void Endpoints_Changed(object sender, ApiObjectsChangedEvent<Endpoint> e)
		{
			lock (_lock)
			{
				Debug.WriteLine($"Endpoints changed: {e}");

				UpdateEndpoints(e.Created.Concat(e.Updated), e.Deleted);
			}
		}

		private void VirtualSignalGroups_Changed(object sender, ApiObjectsChangedEvent<VirtualSignalGroup> e)
		{
			lock (_lock)
			{
				Debug.WriteLine($"Virtual Signal Groups changed: {e}");

				UpdateVirtualSignalGroups(e.Created.Concat(e.Updated), e.Deleted);
			}
		}

		private void Connections_OnChanged(object sender, ConnectionsChangedEvent e)
		{
			lock (_lock)
			{
				Debug.WriteLine($"Connections changed: {e}");

				var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

				foreach (var connection in e.DeletedConnections)
				{
					impactedEndpoints.UnionWith(connection.GetEndpoints());
					_connectionEndpointsMapping.Remove(connection);
				}

				foreach (var connection in e.UpdatedConnections)
				{
					if (_connectionEndpointsMapping.TryGetConnectionForDestination(connection.Destination, out var existingConnection))
					{
						if (connection.IsConnected == existingConnection.IsConnected &&
							connection.ConnectedSource == existingConnection.ConnectedSource)
						{
							continue;
						}

						impactedEndpoints.UnionWith(existingConnection.GetEndpoints());
					}

					impactedEndpoints.UnionWith(connection.GetEndpoints());
					_connectionEndpointsMapping.AddOrUpdate(connection);
				}

				RaiseConnectionsUpdated(impactedEndpoints);
			}
		}

		private void PendingConnectionActions_OnChanged(object sender, PendingConnectionActionsChangedEvent e)
		{
			lock (_lock)
			{
				Debug.WriteLine($"Pending connection actions changed: {e}");

				var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

				foreach (var pendingAction in e.DeletedPendingActions)
				{
					impactedEndpoints.UnionWith(pendingAction.GetEndpoints());
					_pendingConnectionActionsMapping.Remove(pendingAction);
				}

				foreach (var pendingAction in e.UpdatedPendingActions)
				{
					if (_pendingConnectionActionsMapping.TryGetPendingConnectionActionForDestination(pendingAction.Destination, out var existingPendingConnectionAction))
					{
						if (pendingAction.Action == existingPendingConnectionAction.Action &&
							pendingAction.PendingSource == existingPendingConnectionAction.PendingSource)
						{
							continue;
						}

						impactedEndpoints.UnionWith(existingPendingConnectionAction.GetEndpoints());
					}

					impactedEndpoints.UnionWith(pendingAction.GetEndpoints());
					_pendingConnectionActionsMapping.AddOrUpdate(pendingAction);
				}

				RaiseConnectionsUpdated(impactedEndpoints);
			}
		}

		private void RaiseConnectionsUpdated(ICollection<ApiObjectReference<Endpoint>> impactedEndpoints)
		{
			if (impactedEndpoints.Count <= 0)
			{
				return;
			}

			LoadData(impactedEndpoints);

			var impactedVirtualSignalGroups = impactedEndpoints
				.SelectMany(x => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(x))
				.Distinct()
				.ToList();

			var eventArgs = new ConnectionsUpdatedEvent(
				impactedEndpoints.Select(GetConnectivity).ToList(),
				impactedVirtualSignalGroups.Select(GetConnectivity).ToList());

			ConnectionsUpdated?.Invoke(this, eventArgs);
		}

		private void UpdateEndpoints(IEnumerable<Endpoint> updated, IEnumerable<Endpoint> deleted = null)
		{
			lock (_lock)
			{
				if (updated != null)
				{
					foreach (var item in updated)
					{
						_endpoints[item.ID] = item;
					}
				}

				if (deleted != null)
				{
					foreach (var item in deleted)
					{
						_endpoints.Remove(item);
					}
				}
			}
		}

		private void UpdateVirtualSignalGroups(IEnumerable<VirtualSignalGroup> updated, IEnumerable<VirtualSignalGroup> deleted = null)
		{
			lock (_lock)
			{
				if (updated != null)
				{
					foreach (var item in updated)
					{
						_virtualSignalGroups[item.ID] = item;
						_virtualSignalGroupEndpointsMapping.AddOrUpdate(item);
					}
				}

				if (deleted != null)
				{
					foreach (var item in deleted)
					{
						_virtualSignalGroups.Remove(item);
						_virtualSignalGroupEndpointsMapping.Remove(item);
					}
				}
			}
		}

		private void LoadVirtualSignalGroupsThatContainEndpoints(IEnumerable<ApiObjectReference<Endpoint>> endpoints)
		{
			lock (_lock)
			{
				var endpointsToLoad = endpoints
					.Where(x => !_virtualSignalGroupEndpointsMapping.Contains(x))
					.Select(x => x.ID)
					.Distinct()
					.ToList();

				if (endpointsToLoad.Count > 0)
				{
					Debug.WriteLine($"Loading VSGs with endpoints: {String.Join(", ", endpointsToLoad)}");
					var virtualSignalGroups = Api.VirtualSignalGroups.GetByEndpointIds(endpointsToLoad).ToList();
					Debug.WriteLine($"Loaded {virtualSignalGroups.Count} VSGs: {String.Join(", ", virtualSignalGroups.Select(x => x.ID))}");

					UpdateVirtualSignalGroups(virtualSignalGroups);
				}
			}
		}

		private void LoadEndpoints(IEnumerable<ApiObjectReference<Endpoint>> endpointIds)
		{
			lock (_lock)
			{
				var endpointIdsToRetrieve = endpointIds
					.Where(id => !_endpoints.ContainsKey(id))
					.Distinct()
					.ToList();

				if (endpointIdsToRetrieve.Count > 0)
				{
					Debug.WriteLine($"Loading endpoints: {String.Join(", ", endpointIdsToRetrieve.Select(x => x.ID))}");
					var endpoints = Api.Endpoints.Read(endpointIdsToRetrieve);
					Debug.WriteLine($"Loaded {endpoints.Count} endpoints");

					UpdateEndpoints(endpoints.Values);
				}
			}
		}

		private void LoadVirtualSignalGroups(IEnumerable<ApiObjectReference<VirtualSignalGroup>> vsgIds)
		{
			lock (_lock)
			{
				var vsgIdsToRetrieve = vsgIds
					.Where(id => !_virtualSignalGroups.ContainsKey(id))
					.Distinct()
					.ToList();

				if (vsgIdsToRetrieve.Count > 0)
				{
					Debug.WriteLine($"Loading VSGs: {String.Join(", ", vsgIdsToRetrieve.Select(x => x.ID))}");
					var virtualSignalGroups = Api.VirtualSignalGroups.Read(vsgIdsToRetrieve);
					Debug.WriteLine($"Loaded {virtualSignalGroups.Count} VSGs");

					UpdateVirtualSignalGroups(virtualSignalGroups.Values);
				}
			}
		}

		private void LoadDataFromMediationElements()
		{
			lock (_lock)
			{
				var mediationElements = Api.MediationElements.AllElements;

				Parallel.ForEach(mediationElements, element =>
				{
					foreach (var connection in element.GetConnections())
					{
						_connectionEndpointsMapping.AddOrUpdate(connection);
					}

					foreach (var action in element.GetPendingConnectionActions())
					{
						_pendingConnectionActionsMapping.AddOrUpdate(action);
					}
				});
			}
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

				if (_connectionEndpointsMapping.TryGetConnectionForDestination(endpoint, out var connection) &&
					connection.IsConnected)
				{
					isConnected = true;

					if (connection.ConnectedSource.HasValue &&
						_endpoints.TryGetValue(connection.ConnectedSource.Value, out Endpoint connectedSourceEndpoint))
					{
						connectedSource = connectedSourceEndpoint;
					}
				}

				if (_pendingConnectionActionsMapping.TryGetPendingConnectionActionForDestination(endpoint, out var pendingAction))
				{
					if (pendingAction.Action == PendingConnectionActionType.Connect)
					{
						if (pendingAction.PendingSource.HasValue &&
							_endpoints.TryGetValue(pendingAction.PendingSource.Value, out var pendingSource) &&
							pendingSource != connectedSource)
						{
							isConnecting = true;
							pendingConnectedSource = pendingSource;
						}
					}
					else if (pendingAction.Action == PendingConnectionActionType.Disconnect)
					{
						isDisconnecting = true;
					}
				}

				var virtualSignalGroups = _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(endpoint);

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

				var connections = _connectionEndpointsMapping.GetConnectionsWithSource(endpoint);
				var pendingActions = _pendingConnectionActionsMapping.GetPendingConnectionActionsWithSource(endpoint);

				foreach (var connection in connections.Where(c => c.IsConnected))
				{
					isConnected = true;

					if (_endpoints.TryGetValue(connection.Destination, out var destination))
					{
						destinationStates[destination] = EndpointConnectionState.Connected;
					}
				}

				foreach (var pendingAction in pendingActions)
				{
					if (!_endpoints.TryGetValue(pendingAction.Destination, out var destination))
					{
						continue;
					}

					if (pendingAction.Action == PendingConnectionActionType.Connect)
					{
						if (destinationStates.TryGetValue(destination, out var existingState) &&
							existingState == EndpointConnectionState.Connected)
						{
							// If already fully connected, we can ignore this pending action
							continue;
						}

						isConnecting = true;
						destinationStates[destination] = EndpointConnectionState.Connecting;
					}
					else if (pendingAction.Action == PendingConnectionActionType.Disconnect)
					{
						isDisconnecting = true;
						destinationStates[destination] = EndpointConnectionState.Disconnecting;
					}
				}

				var virtualSignalGroups = _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(endpoint);
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
