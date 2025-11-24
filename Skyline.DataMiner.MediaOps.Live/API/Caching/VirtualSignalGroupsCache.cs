namespace Skyline.DataMiner.MediaOps.Live.API.Caching
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Tools;

	using Categories = Skyline.DataMiner.Utils.Categories.API.Objects;

	public class VirtualSignalGroupsCache
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> _virtualSignalGroups = new();
		private readonly Dictionary<string, VirtualSignalGroup> _virtualSignalGroupsByName = new();

		private readonly OneToOneMapping<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroupState> _virtualSignalGroupStates = new();

		private readonly VirtualSignalGroupEndpointsMapping _virtualSignalGroupEndpointsMapping = new();
		private readonly VirtualSignalGroupCategoriesMapping _virtualSignalGroupCategoriesMapping = new();

		public VirtualSignalGroupsCache()
		{
		}

		public VirtualSignalGroupsCache(IEnumerable<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups != null)
			{
				UpdateVirtualSignalGroups(virtualSignalGroups, []);
			}
		}

		public VirtualSignalGroupsCache(IEnumerable<VirtualSignalGroup> virtualSignalGroups, IEnumerable<VirtualSignalGroupState> virtualSignalGroupStates)
		{
			if (virtualSignalGroups != null)
			{
				UpdateVirtualSignalGroups(virtualSignalGroups, []);
			}

			if (virtualSignalGroupStates != null)
			{
				UpdateVirtualSignalGroupStates(virtualSignalGroupStates, []);
			}
		}

		public IReadOnlyDictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> VirtualSignalGroups => _virtualSignalGroups;

		public IReadOnlyDictionary<string, VirtualSignalGroup> VirtualSignalGroupsByName => _virtualSignalGroupsByName;

		public VirtualSignalGroup GetVirtualSignalGroup(ApiObjectReference<VirtualSignalGroup> id)
		{
			lock (_lock)
			{
				if (!TryGetVirtualSignalGroup(id, out var virtualSignalGroup))
				{
					throw new ArgumentException($"Couldn't find virtual signal group with ID {id.ID}", nameof(id));
				}

				return virtualSignalGroup;
			}
		}

		public VirtualSignalGroup GetVirtualSignalGroup(string name)
		{
			lock (_lock)
			{
				if (!TryGetVirtualSignalGroup(name, out var virtualSignalGroup))
				{
					throw new ArgumentException($"Couldn't find virtual signal group with name '{name}'", nameof(name));
				}

				return virtualSignalGroup;
			}
		}

		public bool TryGetVirtualSignalGroup(ApiObjectReference<VirtualSignalGroup> id, out VirtualSignalGroup virtualSignalGroup)
		{
			lock (_lock)
			{
				return _virtualSignalGroups.TryGetValue(id, out virtualSignalGroup);
			}
		}

		public bool TryGetVirtualSignalGroup(string name, out VirtualSignalGroup virtualSignalGroup)
		{
			lock (_lock)
			{
				return _virtualSignalGroupsByName.TryGetValue(name, out virtualSignalGroup);
			}
		}

		public VirtualSignalGroupState GetVirtualSignalGroupState(ApiObjectReference<VirtualSignalGroup> virtualSignalGroup)
		{
			lock (_lock)
			{
				if (!TryGetVirtualSignalGroupState(virtualSignalGroup, out var state))
				{
					throw new ArgumentException($"Couldn't find virtual signal group state for virtual signal group with ID {virtualSignalGroup.ID}", nameof(virtualSignalGroup));
				}

				return state;
			}
		}

		public bool TryGetVirtualSignalGroupState(ApiObjectReference<VirtualSignalGroup> virtualSignalGroup, out VirtualSignalGroupState state)
		{
			lock (_lock)
			{
				return _virtualSignalGroupStates.TryGetForward(virtualSignalGroup, out state);
			}
		}

		public bool IsLocked(ApiObjectReference<VirtualSignalGroup> virtualSignalGroup)
		{
			lock (_lock)
			{
				return TryGetVirtualSignalGroupState(virtualSignalGroup, out var state) && state.IsLocked;
			}
		}

		public bool IsLocked(ApiObjectReference<VirtualSignalGroup> virtualSignalGroup, out string lockedBy, out DateTimeOffset lockedTime, out string reason)
		{
			lock (_lock)
			{
				if (TryGetVirtualSignalGroupState(virtualSignalGroup, out var state) && state.IsLocked)
				{
					lockedBy = state.LockedBy;
					lockedTime = state.LockTime;
					reason = state.LockReason;
					return true;
				}
				else
				{
					lockedBy = default;
					lockedTime = default;
					reason = default;
					return false;
				}
			}
		}

		public IReadOnlyCollection<VirtualSignalGroup> GetVirtualSignalGroupsThatContainEndpoint(ApiObjectReference<Endpoint> endpoint)
		{
			lock (_lock)
			{
				return _virtualSignalGroupEndpointsMapping.GetVirtualSignalGroups(endpoint);
			}
		}

		public IReadOnlyCollection<VirtualSignalGroup> GetVirtualSignalGroupsInCategory(Categories.ApiObjectReference<Categories.Category> category)
		{
			lock (_lock)
			{
				return _virtualSignalGroupCategoriesMapping.GetVirtualSignalGroups(category);
			}
		}

		public void LoadInitialData(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			var virtualSignalGroupsTask = Task.Run(() => api.VirtualSignalGroups.ReadAll());
			var virtualSignalGroupStatesTask = Task.Run(() => api.VirtualSignalGroupStates.ReadAll());

			Task.WaitAll(virtualSignalGroupsTask, virtualSignalGroupStatesTask);

			lock (_lock)
			{
				UpdateVirtualSignalGroups(virtualSignalGroupsTask.Result, []);
				UpdateVirtualSignalGroupStates(virtualSignalGroupStatesTask.Result, []);
			}
		}

		public void UpdateVirtualSignalGroups(IEnumerable<VirtualSignalGroup> updated, IEnumerable<VirtualSignalGroup> deleted)
		{
			if (updated is null)
			{
				throw new ArgumentNullException(nameof(updated));
			}

			if (deleted is null)
			{
				throw new ArgumentNullException(nameof(deleted));
			}

			lock (_lock)
			{
				foreach (var item in updated)
				{
					// Remove old mappings if they exist
					if (_virtualSignalGroups.TryGetValue(item.ID, out var existing))
					{
						_virtualSignalGroupsByName.Remove(existing.Name);
						_virtualSignalGroupEndpointsMapping.Remove(existing);
						_virtualSignalGroupCategoriesMapping.Remove(existing);
					}

					_virtualSignalGroups[item.ID] = item;
					_virtualSignalGroupsByName[item.Name] = item;
					_virtualSignalGroupEndpointsMapping.AddOrUpdate(item);
					_virtualSignalGroupCategoriesMapping.AddOrUpdate(item);
				}

				foreach (var item in deleted)
				{
					_virtualSignalGroups.Remove(item.ID);
					_virtualSignalGroupsByName.Remove(item.Name);
					_virtualSignalGroupEndpointsMapping.Remove(item);
					_virtualSignalGroupCategoriesMapping.Remove(item);
				}
			}
		}

		public void UpdateVirtualSignalGroupStates(IEnumerable<VirtualSignalGroupState> updated, IEnumerable<VirtualSignalGroupState> deleted)
		{
			if (updated is null)
			{
				throw new ArgumentNullException(nameof(updated));
			}

			if (deleted is null)
			{
				throw new ArgumentNullException(nameof(deleted));
			}

			lock (_lock)
			{
				foreach (var item in updated)
				{
					_virtualSignalGroupStates.AddOrUpdate(item.VirtualSignalGroupReference, item);
				}

				foreach (var item in deleted)
				{
					_virtualSignalGroupStates.TryRemoveReverse(item);
				}
			}
		}
	}
}