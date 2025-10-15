namespace Skyline.DataMiner.MediaOps.Live.API.Caching
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;

	using Skyline.DataMiner.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Subscriptions;

	public class VirtualSignalGroupEndpointsCache : IDisposable
	{
		private readonly object _lock = new();

		private readonly ConcurrentDictionary<ApiObjectReference<Endpoint>, Endpoint> _endpoints = new();
		private readonly ConcurrentDictionary<string, Endpoint> _endpointsByName = new();
		private readonly ConcurrentDictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> _virtualSignalGroups = new();
		private readonly ConcurrentDictionary<string, VirtualSignalGroup> _virtualSignalGroupsByName = new();

		private readonly VirtualSignalGroupEndpointsMapping _virtualSignalGroupEndpointsMapping = new();

		private RepositorySubscription<Endpoint> _subscriptionEndpoints;
		private RepositorySubscription<VirtualSignalGroup> _subscriptionVirtualSignalGroups;

		public VirtualSignalGroupEndpointsCache(MediaOpsLiveApi api, bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));

			Initialize(subscribe);
		}

		public MediaOpsLiveApi Api { get; }

		public IReadOnlyDictionary<ApiObjectReference<Endpoint>, Endpoint> Endpoints => _endpoints;

		public IReadOnlyDictionary<string, Endpoint> EndpointsByName => _endpointsByName;

		public IReadOnlyDictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> VirtualSignalGroups => _virtualSignalGroups;

		public IReadOnlyDictionary<string, VirtualSignalGroup> VirtualSignalGroupsByName => _virtualSignalGroupsByName;

		public bool IsSubscribed { get; private set; }

		public Endpoint GetEndpoint(ApiObjectReference<Endpoint> id)
		{
			if (!TryGetEndpoint(id, out var endpoint))
			{
				throw new ArgumentException($"Couldn't find endpoint with ID {id.ID}", nameof(id));
			}

			return endpoint;
		}

		public Endpoint GetEndpoint(string name)
		{
			if (!TryGetEndpoint(name, out var endpoint))
			{
				throw new ArgumentException($"Couldn't find endpoint with name '{name}'", nameof(name));
			}

			return endpoint;
		}

		public bool TryGetEndpoint(ApiObjectReference<Endpoint> id, out Endpoint endpoint)
		{
			return _endpoints.TryGetValue(id, out endpoint);
		}

		public bool TryGetEndpoint(string name, out Endpoint endpoint)
		{
			return _endpointsByName.TryGetValue(name, out endpoint);
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithTransportMetadata(params (string fieldName, string value)[] metadataFilters)
		{
			if (metadataFilters is null)
			{
				throw new ArgumentNullException(nameof(metadataFilters));
			}

			return _endpoints.Values.WithTransportMetadata(metadataFilters).ToList();
		}

		public VirtualSignalGroup GetVirtualSignalGroup(ApiObjectReference<VirtualSignalGroup> id)
		{
			if (!TryGetVirtualSignalGroup(id, out var virtualSignalGroup))
			{
				throw new ArgumentException($"Couldn't find virtual signal group with ID {id.ID}", nameof(id));
			}

			return virtualSignalGroup;
		}

		public VirtualSignalGroup GetVirtualSignalGroup(string name)
		{
			if (!TryGetVirtualSignalGroup(name, out var virtualSignalGroup))
			{
				throw new ArgumentException($"Couldn't find virtual signal group with name '{name}'", nameof(name));
			}

			return virtualSignalGroup;
		}

		public bool TryGetVirtualSignalGroup(ApiObjectReference<VirtualSignalGroup> id, out VirtualSignalGroup virtualSignalGroup)
		{
			return _virtualSignalGroups.TryGetValue(id, out virtualSignalGroup);
		}

		public bool TryGetVirtualSignalGroup(string name, out VirtualSignalGroup virtualSignalGroup)
		{
			return _virtualSignalGroupsByName.TryGetValue(name, out virtualSignalGroup);
		}

		public IReadOnlyCollection<VirtualSignalGroup> GetVirtualSignalGroupsThatContainEndpoint(ApiObjectReference<Endpoint> endpoint)
		{
			lock (_lock)
			{
				return _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(endpoint);
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
				_subscriptionEndpoints.Changed += Endpoints_Changed;

				_subscriptionVirtualSignalGroups = Api.VirtualSignalGroups.Subscribe();
				_subscriptionVirtualSignalGroups.Changed += VirtualSignalGroups_Changed;

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
				_subscriptionEndpoints.Dispose();

				_subscriptionVirtualSignalGroups.Changed -= VirtualSignalGroups_Changed;
				_subscriptionVirtualSignalGroups.Dispose();

				IsSubscribed = false;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
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

				LoadInitialData();
			}
		}

		private void LoadInitialData()
		{
			var endpointsTask = Task.Run(() => Api.Endpoints.ReadAll());
			var virtualSignalGroupsTask = Task.Run(() => Api.VirtualSignalGroups.ReadAll());

			Task.WaitAll(endpointsTask, virtualSignalGroupsTask);

			UpdateEndpoints(endpointsTask.Result);
			UpdateVirtualSignalGroups(virtualSignalGroupsTask.Result);
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

		private void UpdateEndpoints(IEnumerable<Endpoint> updated, IEnumerable<Endpoint> deleted = null)
		{
			lock (_lock)
			{
				if (updated != null)
				{
					foreach (var item in updated)
					{
						// Remove old name if it exists
						if (_endpoints.TryGetValue(item.ID, out var existing))
						{
							_endpointsByName.TryRemove(existing.Name, out _);
						}

						_endpoints[item.ID] = item;
						_endpointsByName[item.Name] = item;
					}
				}

				if (deleted != null)
				{
					foreach (var item in deleted)
					{
						_endpoints.TryRemove(item.ID, out _);
						_endpointsByName.TryRemove(item.Name, out _);
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
						// Remove old name if it exists
						if (_virtualSignalGroups.TryGetValue(item.ID, out var existing))
						{
							_virtualSignalGroupsByName.TryRemove(existing.Name, out _);
						}

						_virtualSignalGroups[item.ID] = item;
						_virtualSignalGroupsByName[item.Name] = item;
						_virtualSignalGroupEndpointsMapping.AddOrUpdate(item);
					}
				}

				if (deleted != null)
				{
					foreach (var item in deleted)
					{
						_virtualSignalGroups.TryRemove(item.ID, out _);
						_virtualSignalGroupsByName.TryRemove(item.Name, out _);
						_virtualSignalGroupEndpointsMapping.Remove(item);
					}
				}
			}
		}
	}
}
