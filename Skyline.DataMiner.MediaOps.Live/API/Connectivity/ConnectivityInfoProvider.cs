namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
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
		private readonly Dictionary<ApiObjectReference<Endpoint>, Connection> _connectionsByDestination = new();
		private readonly Dictionary<ApiObjectReference<Endpoint>, PendingConnectionAction> _pendingActionsByDestination = new();

		private readonly VirtualSignalGroupEndpointsMapping _virtualSignalGroupEndpointsMapping = new();
		private readonly ConnectionEndpointsMapping _connectionEndpointsMapping = new();
		private readonly PendingConnectionActionMapping _pendingConnectionActionsMapping = new();

		private readonly ICollection<MediationElement> _mediationElements = [];

		private RepositorySubscription<Endpoint> _subscriptionEndpoints;
		private RepositorySubscription<VirtualSignalGroup> _subscriptionVirtualSignalGroups;

		public ConnectivityInfoProvider(MediaOpsLiveApi api, bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));

			_mediationElements = MediationElement.GetAllMediationElements(api).ToList();

			if (subscribe)
			{
				Subscribe();
			}
			else
			{
				LoadDataFromMediationElements();
			}
		}

		public event EventHandler<ConnectionsUpdatedEvent> ConnectionsUpdated;

		public MediaOpsLiveApi Api { get; }

		public IDms Dms { get; }

		public bool IsSubscribed { get; private set; }

		public bool IsConnected(Endpoint endpoint)
		{
			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded([endpoint]);

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
				EnsureVirtualSignalGroupsAreLoaded([virtualSignalGroup]);

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

		public IDictionary<Endpoint, bool> IsConnected(ICollection<Endpoint> endpoints)
		{
			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded(endpoints.Select(x => x.Reference));

				return endpoints.ToDictionary(x => x, IsConnected);
			}
		}

		public IDictionary<VirtualSignalGroup, ConnectionState> IsConnected(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			lock (_lock)
			{
				EnsureVirtualSignalGroupsAreLoaded(virtualSignalGroups.Select(x => x.Reference));

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
				EnsureEndpointsAreLoaded([endpoint]);

				var connectedSource = (EndpointConnection)null;
				var pendingConnectedSource = (Endpoint)null;
				var destinationStates = new Dictionary<Endpoint, EndpointConnectionState>();

				var connections = _connectionEndpointsMapping.GetConnections(endpoint);

				foreach (var connection in connections.Where(x => x.ConnectedSource.HasValue))
				{
					if (connection.ConnectedSource.HasValue &&
						_endpoints.TryGetValue(connection.Destination, out var destination) &&
						_endpoints.TryGetValue(connection.ConnectedSource.Value, out var connectedSourceEndpoint))
					{
						var isDisconnecting = _pendingConnectionActionsMapping.IsDisconnecting(connection.Destination);
						var state = isDisconnecting ? EndpointConnectionState.Disconnecting : EndpointConnectionState.Connected;

						if (connection.Destination == endpoint)
						{
							connectedSource = new EndpointConnection(connectedSourceEndpoint, state);
						}
						else if (connection.ConnectedSource == endpoint)
						{
							destinationStates[destination] = state;
						}
					}
				}

				var pendingActions = _pendingConnectionActionsMapping.GetPendingConnectionActions(endpoint);

				foreach (var pendingAction in pendingActions)
				{
					if (!_endpoints.TryGetValue(pendingAction.Destination, out var destination))
					{
						continue;
					}

					if (pendingAction.Action == PendingConnectionActionType.Connect &&
						pendingAction.PendingSource.HasValue &&
						_endpoints.TryGetValue(pendingAction.PendingSource.Value, out var pendingSource))
					{
						if (pendingAction.Destination == endpoint)
						{
							if (pendingSource == connectedSource?.Endpoint)
							{
								// If the pending source is the same as the connected source, we can ignore this pending action
								continue;
							}

							pendingConnectedSource = pendingSource;
						}
						else if (pendingAction.PendingSource == endpoint)
						{
							if (destinationStates.TryGetValue(destination, out var existingState) &&
								existingState == EndpointConnectionState.Connected)
							{
								// If already fully connected, we can ignore this pending action
								continue;
							}

							destinationStates[destination] = EndpointConnectionState.Connecting;
						}
					}
				}

				var destinationConnections = destinationStates.Select(x => new EndpointConnection(x.Key, x.Value)).ToList();

				return new EndpointConnectivity(
					endpoint,
					_virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(endpoint),
					connectedSource,
					pendingConnectedSource,
					destinationConnections);
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
				EnsureEndpointsAreLoaded([endpointRef]);

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
				EnsureVirtualSignalGroupsAreLoaded([virtualSignalGroup]);

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
						var virtualSignalGroups = _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(connectivity.ConnectedSource.Endpoint);
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

		public IDictionary<Endpoint, EndpointConnectivity> GetConnectivity(ICollection<Endpoint> endpoints)
		{
			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded(endpoints.Select(x => x.Reference));

				return endpoints.ToDictionary(x => x, GetConnectivity);
			}
		}

		public IDictionary<VirtualSignalGroup, VirtualSignalGroupConnectivity> GetConnectivity(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			lock (_lock)
			{
				EnsureVirtualSignalGroupsAreLoaded(virtualSignalGroups.Select(x => x.Reference));

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

				foreach (var mediationElement in _mediationElements)
				{
					mediationElement.PendingConnectionActionsChanged += PendingConnectionActions_OnChanged;
					mediationElement.ConnectionsChanged += Connections_OnChanged;
					mediationElement.Subscribe();
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

				foreach (var mediationElement in _mediationElements)
				{
					mediationElement.Unsubscribe();
					mediationElement.PendingConnectionActionsChanged -= PendingConnectionActions_OnChanged;
					mediationElement.ConnectionsChanged -= Connections_OnChanged;
				}

				IsSubscribed = false;
			}
		}

		public void UpdateEndpoints(IEnumerable<Endpoint> updated, IEnumerable<Endpoint> deleted = null)
		{
			lock (_lock)
			{
				if (updated != null)
				{
					var newEndpoints = updated.Where(x => !_endpoints.ContainsKey(x)).ToList();

					foreach (var item in updated)
					{
						_endpoints[item.ID] = item;
					}

					if (newEndpoints.Count > 0)
					{
						LoadExtraDataForEndpoints(newEndpoints);
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

		public void UpdateVirtualSignalGroups(IEnumerable<VirtualSignalGroup> updated, IEnumerable<VirtualSignalGroup> deleted = null)
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

					// Ensure that endpoints for the updated virtual signal groups are loaded
					var endpoints = updated.SelectMany(x => x.GetLevelEndpoints()).Select(x => x.Endpoint);
					EnsureEndpointsAreLoaded(endpoints);
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

				foreach (var destinationId in e.DeletedConnections)
				{
					if (_connectionsByDestination.TryGetValue(destinationId, out var existingConnection))
					{
						impactedEndpoints.UnionWith(existingConnection.GetEndpoints());
						_connectionsByDestination.Remove(existingConnection.Destination);
						_connectionEndpointsMapping.Remove(existingConnection);
					}
				}

				foreach (var connection in e.UpdatedConnections)
				{
					if (_connectionsByDestination.TryGetValue(connection.Destination, out var existingConnection))
					{
						impactedEndpoints.UnionWith(existingConnection.GetEndpoints());
					}

					impactedEndpoints.UnionWith(connection.GetEndpoints());
					_connectionsByDestination[connection.Destination] = connection;
					_connectionEndpointsMapping.AddOrUpdate(connection);
				}

				EnsureEndpointsAreLoaded(impactedEndpoints);
				RaiseConnectionsUpdated(impactedEndpoints);
			}
		}

		private void PendingConnectionActions_OnChanged(object sender, PendingConnectionActionsChangedEvent e)
		{
			lock (_lock)
			{
				Debug.WriteLine($"Pending connection actions changed: {e}");

				var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

				foreach (var destinationId in e.DeletedPendingActions)
				{
					if (_pendingActionsByDestination.TryGetValue(destinationId, out var existingPendingAction))
					{
						impactedEndpoints.UnionWith(existingPendingAction.GetEndpoints());
						_pendingActionsByDestination.Remove(existingPendingAction.Destination);
						_pendingConnectionActionsMapping.Remove(existingPendingAction);
					}
				}

				foreach (var pendingAction in e.UpdatedPendingActions)
				{
					if (_pendingActionsByDestination.TryGetValue(pendingAction.Destination, out var existingPendingConnectionAction))
					{
						impactedEndpoints.UnionWith(existingPendingConnectionAction.GetEndpoints());
					}

					impactedEndpoints.UnionWith(pendingAction.GetEndpoints());
					_pendingActionsByDestination[pendingAction.Destination] = pendingAction;
					_pendingConnectionActionsMapping.AddOrUpdate(pendingAction);
				}

				EnsureEndpointsAreLoaded(impactedEndpoints);
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
				.SelectMany(x => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(x))
				.Distinct()
				.ToList();

			var eventArgs = new ConnectionsUpdatedEvent(
				impactedEndpoints.Select(GetConnectivity).ToList(),
				impactedVirtualSignalGroups.Select(GetConnectivity).ToList());

			ConnectionsUpdated?.Invoke(this, eventArgs);
		}

		private void LoadExtraDataForEndpoints(IEnumerable<Endpoint> endpoints)
		{
			lock (_lock)
			{
				var endpointIds = endpoints.Select(x => x.ID).ToList();

				Debug.WriteLine($"Loading VSGs with endpoints: {String.Join(", ", endpointIds)}");
				var virtualSignalGroups = Api.VirtualSignalGroups.GetByEndpointIds(endpointIds).ToList();
				Debug.WriteLine($"Loaded {virtualSignalGroups.Count} VSGs: {String.Join(", ", virtualSignalGroups.Select(x => x.ID))}");
				UpdateVirtualSignalGroups(virtualSignalGroups);
			}
		}

		private void EnsureEndpointsAreLoaded(IEnumerable<ApiObjectReference<Endpoint>> endpointIds)
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

		private void EnsureVirtualSignalGroupsAreLoaded(IEnumerable<ApiObjectReference<VirtualSignalGroup>> vsgIds)
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

					var endpoints = virtualSignalGroups.Values
						.SelectMany(vsg => vsg.GetLevelEndpoints())
						.Select(endpoint => endpoint.Endpoint);
					EnsureEndpointsAreLoaded(endpoints);
				}
			}
		}

		private void LoadDataFromMediationElements()
		{
			lock (_lock)
			{
				var connections = _mediationElements.AsParallel().SelectMany(x => x.GetConnections()).ToList();
				var pendingConnectionActions = _mediationElements.AsParallel().SelectMany(x => x.GetPendingConnectionActions()).ToList();

				var endpointIds = new HashSet<ApiObjectReference<Endpoint>>();

				foreach (var connection in connections)
				{
					_connectionsByDestination[connection.Destination] = connection;
					_connectionEndpointsMapping.AddOrUpdate(connection);

					endpointIds.UnionWith(connection.GetEndpoints());
				}

				foreach (var action in pendingConnectionActions)
				{
					_pendingActionsByDestination[action.Destination] = action;
					_pendingConnectionActionsMapping.AddOrUpdate(action);

					endpointIds.UnionWith(action.GetEndpoints());
				}

				EnsureEndpointsAreLoaded(endpointIds);
			}
		}

		public void Dispose()
		{
			Unsubscribe();
		}
	}
}
