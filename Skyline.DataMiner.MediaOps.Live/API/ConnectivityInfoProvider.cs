namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

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
				EnsureEndpointsAreLoaded([endpoint.Reference]);

				if (!_connectionEndpointsMapping.TryGetConnections(endpoint, out var connections))
				{
					return false;
				}

				return connections.Any(c => c.IsConnected);
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
				EnsureEndpointsAreLoaded([endpoint.Reference]);

				if (!_connectionEndpointsMapping.TryGetConnections(endpoint, out var connections))
				{
					return false;
				}

				return connections.Any(c => c.PendingConnectedSource != Guid.Empty);
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

		private void LoadExtraDataForEndpoints(IEnumerable<Endpoint> endpoints)
		{
			lock (_lock)
			{
				var endpointIds = endpoints.Select(x => x.ID).ToList();

				var virtualSignalGroups = Api.VirtualSignalGroups.GetByEndpointIds(endpointIds).ToList();
				UpdateVirtualSignalGroups(virtualSignalGroups);

				var connections = Api.Connections.GetByEndpointIds(endpointIds).ToList();
				UpdateConnections(connections);
			}
		}

		private void LoadExtraDataForVirtualSignalGroups(IEnumerable<VirtualSignalGroup> virtualSignalGroups)
		{
			lock (_lock)
			{
				var endpoints = virtualSignalGroups
					.SelectMany(vsg => vsg.GetLevelEndpoints())
					.Select(e => e.Endpoint)
					.Distinct();

				EnsureEndpointsAreLoaded(endpoints);
			}
		}

		private void LoadExtraDataForConnections(IEnumerable<Connection> connections)
		{
			lock (_lock)
			{
				var endpoints = connections
					.SelectMany(x => x.GetEndpoints())
					.Distinct();

				EnsureEndpointsAreLoaded(endpoints);
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
					var endpoints = Api.Endpoints.Read(endpointIdsToRetrieve);
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
					var virtualSignalGroups = Api.VirtualSignalGroups.Read(vsgIdsToRetrieve);
					UpdateVirtualSignalGroups(virtualSignalGroups.Values);
				}
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
