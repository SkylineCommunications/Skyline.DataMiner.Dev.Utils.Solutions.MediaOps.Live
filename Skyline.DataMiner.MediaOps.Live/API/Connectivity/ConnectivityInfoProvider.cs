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
				return IsConnected(endpoint.Reference);
			}
		}

		public bool IsConnected(ApiObjectReference<Endpoint> endpoint)
		{
			if (endpoint == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("The endpoint reference cannot be empty.", nameof(endpoint));
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
					if (IsConnected(levelEndpoint.Endpoint))
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
				EnsureEndpointsAreLoaded(endpoints);

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
				EnsureVirtualSignalGroupsAreLoaded(virtualSignalGroups);

				return virtualSignalGroups.ToDictionary(x => x, IsConnected);
			}
		}

		public bool IsPendingConnected(Endpoint endpoint)
		{
			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			lock (_lock)
			{
				return IsPendingConnected(endpoint.Reference);
			}
		}

		public bool IsPendingConnected(ApiObjectReference<Endpoint> endpoint)
		{
			if (endpoint == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("The endpoint reference cannot be empty.", nameof(endpoint));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded([endpoint]);

				return _connectionEndpointsMapping.GetConnections(endpoint)
					.Any(c => c.PendingConnectedSource != ApiObjectReference<Endpoint>.Empty);
			}
		}

		public ConnectionStatus IsPendingConnected(VirtualSignalGroup virtualSignalGroup)
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
					if (IsPendingConnected(levelEndpoint.Endpoint))
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

		public IDictionary<Endpoint, bool> IsPendingConnected(ICollection<Endpoint> endpoints)
		{
			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded(endpoints);

				return endpoints.ToDictionary(x => x, IsPendingConnected);
			}
		}

		public IDictionary<VirtualSignalGroup, ConnectionStatus> IsPendingConnected(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			lock (_lock)
			{
				EnsureVirtualSignalGroupsAreLoaded(virtualSignalGroups);

				return virtualSignalGroups.ToDictionary(x => x, IsPendingConnected);
			}
		}

		public Endpoint GetConnectedSource(Endpoint destinationEndpoint)
		{
			if (destinationEndpoint is null)
			{
				throw new ArgumentNullException(nameof(destinationEndpoint));
			}

			if (!destinationEndpoint.IsDestination)
			{
				throw new ArgumentException("The endpoint must be a destination.", nameof(destinationEndpoint));
			}

			lock (_lock)
			{
				return GetConnectedSource(destinationEndpoint.Reference);
			}
		}

		public Endpoint GetConnectedSource(ApiObjectReference<Endpoint> destinationEndpoint)
		{
			if (destinationEndpoint == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("The endpoint reference cannot be empty.", nameof(destinationEndpoint));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded([destinationEndpoint]);

				var connection = _connectionEndpointsMapping.GetConnections(destinationEndpoint)
					.FirstOrDefault(c => c.Destination == destinationEndpoint);

				if (connection?.ConnectedSource != null &&
					_endpoints.TryGetValue(connection.ConnectedSource.Value, out var sourceEndpoint))
				{
					return sourceEndpoint;
				}

				return null;
			}
		}

		public IDictionary<Endpoint, Endpoint> GetConnectedSource(ICollection<Endpoint> destinationEndpoints)
		{
			if (destinationEndpoints is null)
			{
				throw new ArgumentNullException(nameof(destinationEndpoints));
			}

			if (!destinationEndpoints.All(x => x.IsDestination))
			{
				throw new ArgumentException("All endpoints must be destinations.", nameof(destinationEndpoints));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded(destinationEndpoints);

				return destinationEndpoints.ToDictionary(x => x, GetConnectedSource);
			}
		}

		public ICollection<VirtualSignalGroup> GetConnectedSources(VirtualSignalGroup destinationVirtualSignalGroup)
		{
			if (destinationVirtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(destinationVirtualSignalGroup));
			}

			if (!destinationVirtualSignalGroup.IsDestination)
			{
				throw new ArgumentException("The virtual signal group must be a destination.", nameof(destinationVirtualSignalGroup));
			}

			lock (_lock)
			{
				EnsureVirtualSignalGroupsAreLoaded([destinationVirtualSignalGroup]);

				var destinationEndpoints = _virtualSignalGroupEndpointsMapping.GetEndpoints(destinationVirtualSignalGroup);

				var connectedSourceEndpoints = destinationEndpoints
					.Select(GetConnectedSource)
					.Where(source => source != null)
					.Distinct();

				var sourceVirtualSignalGroups = connectedSourceEndpoints
					.SelectMany(x => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(x))
					.Distinct()
					.ToList();

				return sourceVirtualSignalGroups;
			}
		}

		public IDictionary<VirtualSignalGroup, ICollection<VirtualSignalGroup>> GetConnectedSources(ICollection<VirtualSignalGroup> destinationVirtualSignalGroups)
		{
			if (destinationVirtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(destinationVirtualSignalGroups));
			}

			if (!destinationVirtualSignalGroups.All(x => x.IsDestination))
			{
				throw new ArgumentException("All virtual signal groups must be destinations.", nameof(destinationVirtualSignalGroups));
			}

			lock (_lock)
			{
				EnsureVirtualSignalGroupsAreLoaded(destinationVirtualSignalGroups);

				return destinationVirtualSignalGroups.ToDictionary(x => x, GetConnectedSources);
			}
		}

		public ICollection<Endpoint> GetConnectedDestinations(Endpoint sourceEndpoint)
		{
			if (sourceEndpoint is null)
			{
				throw new ArgumentNullException(nameof(sourceEndpoint));
			}

			if (!sourceEndpoint.IsSource)
			{
				throw new ArgumentException("The endpoint must be a source.", nameof(sourceEndpoint));
			}

			lock (_lock)
			{
				return GetConnectedDestinations(sourceEndpoint.Reference);
			}
		}

		public ICollection<Endpoint> GetConnectedDestinations(ApiObjectReference<Endpoint> sourceEndpoint)
		{
			if (sourceEndpoint == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("The endpoint reference cannot be empty.", nameof(sourceEndpoint));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded([sourceEndpoint]);

				var result = new List<Endpoint>();

				var connections = _connectionEndpointsMapping.GetConnections(sourceEndpoint)
					.Where(c => c.ConnectedSource == sourceEndpoint);

				foreach (var connection in connections)
				{
					if (_endpoints.TryGetValue(connection.Destination, out var destinationEndpoint))
					{
						result.Add(destinationEndpoint);
					}
				}

				return result;
			}
		}

		public ICollection<VirtualSignalGroup> GetConnectedDestinations(VirtualSignalGroup sourceVirtualSignalGroup)
		{
			if (sourceVirtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(sourceVirtualSignalGroup));
			}

			if (!sourceVirtualSignalGroup.IsSource)
			{
				throw new ArgumentException("The virtual signal group must be a source.", nameof(sourceVirtualSignalGroup));
			}

			lock (_lock)
			{
				EnsureVirtualSignalGroupsAreLoaded([sourceVirtualSignalGroup]);

				var sourceEndpoints = _virtualSignalGroupEndpointsMapping.GetEndpoints(sourceVirtualSignalGroup);

				var connectedDestinationEndpoints = sourceEndpoints
					.SelectMany(GetConnectedDestinations)
					.Distinct();

				var destinationVirtualSignalGroups = connectedDestinationEndpoints
					.SelectMany(x => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(x))
					.Distinct()
					.ToList();

				return destinationVirtualSignalGroups;
			}
		}

		public IDictionary<Endpoint, ICollection<Endpoint>> GetConnectedDestinations(ICollection<Endpoint> sourceEndpoints)
		{
			if (sourceEndpoints is null)
			{
				throw new ArgumentNullException(nameof(sourceEndpoints));
			}

			if (!sourceEndpoints.All(x => x.IsSource))
			{
				throw new ArgumentException("All endpoints must be sources.", nameof(sourceEndpoints));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded(sourceEndpoints);

				return sourceEndpoints.ToDictionary(x => x, GetConnectedDestinations);
			}
		}

		public IDictionary<VirtualSignalGroup, ICollection<VirtualSignalGroup>> GetConnectedDestinations(ICollection<VirtualSignalGroup> sourceVirtualSignalGroups)
		{
			if (sourceVirtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(sourceVirtualSignalGroups));
			}

			if (!sourceVirtualSignalGroups.All(x => x.IsSource))
			{
				throw new ArgumentException("All virtual signal groups must be sources.", nameof(sourceVirtualSignalGroups));
			}

			lock (_lock)
			{
				EnsureVirtualSignalGroupsAreLoaded(sourceVirtualSignalGroups);

				return sourceVirtualSignalGroups.ToDictionary(x => x, GetConnectedDestinations);
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
				return GetConnectivity(endpoint.Reference);
			}
		}

		public EndpointConnectivity GetConnectivity(ApiObjectReference<Endpoint> endpoint)
		{
			if (endpoint == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("The endpoint reference cannot be empty.", nameof(endpoint));
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

				return new EndpointConnectivity(
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

				var endpoints = _virtualSignalGroupEndpointsMapping.GetEndpoints(virtualSignalGroup).ToList();
				var endpointsConnectivity = GetConnectivity(endpoints);

				foreach (var endpointConnectivity in endpointsConnectivity)
				{
					var connectivity = endpointConnectivity.Value;

					if (connectivity.ConnectedSource != null)
					{
						connectedSources.AddRange(_virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(connectivity.ConnectedSource));
					}

					if (connectivity.PendingConnectedSource != null)
					{
						pendingConnectedSources.AddRange(_virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(connectivity.PendingConnectedSource));
					}

					if (connectivity.ConnectedDestinations.Count > 0)
					{
						var vsgs = connectivity.ConnectedDestinations.SelectMany(x => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(x));
						connectedDestinations.AddRange(vsgs);
					}

					if (connectivity.PendingConnectedDestinations.Count > 0)
					{
						var vsgs = connectivity.PendingConnectedDestinations.SelectMany(x => _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(x));
						pendingConnectedDestinations.AddRange(vsgs);
					}
				}

				var connectionStatus = endpointsConnectivity.Values.All(x => x.IsConnected)
					? ConnectionStatus.Connected
					: endpointsConnectivity.Values.Any(x => x.IsConnected) ? ConnectionStatus.Partial : ConnectionStatus.Disconnected;

				var pendingConnectionStatus = endpointsConnectivity.Values.All(x => x.IsPendingConnected)
					? ConnectionStatus.Connected
					: endpointsConnectivity.Values.Any(x => x.IsPendingConnected) ? ConnectionStatus.Partial : ConnectionStatus.Disconnected;

				return new VirtualSignalGroupConnectivity(
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
				EnsureEndpointsAreLoaded(endpoints);

				return endpoints.ToDictionary(x => x, GetConnectivity);
			}
		}

		public IDictionary<ApiObjectReference<Endpoint>, EndpointConnectivity> GetConnectivity(ICollection<ApiObjectReference<Endpoint>> endpoints)
		{
			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded(endpoints);

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
				EnsureVirtualSignalGroupsAreLoaded(virtualSignalGroups);

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
			_subscriptionVirtualSignalGroups = Api.VirtualSignalGroups.Subscribe();
			_subscriptionConnections = Api.Connections.Subscribe();
		}

		private void UpdateEndpoints(ICollection<Endpoint> updated, ICollection<Endpoint> deleted = null)
		{
			lock (_lock)
			{
				if (updated != null)
				{
					var newItems = updated.Where(item => !_endpoints.ContainsKey(item.ID)).ToList();

					foreach (var item in updated)
					{
						_endpoints[item.ID] = item;
					}

					if (newItems.Count > 0)
					{
						LoadExtraDataForEndpoints(newItems);
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

		private void UpdateVirtualSignalGroups(ICollection<VirtualSignalGroup> updated, ICollection<VirtualSignalGroup> deleted = null)
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

		private void UpdateConnections(ICollection<Connection> updated, ICollection<Connection> deleted = null)
		{
			lock (_lock)
			{
				if (updated != null)
				{
					var newItems = updated.Where(item => !_connections.ContainsKey(item.ID)).ToList();

					foreach (var item in updated)
					{
						_connections[item.ID] = item;
						_connectionEndpointsMapping.AddOrUpdate(item);
					}

					if (newItems.Count > 0)
					{
						LoadExtraDataForConnections(newItems);
					}
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

		private void LoadExtraDataForEndpoints(ICollection<Endpoint> endpoints)
		{
			lock (_lock)
			{
				var endpointIds = endpoints.Select(x => x.ID).ToList();

				Debug.WriteLine($"Loading VSGs with endpoints: {string.Join(", ", endpointIds)}");
				var virtualSignalGroups = Api.VirtualSignalGroups.GetByEndpointIds(endpointIds).ToList();
				Debug.WriteLine($"Loaded {virtualSignalGroups.Count} VSGs: {string.Join(", ", virtualSignalGroups.Select(x => x.ID))}");
				UpdateVirtualSignalGroups(virtualSignalGroups);

				Debug.WriteLine($"Loading connections with endpoints: {string.Join(", ", endpointIds)}");
				var connections = Api.Connections.GetByEndpointIds(endpointIds).ToList();
				Debug.WriteLine($"Loaded {connections.Count} connections: {string.Join(", ", connections.Select(x => x.ID))}");
				UpdateConnections(connections);
			}
		}

		private void LoadExtraDataForConnections(ICollection<Connection> connections)
		{
			lock (_lock)
			{
				var endpoints = connections
					.SelectMany(x => x.GetEndpoints())
					.Distinct()
					.ToList();

				EnsureEndpointsAreLoaded(endpoints);
			}
		}

		private void EnsureEndpointsAreLoaded(ICollection<ApiObjectReference<Endpoint>> endpointIds)
		{
			lock (_lock)
			{
				var endpointIdsToRetrieve = endpointIds
					.Where(id => !_endpoints.ContainsKey(id))
					.ToList();

				if (endpointIdsToRetrieve.Count > 0)
				{
					Debug.WriteLine($"Loading endpoints: {string.Join(", ", endpointIdsToRetrieve.Select(x => x.ID))}");
					var endpoints = Api.Endpoints.Read(endpointIdsToRetrieve);
					Debug.WriteLine($"Loaded {endpoints.Count} endpoints: {string.Join(", ", endpoints.Select(x => x.Key.ID))}");
					UpdateEndpoints(endpoints.Values);
				}
			}
		}

		private void EnsureEndpointsAreLoaded(ICollection<Endpoint> endpoints)
		{
			lock (_lock)
			{
				EnsureEndpointsAreLoaded(endpoints.Select(x => x.Reference).ToList());
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
					Debug.WriteLine($"Loading VSGs: {string.Join(", ", vsgIdsToRetrieve.Select(x => x.ID))}");
					var virtualSignalGroups = Api.VirtualSignalGroups.Read(vsgIdsToRetrieve);
					Debug.WriteLine($"Loaded {virtualSignalGroups.Count} VSGs: {string.Join(", ", virtualSignalGroups.Select(x => x.Key.ID))}");
					UpdateVirtualSignalGroups(virtualSignalGroups.Values);
				}
			}
		}

		private void EnsureVirtualSignalGroupsAreLoaded(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			lock (_lock)
			{
				EnsureVirtualSignalGroupsAreLoaded(virtualSignalGroups.Select(x => x.Reference).ToList());
			}
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
