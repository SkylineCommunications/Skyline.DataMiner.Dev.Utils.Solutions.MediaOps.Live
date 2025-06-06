namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;

	public class ConnectivityInfoProvider : IDisposable
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<Endpoint>, Endpoint> _endpoints = new();
		private readonly Dictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> _virtualSignalGroups = new();
		private readonly Dictionary<ApiObjectReference<Connection>, Connection> _connections = new();

		private readonly VirtualSignalGroupEndpointsMapping _virtualSignalGroupEndpointsMapping = new();
		private readonly ConnectionEndpointsMapping _connectionEndpointsMapping = new();

		private RepositorySubscription<Endpoint> _subscriptionEndpoints;
		private RepositorySubscription<VirtualSignalGroup> _subscriptionVirtualSignalGroups;
		private RepositorySubscription<Connection> _subscriptionConnections;
		private bool _isSubscribed;

		public ConnectivityInfoProvider(MediaOpsLiveApi api, bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));

			if (subscribe)
			{
				Subscribe();
			}
		}

		public MediaOpsLiveApi Api { get; }

		public bool IsConnected(Endpoint endpoint)
		{
			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded([endpoint]);

				return _connectionEndpointsMapping.GetConnections(endpoint)
					.Any(c => c.IsConnected);
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
				var connectedDestinations = new List<Endpoint>();
				var pendingConnectedDestinations = new List<Endpoint>();

				var connections = _connectionEndpointsMapping.GetConnections(endpoint);

				foreach (var connection in connections)
				{
					if (connection.ConnectedSource == endpoint &&
						_endpoints.TryGetValue(connection.Destination, out var destination))
					{
						connectedDestinations.Add(destination);
					}

					if (connection.PendingConnectedSource == endpoint &&
						_endpoints.TryGetValue(connection.Destination, out var pendingDestination))
					{
						pendingConnectedDestinations.Add(pendingDestination);
					}

					if (connection.Destination == endpoint)
					{
						if (connection.ConnectedSource.HasValue)
						{
							_endpoints.TryGetValue(connection.ConnectedSource.Value, out connectedSource);
						}

						if (connection.PendingConnectedSource.HasValue)
						{
							_endpoints.TryGetValue(connection.PendingConnectedSource.Value, out pendingConnectedSource);
						}
					}
				}

				if (!_endpoints.TryGetValue(endpoint, out var realEndpoint))
				{
					throw new InvalidOperationException("Couldn't find endpoint");
				}

				return new EndpointConnectivity(
					realEndpoint,
					connectedSource,
					pendingConnectedSource,
					connectedDestinations,
					pendingConnectedDestinations);
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
				var reference = virtualSignalGroup.Reference;
				EnsureVirtualSignalGroupsAreLoaded([reference]);

				var connectedSources = new List<VirtualSignalGroup>();
				var pendingConnectedSources = new List<VirtualSignalGroup>();
				var connectedDestinations = new List<VirtualSignalGroup>();
				var pendingConnectedDestinations = new List<VirtualSignalGroup>();

				var endpoints = _virtualSignalGroupEndpointsMapping.GetEndpoints(virtualSignalGroup)
					.Select(x => _endpoints[x])
					.ToList();

				var endpointsConnectivity = GetConnectivity(endpoints).Values;

				foreach (var connectivity in endpointsConnectivity)
				{
					if (connectivity.ConnectedSource != null)
					{
						var virtualSignalGroups = _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(connectivity.ConnectedSource);
						connectedSources.AddRange(virtualSignalGroups);
					}

					if (connectivity.PendingConnectedSource != null)
					{
						var virtualSignalGroups = _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(connectivity.PendingConnectedSource);
						pendingConnectedSources.AddRange(virtualSignalGroups);
					}

					if (connectivity.ConnectedDestinations.Count > 0)
					{
						var virtualSignalGroups = connectivity.ConnectedDestinations.SelectMany(x => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(x));
						connectedDestinations.AddRange(virtualSignalGroups);
					}

					if (connectivity.PendingConnectedDestinations.Count > 0)
					{
						var virtualSignalGroups = connectivity.PendingConnectedDestinations.SelectMany(x => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(x));
						pendingConnectedDestinations.AddRange(virtualSignalGroups);
					}
				}

				var connectionStatus = endpointsConnectivity.All(x => x.IsConnected)
					? ConnectionStatus.Connected
					: endpointsConnectivity.Any(x => x.IsConnected)
						? ConnectionStatus.Partial
						: ConnectionStatus.Disconnected;

				var pendingConnectionStatus = endpointsConnectivity.All(x => x.IsPendingConnected)
					? ConnectionStatus.Connected
					: endpointsConnectivity.Any(x => x.IsPendingConnected)
						? ConnectionStatus.Partial
						: ConnectionStatus.Disconnected;

				return new VirtualSignalGroupConnectivity(
					virtualSignalGroup,
					connectionStatus,
					pendingConnectionStatus,
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
			if (_isSubscribed)
			{
				return;
			}

			_isSubscribed = true;

			_subscriptionEndpoints = Api.Endpoints.Subscribe();
			_subscriptionEndpoints.Changed += Endpoints_Changed;

			_subscriptionVirtualSignalGroups = Api.VirtualSignalGroups.Subscribe();
			_subscriptionVirtualSignalGroups.Changed += VirtualSignalGroups_Changed;

			_subscriptionConnections = Api.Connections.Subscribe();
			_subscriptionConnections.Changed += Connections_Changed;
		}

		private void Endpoints_Changed(object sender, ApiObjectsChangedEvent<Endpoint> e)
		{
			lock (_lock)
			{
				UpdateEndpoints(e.Created.Concat(e.Updated), e.Deleted);

				var impactedConnections = FindImpactedConnections(e.Created.Concat(e.Updated).Concat(e.Deleted)).ToList();

				throw new NotImplementedException();
			}
		}

		private void VirtualSignalGroups_Changed(object sender, ApiObjectsChangedEvent<VirtualSignalGroup> e)
		{
			lock (_lock)
			{
				UpdateVirtualSignalGroups(e.Created.Concat(e.Updated), e.Deleted);

				var impactedConnections = FindImpactedConnections(e.Created.Concat(e.Updated).Concat(e.Deleted)).ToList();

				throw new NotImplementedException();
			}
		}

		private void Connections_Changed(object sender, ApiObjectsChangedEvent<Connection> e)
		{
			lock (_lock)
			{
				UpdateConnections(e.Created.Concat(e.Updated), e.Deleted);

				var impactedConnections = FindImpactedConnections(e.Created.Concat(e.Updated).Concat(e.Deleted)).ToList();

				throw new NotImplementedException();
			}
		}

		private void UpdateEndpoints(IEnumerable<Endpoint> updated, IEnumerable<Endpoint> deleted = null)
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

		private void UpdateConnections(IEnumerable<Connection> updated, IEnumerable<Connection> deleted = null)
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

		private IEnumerable<VirtualSignalGroupConnectivity> FindImpactedConnections(IEnumerable<Endpoint> endpoints)
		{
			var virtualSignalGroups = endpoints
				.SelectMany(endpoint => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(endpoint))
				.Distinct()
				.ToList();

			return GetConnectivity(virtualSignalGroups).Values;
		}

		private IEnumerable<VirtualSignalGroupConnectivity> FindImpactedConnections(IEnumerable<Connection> connections)
		{
			var virtualSignalGroups = connections
				.SelectMany(connection => connection.GetEndpoints())
				.SelectMany(endpoint => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(endpoint))
				.Distinct()
				.ToList();

			return GetConnectivity(virtualSignalGroups).Values;
		}

		private IEnumerable<VirtualSignalGroupConnectivity> FindImpactedConnections(IEnumerable<VirtualSignalGroup> virtualSignalGroups)
		{
			var expandedVirtualSignalGroups = virtualSignalGroups
				.SelectMany(vsg => vsg.GetLevelEndpoints())
				.SelectMany(levelEndpoint => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(levelEndpoint.Endpoint))
				.Distinct()
				.ToList();

			return GetConnectivity(expandedVirtualSignalGroups).Values;
		}

		public void Dispose()
		{
			if (_isSubscribed)
			{
				_subscriptionEndpoints?.Dispose();
				_subscriptionVirtualSignalGroups?.Dispose();
				_subscriptionConnections?.Dispose();
			}
		}
	}
}
