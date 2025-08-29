namespace Skyline.DataMiner.MediaOps.Live.API.Tools
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Subscriptions;

	public class VirtualSignalGroupEndpointsCache : IDisposable
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<Endpoint>, Endpoint> _endpoints = new();
		private readonly Dictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> _virtualSignalGroups = new();
		private readonly Dictionary<ApiObjectReference<Level>, Level> _levels = new();

		private readonly VirtualSignalGroupEndpointsMapping _virtualSignalGroupEndpointsMapping = new();

		private RepositorySubscription<Endpoint> _subscriptionEndpoints;
		private RepositorySubscription<VirtualSignalGroup> _subscriptionVirtualSignalGroups;
		private RepositorySubscription<Level> _subscriptionLevels;

		public VirtualSignalGroupEndpointsCache(MediaOpsLiveApi api, bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));

			Initialize(subscribe);
		}

		public MediaOpsLiveApi Api { get; }

		public IReadOnlyDictionary<ApiObjectReference<Endpoint>, Endpoint> Endpoints => _endpoints;

		public IReadOnlyDictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> VirtualSignalGroups => _virtualSignalGroups;

		public IReadOnlyDictionary<ApiObjectReference<Level>, Level> Levels => _levels;

		public bool IsSubscribed { get; private set; }

		public Endpoint GetEndpoint(ApiObjectReference<Endpoint> id)
		{
			if (!TryGetEndpoint(id, out var endpoint))
			{
				throw new ArgumentException($"Couldn't find endpoint with ID {id.ID}", nameof(id));
			}

			return endpoint;
		}

		public bool TryGetEndpoint(ApiObjectReference<Endpoint> id, out Endpoint endpoint)
		{
			return _endpoints.TryGetValue(id, out endpoint);
		}

		public VirtualSignalGroup GetVirtualSignalGroup(ApiObjectReference<VirtualSignalGroup> id)
		{
			if (!TryGetVirtualSignalGroup(id, out var virtualSignalGroup))
			{
				throw new ArgumentException($"Couldn't find virtual signal group with ID {id.ID}", nameof(id));
			}

			return virtualSignalGroup;
		}

		public bool TryGetVirtualSignalGroup(ApiObjectReference<VirtualSignalGroup> id, out VirtualSignalGroup virtualSignalGroup)
		{
			return _virtualSignalGroups.TryGetValue(id, out virtualSignalGroup);
		}

		public IReadOnlyCollection<VirtualSignalGroup> GetVirtualSignalGroupsThatContainEndpoint(ApiObjectReference<Endpoint> endpoint)
		{
			return _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(endpoint);
		}

		public Level GetEndpoint(ApiObjectReference<Level> id)
		{
			if (!TryGetLevel(id, out var level))
			{
				throw new ArgumentException($"Couldn't find level with ID {id.ID}", nameof(id));
			}

			return level;
		}

		public bool TryGetLevel(ApiObjectReference<Level> id, out Level level)
		{
			return _levels.TryGetValue(id, out level);
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

				_subscriptionLevels = Api.Levels.Subscribe();
				_subscriptionLevels.Changed += Levels_Changed;

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

				_subscriptionLevels.Changed -= Levels_Changed;
				_subscriptionLevels.Dispose();

				IsSubscribed = false;
			}
		}

		public virtual void Dispose()
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
			var levelsTask = Task.Run(() => Api.Levels.ReadAll());
			var endpointsTask = Task.Run(() => Api.Endpoints.ReadAll());
			var virtualSignalGroupsTask = Task.Run(() => Api.VirtualSignalGroups.ReadAll());

			Task.WaitAll(levelsTask, endpointsTask, virtualSignalGroupsTask);

			UpdateLevels(levelsTask.Result);
			UpdateEndpoints(endpointsTask.Result);
			UpdateVirtualSignalGroups(virtualSignalGroupsTask.Result);
		}

		private void Levels_Changed(object sender, ApiObjectsChangedEvent<Level> e)
		{
			lock (_lock)
			{
				Debug.WriteLine($"Levels changed: {e}");

				UpdateLevels(e.Created.Concat(e.Updated), e.Deleted);
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

		private void UpdateLevels(IEnumerable<Level> updated, IEnumerable<Level> deleted = null)
		{
			lock (_lock)
			{
				if (updated != null)
				{
					foreach (var item in updated)
					{
						_levels[item.ID] = item;
					}
				}

				if (deleted != null)
				{
					foreach (var item in deleted)
					{
						_levels.Remove(item);
					}
				}
			}
		}
	}
}
