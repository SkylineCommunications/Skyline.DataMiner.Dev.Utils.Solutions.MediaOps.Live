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
	using Skyline.DataMiner.MediaOps.Live.Mediation;
	using Skyline.DataMiner.MediaOps.Live.Subscriptions;

	public sealed class ConnectivityInfoProvider : IDisposable
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<Endpoint>, Endpoint> _endpoints = new();
		private readonly Dictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> _virtualSignalGroups = new();
		private readonly Dictionary<ApiObjectReference<Connection>, Connection> _connections = new();
		private readonly Dictionary<ApiObjectReference<Endpoint>, PendingConnectionAction> _pendingConnectionActions = new();

		private readonly VirtualSignalGroupEndpointsMapping _virtualSignalGroupEndpointsMapping = new();
		private readonly ConnectionEndpointsMapping _connectionEndpointsMapping = new();
		private readonly PendingConnectionActionMapping _pendingConnectionActionsMapping = new();

		private readonly ICollection<TableSubscription> _pendingConnectionActionsSubscriptions = [];
		private RepositorySubscription<Endpoint> _subscriptionEndpoints;
		private RepositorySubscription<VirtualSignalGroup> _subscriptionVirtualSignalGroups;
		private RepositorySubscription<Connection> _subscriptionConnections;

		private bool _isSubscribed;

		public ConnectivityInfoProvider(MediaOpsLiveApi api, bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			Dms = api.Connection.GetDms();

			if (subscribe)
			{
				Subscribe();
			}
			else
			{
				LoadPendingConnectionActions();
			}
		}

		public event EventHandler<ConnectionsUpdatedEvent> ConnectionsUpdated;

		public MediaOpsLiveApi Api { get; }

		public IDms Dms { get; }

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

		public ConnectionStatus IsConnected(VirtualSignalGroup virtualSignalGroup)
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
						return ConnectionStatus.Partial;
				}

				return anyConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
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

		public IDictionary<VirtualSignalGroup, ConnectionStatus> IsConnected(ICollection<VirtualSignalGroup> virtualSignalGroups)
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

				Endpoint connectedSource = null;
				Endpoint pendingConnectedSource = null;
				var connectedDestinations = new HashSet<Endpoint>();
				var pendingConnectedDestinations = new HashSet<Endpoint>();

				var connections = _connectionEndpointsMapping.GetConnections(endpoint);
				var pendingActions = _pendingConnectionActionsMapping.GetPendingConnectionActions(endpoint);

				foreach (var connection in connections)
				{
					if (connection.ConnectedSource.HasValue)
					{
						if (connection.ConnectedSource == endpoint &&
							_endpoints.TryGetValue(connection.Destination, out var destination))
						{
							connectedDestinations.Add(destination);
						}

						if (connection.Destination == endpoint)
						{
							_endpoints.TryGetValue(connection.ConnectedSource.Value, out connectedSource);
						}
					}

					foreach (var pendingAction in pendingActions)
					{
						if (pendingAction.Action == PendingConnectionAction.PendingActionType.Connect &&
							pendingAction.PendingSource.HasValue)
						{
							if (pendingAction.Destination == endpoint &&
								_endpoints.TryGetValue(pendingAction.PendingSource.Value, out var pendingSource) &&
								connectedSource != pendingSource)
							{
								pendingConnectedSource = pendingSource;
							}

							if (pendingAction.PendingSource == endpoint &&
								_endpoints.TryGetValue(pendingAction.Destination, out var pendingDestination) &&
								!connectedDestinations.Contains(pendingDestination))
							{
								pendingConnectedDestinations.Add(pendingDestination);
							}
						}
					}
				}

				return new EndpointConnectivity(
					endpoint,
					_virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(endpoint),
					connectedSource,
					pendingConnectedSource,
					connectedDestinations,
					pendingConnectedDestinations);
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
						var virtualSignalGroups = _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(connectivity.ConnectedSource);
						connectedSources.UnionWith(virtualSignalGroups);
					}

					if (connectivity.PendingConnectedSource != null)
					{
						var virtualSignalGroups = _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(connectivity.PendingConnectedSource);
						pendingConnectedSources.UnionWith(virtualSignalGroups);
					}

					if (connectivity.ConnectedDestinations.Count > 0)
					{
						var virtualSignalGroups = connectivity.ConnectedDestinations.SelectMany(x => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(x));
						connectedDestinations.UnionWith(virtualSignalGroups);
					}

					if (connectivity.PendingConnectedDestinations.Count > 0)
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
				if (_isSubscribed)
				{
					return;
				}

				_subscriptionEndpoints = Api.Endpoints.Subscribe();
				_subscriptionVirtualSignalGroups = Api.VirtualSignalGroups.Subscribe();
				_subscriptionConnections = Api.Connections.Subscribe();

				_subscriptionEndpoints.Changed += Endpoints_Changed;
				_subscriptionVirtualSignalGroups.Changed += VirtualSignalGroups_Changed;
				_subscriptionConnections.Changed += Connections_Changed;

				foreach (var mediationElement in MediationElement.GetAllMediationElements(Dms))
				{
					var tableSubscription = new TableSubscription(Api.Connection, mediationElement.DmsElement, 3000);
					tableSubscription.OnChanged += PendingConnectionActions_OnChanged;

					_pendingConnectionActionsSubscriptions.Add(tableSubscription);
				}

				_isSubscribed = true;
			}
		}

		public void Unsubscribe()
		{
			lock (_lock)
			{
				if (!_isSubscribed)
				{
					return;
				}

				_subscriptionEndpoints.Changed -= Endpoints_Changed;
				_subscriptionVirtualSignalGroups.Changed -= VirtualSignalGroups_Changed;
				_subscriptionConnections.Changed -= Connections_Changed;

				_subscriptionEndpoints.Dispose();
				_subscriptionVirtualSignalGroups.Dispose();
				_subscriptionConnections.Dispose();

				foreach (var tableSubscription in _pendingConnectionActionsSubscriptions)
				{
					tableSubscription.OnChanged -= PendingConnectionActions_OnChanged;
					tableSubscription.Dispose();
				}

				_pendingConnectionActionsSubscriptions.Clear();

				_isSubscribed = false;
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

		public void UpdateConnections(IEnumerable<Connection> updated, IEnumerable<Connection> deleted = null)
		{
			lock (_lock)
			{
				if (updated != null)
				{
					foreach (var item in updated)
					{
						_connections[item.ID] = item;
						_connectionEndpointsMapping.AddOrUpdate(item);
					}

					// Ensure that endpoints for the updated connections are loaded
					var endpoints = updated.SelectMany(x => x.GetEndpoints());
					EnsureEndpointsAreLoaded(endpoints);
				}

				if (deleted != null)
				{
					foreach (var item in deleted)
					{
						_connections.Remove(item);
						_connectionEndpointsMapping.Remove(item);
					}
				}
			}
		}

		private void Endpoints_Changed(object sender, ApiObjectsChangedEvent<Endpoint> e)
		{
			lock (_lock)
			{
				UpdateEndpoints(e.Created.Concat(e.Updated), e.Deleted);
			}
		}

		private void VirtualSignalGroups_Changed(object sender, ApiObjectsChangedEvent<VirtualSignalGroup> e)
		{
			lock (_lock)
			{
				UpdateVirtualSignalGroups(e.Created.Concat(e.Updated), e.Deleted);
			}
		}

		private void Connections_Changed(object sender, ApiObjectsChangedEvent<Connection> e)
		{
			lock (_lock)
			{
				var impactedEndpoints = GetImpactedEndpointsForChangedConnections(e);

				UpdateConnections(e.Created.Concat(e.Updated), e.Deleted);

				RaiseConnectionsUpdated(impactedEndpoints);
			}
		}

		private void PendingConnectionActions_OnChanged(object sender, TableValueChange e)
		{
			lock (this)
			{
				var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

				foreach (var key in e.DeletedRows)
				{
					var destinationIdValue = key;
					Guid.TryParse(destinationIdValue, out var destinationId);

					if (_pendingConnectionActions.TryGetValue(destinationId, out var pendingAction))
					{
						impactedEndpoints.UnionWith(pendingAction.GetEndpoints());
						_pendingConnectionActions.Remove(pendingAction.Destination);
						_pendingConnectionActionsMapping.Remove(pendingAction);
					}
				}

				foreach (var row in e.UpdatedRows.Values)
				{
					var pendingAction = new PendingConnectionAction(row);

					impactedEndpoints.UnionWith(pendingAction.GetEndpoints());

					if (_pendingConnectionActions.TryGetValue(pendingAction.Destination, out var existingPendingConnectionAction))
					{
						// Cannot use mapping.AddOrUpdate because PendingConnectionAction doesn't implement IEquatable
						_pendingConnectionActionsMapping.Remove(existingPendingConnectionAction);
					}

					_pendingConnectionActions[pendingAction.Destination] = pendingAction;
					_pendingConnectionActionsMapping.Add(pendingAction);
				}

				if (impactedEndpoints.Count > 0)
				{
					RaiseConnectionsUpdated(impactedEndpoints);
				}
			}
		}

		private ICollection<ApiObjectReference<Endpoint>> GetImpactedEndpointsForChangedConnections(ApiObjectsChangedEvent<Connection> change)
		{
			var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

			foreach (var connection in change.Created.Concat(change.Updated))
			{
				if (!_connections.TryGetValue(connection.ID, out var existing))
				{
					impactedEndpoints.UnionWith(connection.GetEndpoints());
					continue;
				}

				bool hasChangeDetected = false;

				if (connection.ConnectedSource != existing.ConnectedSource)
				{
					hasChangeDetected = true;
					if (existing.ConnectedSource.HasValue)
						impactedEndpoints.Add(existing.ConnectedSource.Value);
					if (connection.ConnectedSource.HasValue)
						impactedEndpoints.Add(connection.ConnectedSource.Value);
				}

				if (hasChangeDetected)
				{
					impactedEndpoints.Add(connection.Destination);
				}
			}

			foreach (var connection in change.Deleted)
			{
				impactedEndpoints.UnionWith(connection.GetEndpoints());
			}

			impactedEndpoints.RemoveWhere(x => x != ApiObjectReference<Endpoint>.Empty);

			return impactedEndpoints;
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

				Debug.WriteLine($"Loading connections with endpoints: {String.Join(", ", endpointIds)}");
				var connections = Api.Connections.GetByEndpointIds(endpointIds).ToList();
				Debug.WriteLine($"Loaded {connections.Count} connections: {String.Join(", ", connections.Select(x => x.ID))}");
				UpdateConnections(connections);
			}
		}

		private void EnsureEndpointsAreLoaded(IEnumerable<ApiObjectReference<Endpoint>> endpointIds)
		{
			lock (_lock)
			{
				var endpointIdsToRetrieve = endpointIds
					.Where(id => !_endpoints.ContainsKey(id))
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

		private void LoadPendingConnectionActions()
		{
			lock (_lock)
			{
				var pendingConnectionActions = MediationElement.GetAllMediationElements(Dms)
					.AsParallel()
					.SelectMany(x => x.GetPendingConnectionActions())
					.ToList();

				foreach (var action in pendingConnectionActions)
				{
					_pendingConnectionActionsMapping.Add(action);
				}
			}
		}

		public void Dispose()
		{
			Unsubscribe();
		}
	}
}
