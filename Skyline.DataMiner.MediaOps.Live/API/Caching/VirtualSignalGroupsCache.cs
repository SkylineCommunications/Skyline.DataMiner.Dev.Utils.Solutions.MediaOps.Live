namespace Skyline.DataMiner.MediaOps.Live.API.Caching
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class VirtualSignalGroupsCache
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> _virtualSignalGroups = new();
		private readonly Dictionary<string, VirtualSignalGroup> _virtualSignalGroupsByName = new();

		private readonly VirtualSignalGroupEndpointsMapping _virtualSignalGroupEndpointsMapping = new();

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

		public IReadOnlyDictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> VirtualSignalGroups => _virtualSignalGroups;

		public IReadOnlyDictionary<string, VirtualSignalGroup> VirtualSignalGroupsByName => _virtualSignalGroupsByName;

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

		public void LoadInitialData(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			var virtualSignalGroups = api.VirtualSignalGroups.ReadAll();

			lock (_lock)
			{
				UpdateVirtualSignalGroups(virtualSignalGroups, []);
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
					}

					_virtualSignalGroups[item.ID] = item;
					_virtualSignalGroupsByName[item.Name] = item;
					_virtualSignalGroupEndpointsMapping.AddOrUpdate(item);
				}

				foreach (var item in deleted)
				{
					_virtualSignalGroups.Remove(item.ID);
					_virtualSignalGroupsByName.Remove(item.Name);
					_virtualSignalGroupEndpointsMapping.Remove(item);
				}
			}
		}
	}
}