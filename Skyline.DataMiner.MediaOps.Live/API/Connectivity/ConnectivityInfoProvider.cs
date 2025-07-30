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
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;

	public sealed class ConnectivityInfoProvider : IDisposable
	{
		private readonly object _lock = new();

		private readonly ConnectionEndpointsMapping _connectionEndpointsMapping = new();
		private readonly PendingConnectionActionMapping _pendingConnectionActionsMapping = new();

		private readonly ICollection<ConnectionSubscription> _connectionSubscriptions = [];
		private readonly ICollection<PendingConnectionActionSubscription> _pendingActionSubscriptions = [];

		public ConnectivityInfoProvider(MediaOpsLiveApi api, VirtualSignalGroupEndpointsCache endpointsCache, bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			Endpoints = endpointsCache ?? throw new ArgumentNullException(nameof(endpointsCache));

			Initialize(subscribe);
		}

		public ConnectivityInfoProvider(MediaOpsLiveApi api, bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			Endpoints = new VirtualSignalGroupEndpointsCache(api, subscribe);

			Initialize(subscribe);
		}

		public event EventHandler<ConnectionsUpdatedEvent> ConnectionsUpdated;

		public MediaOpsLiveApi Api { get; }

		public VirtualSignalGroupEndpointsCache Endpoints { get; }

		public bool IsSubscribed { get; private set; }

		public bool IsConnected(ApiObjectReference<Endpoint> endpoint)
		{
			if (endpoint == ApiObjectReference<Endpoint>.Empty)
			{
				new ArgumentNullException(nameof(endpoint));
			}

			lock (_lock)
			{
				return _connectionEndpointsMapping.GetConnections(endpoint).Any(c => c.IsConnected);
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
				return _connectionEndpointsMapping.IsConnected(source, destination);
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
				if (!Endpoints.TryGetEndpoint(endpointRef, out var endpoint))
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
				var levelsConnectivity = new Dictionary<ApiObjectReference<Level>, EndpointConnectivity>();
				var connectedSources = new HashSet<VirtualSignalGroup>();
				var pendingConnectedSources = new HashSet<VirtualSignalGroup>();
				var connectedDestinations = new HashSet<VirtualSignalGroup>();
				var pendingConnectedDestinations = new HashSet<VirtualSignalGroup>();

				foreach (var levelEndpoint in virtualSignalGroup.GetLevelEndpoints())
				{
					if (!Endpoints.TryGetEndpoint(levelEndpoint.Endpoint, out var endpoint))
					{
						throw new InvalidOperationException($"Endpoint {levelEndpoint.Endpoint.ID} not found for virtual signal group {virtualSignalGroup.ID}");
					}

					var connectivity = GetConnectivity(endpoint);

					levelsConnectivity[levelEndpoint.Level] = connectivity;

					if (connectivity.ConnectedSource != null)
					{
						var virtualSignalGroups = Endpoints.GetVirtualSignalGroupsThatContainEndpoint(connectivity.ConnectedSource);
						connectedSources.UnionWith(virtualSignalGroups);
					}

					if (connectivity.PendingConnectedSource != null)
					{
						var virtualSignalGroups = Endpoints.GetVirtualSignalGroupsThatContainEndpoint(connectivity.PendingConnectedSource);
						pendingConnectedSources.UnionWith(virtualSignalGroups);
					}

					if (connectivity.ConnectedDestinations.Any())
					{
						var virtualSignalGroups = connectivity.ConnectedDestinations.SelectMany(x => Endpoints.GetVirtualSignalGroupsThatContainEndpoint(x));
						connectedDestinations.UnionWith(virtualSignalGroups);
					}

					if (connectivity.PendingConnectedDestinations.Any())
					{
						var virtualSignalGroups = connectivity.PendingConnectedDestinations.SelectMany(x => Endpoints.GetVirtualSignalGroupsThatContainEndpoint(x));
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

				Endpoints.Subscribe();

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

				Endpoints.Unsubscribe();

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

			var impactedVirtualSignalGroups = impactedEndpoints
				.SelectMany(x => Endpoints.GetVirtualSignalGroupsThatContainEndpoint(x))
				.Distinct()
				.ToList();

			var eventArgs = new ConnectionsUpdatedEvent(
				impactedEndpoints.Select(GetConnectivity).ToList(),
				impactedVirtualSignalGroups.Select(GetConnectivity).ToList());

			ConnectionsUpdated?.Invoke(this, eventArgs);
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
						Endpoints.TryGetEndpoint(connection.ConnectedSource.Value, out Endpoint connectedSourceEndpoint))
					{
						connectedSource = connectedSourceEndpoint;
					}
				}

				if (_pendingConnectionActionsMapping.TryGetPendingConnectionActionForDestination(endpoint, out var pendingAction))
				{
					if (pendingAction.Action == PendingConnectionActionType.Connect)
					{
						if (pendingAction.PendingSource.HasValue &&
							Endpoints.TryGetEndpoint(pendingAction.PendingSource.Value, out var pendingSource) &&
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

				var virtualSignalGroups = Endpoints.GetVirtualSignalGroupsThatContainEndpoint(endpoint);

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

					if (Endpoints.TryGetEndpoint(connection.Destination, out var destination))
					{
						destinationStates[destination] = EndpointConnectionState.Connected;
					}
				}

				foreach (var pendingAction in pendingActions)
				{
					if (!Endpoints.TryGetEndpoint(pendingAction.Destination, out var destination))
					{
						continue;
					}

					if (pendingAction.Action == PendingConnectionActionType.Connect &&
						!_connectionEndpointsMapping.IsConnected(endpoint, destination))
					{
						isConnecting = true;
						destinationStates[destination] = EndpointConnectionState.Connecting;
					}
					else if (pendingAction.Action == PendingConnectionActionType.Disconnect &&
						_connectionEndpointsMapping.IsConnected(endpoint, destination))
					{
						isDisconnecting = true;
						destinationStates[destination] = EndpointConnectionState.Disconnecting;
					}
				}

				var virtualSignalGroups = Endpoints.GetVirtualSignalGroupsThatContainEndpoint(endpoint);
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
