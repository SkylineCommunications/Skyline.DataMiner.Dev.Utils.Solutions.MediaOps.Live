namespace Skyline.DataMiner.MediaOps.Live.API.Tools
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.Tools;

	public class VirtualSignalGroupEndpointsMapping
	{
		private readonly ManyToManyMapping<VirtualSignalGroup, ApiObjectReference<Endpoint>> _mapping = new();

		public int VirtualSignalGroupCount => _mapping.Forward.Count;

		public int EndpointCount => _mapping.Reverse.Count;

		public IReadOnlyCollection<ApiObjectReference<Endpoint>> GetEndpoints(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			return _mapping.Forward.TryGetValue(virtualSignalGroup, out var endpoints)
				? endpoints.ToList()
				: Array.Empty<ApiObjectReference<Endpoint>>();
		}

		public IReadOnlyCollection<VirtualSignalGroup> GetVirtualSignalGroups(ApiObjectReference<Endpoint> endpoint)
		{
			return _mapping.Reverse.TryGetValue(endpoint, out var virtualSignalGroups)
				? virtualSignalGroups.ToList()
				: Array.Empty<VirtualSignalGroup>();
		}

		public void Add(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			var endpoints = virtualSignalGroup.GetLevelEndpoints()
				.Select(x => x.Endpoint);

			foreach (var endpoint in endpoints)
			{
				_mapping.TryAdd(virtualSignalGroup, endpoint);
			}
		}

		public void Remove(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			_mapping.TryRemoveForward(virtualSignalGroup);
		}

		public void AddOrUpdate(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			Remove(virtualSignalGroup);
			Add(virtualSignalGroup);
		}

		public void Clear()
		{
			_mapping.Clear();
		}

		public bool Contains(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			return _mapping.Forward.ContainsKey(virtualSignalGroup);
		}

		public bool Contains(ApiObjectReference<Endpoint> endpoint)
		{
			return _mapping.Reverse.ContainsKey(endpoint);
		}

		public bool Contains(VirtualSignalGroup virtualSignalGroup, ApiObjectReference<Endpoint> endpoint)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			return _mapping.Contains(virtualSignalGroup, endpoint);
		}
	}
}
