namespace Skyline.DataMiner.MediaOps.Live.API.Caching
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Skyline.DataMiner.MediaOps.Live.API.Enums;
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

		public IReadOnlyCollection<VirtualSignalGroup> GetAllVirtualSignalGroups()
		{
			lock (_lock)
			{
				return _virtualSignalGroups.Values.ToList();
			}
		}

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

		public IReadOnlyCollection<VirtualSignalGroup> GetVirtualSignalGroupsWithRole(EndpointRole role)
		{
			lock (_lock)
			{
				return GetAllVirtualSignalGroups().Where(e => e.Role == role).ToList();
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
				return IsLocked(virtualSignalGroup, out _, out _, out _);
			}
		}

		public bool IsLocked(ApiObjectReference<VirtualSignalGroup> virtualSignalGroup, out string lockedBy, out DateTimeOffset lockedTime, out string reason)
		{
			lock (_lock)
			{
				if (!TryGetVirtualSignalGroupState(virtualSignalGroup, out var state) || !state.IsLocked)
				{
					lockedBy = null;
					lockedTime = default;
					reason = null;
					return false;
				}

				lockedBy = state.LockedBy;
				lockedTime = state.LockTime;
				reason = state.LockReason;
				return true;
			}
		}

		public bool IsProtected(ApiObjectReference<VirtualSignalGroup> virtualSignalGroup)
		{
			lock (_lock)
			{
				return IsProtected(virtualSignalGroup, out _, out _, out _);
			}
		}

		public bool IsProtected(ApiObjectReference<VirtualSignalGroup> virtualSignalGroup, out string lockedBy, out DateTimeOffset lockedTime, out string reason)
		{
			lock (_lock)
			{
				if (!TryGetVirtualSignalGroupState(virtualSignalGroup, out var state) || !state.IsProtected)
				{
					lockedBy = null;
					lockedTime = default;
					reason = null;
					return false;
				}

				lockedBy = state.LockedBy;
				lockedTime = state.LockTime;
				reason = state.LockReason;
				return true;
			}
		}

		public bool IsUnlocked(ApiObjectReference<VirtualSignalGroup> virtualSignalGroup)
		{
			lock (_lock)
			{
				return !TryGetVirtualSignalGroupState(virtualSignalGroup, out var state) || state.IsUnlocked;
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

		public void LoadInitialData(IMediaOpsLiveApi api)
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