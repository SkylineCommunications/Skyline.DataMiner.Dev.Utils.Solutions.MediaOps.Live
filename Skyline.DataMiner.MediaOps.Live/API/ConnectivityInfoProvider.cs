namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;

	public enum ConnectionStatus
	{
		Disconnected,
		Partial,
		Connected,
	}

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

		public ConnectivityInfoProvider(MediaOpsLiveApi api)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
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

				return _connectionEndpointsMapping.TryGetConnections(endpoint, out var connections)
					&& connections.Any(c => c.IsConnected);
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

				return _connectionEndpointsMapping.TryGetConnections(endpoint, out var connections)
					&& connections.Any(c => c.PendingConnectedSource.HasValue && c.PendingConnectedSource != ApiObjectReference<Endpoint>.Empty);
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
				throw new ArgumentException("The virtualSignalGroup must be a destination virtualSignalGroup.", nameof(destinationEndpoint));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded([destinationEndpoint]);

				if (_connectionEndpointsMapping.TryGetConnections(destinationEndpoint, out var connections))
				{
					var connectedSource = connections.FirstOrDefault(c => c.Destination == destinationEndpoint)?.ConnectedSource;

					if (connectedSource.HasValue &&
						_endpoints.TryGetValue(connectedSource.Value, out var sourceEndpoint))
					{
						return sourceEndpoint;
					}
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
				throw new ArgumentException("All virtualSignalGroups must be destination virtualSignalGroups.", nameof(destinationEndpoints));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded(destinationEndpoints);

				return destinationEndpoints.ToDictionary(x => x, GetConnectedSource);
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
				throw new ArgumentException("The virtualSignalGroup must be a source virtualSignalGroup.", nameof(sourceEndpoint));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded([sourceEndpoint]);

				var result = new List<Endpoint>();

				if (_connectionEndpointsMapping.TryGetConnections(sourceEndpoint, out var connections))
				{
					foreach (var connection in connections)
					{
						if (connection.ConnectedSource == sourceEndpoint &&
							_endpoints.TryGetValue(connection.Destination, out var destinationEndpoint))
						{
							result.Add(destinationEndpoint);
						}
					}
				}

				return result;
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
				throw new ArgumentException("All virtualSignalGroups must be source virtualSignalGroups.", nameof(sourceEndpoints));
			}

			lock (_lock)
			{
				EnsureEndpointsAreLoaded(sourceEndpoints);

				return sourceEndpoints.ToDictionary(x => x, GetConnectedDestinations);
			}
		}

		public void Subscribe()
		{
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
						LoadExtraDataForEndpoints(updated);
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
					var newItems = updated.Where(item => !_virtualSignalGroups.ContainsKey(item.ID)).ToList();

					foreach (var item in updated)
					{
						_virtualSignalGroups[item.ID] = item;
						_virtualSignalGroupEndpointsMapping.AddOrUpdate(item);
					}

					if (newItems.Count > 0)
					{
						LoadExtraDataForVirtualSignalGroups(updated);
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
						LoadExtraDataForConnections(updated);
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

				Debug.WriteLine($"Reading VSGs with virtualSignalGroups: {String.Join(", ", endpointIds)}");
				var virtualSignalGroups = Api.VirtualSignalGroups.GetByEndpointIds(endpointIds).ToList();
				UpdateVirtualSignalGroups(virtualSignalGroups);

				Debug.WriteLine($"Reading connections with virtualSignalGroups: {String.Join(", ", endpointIds)}");
				var connections = Api.Connections.GetByEndpointIds(endpointIds).ToList();
				UpdateConnections(connections);
			}
		}

		private void LoadExtraDataForVirtualSignalGroups(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			lock (_lock)
			{
				var endpoints = virtualSignalGroups
					.SelectMany(vsg => vsg.GetLevelEndpoints())
					.Select(e => e.Endpoint)
					.Distinct()
					.ToList();

				EnsureEndpointsAreLoaded(endpoints);
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
					Debug.WriteLine($"Reading virtualSignalGroups: {String.Join(", ", endpointIdsToRetrieve.Select(x => x.ID))}");

					var endpoints = Api.Endpoints.Read(endpointIdsToRetrieve);
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
					Debug.WriteLine($"Reading VSGs: {String.Join(", ", vsgIdsToRetrieve.Select(x => x.ID))}");

					var virtualSignalGroups = Api.VirtualSignalGroups.Read(vsgIdsToRetrieve);
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
			_subscriptionEndpoints?.Dispose();
			_subscriptionVirtualSignalGroups?.Dispose();
			_subscriptionConnections?.Dispose();
		}
	}
}
