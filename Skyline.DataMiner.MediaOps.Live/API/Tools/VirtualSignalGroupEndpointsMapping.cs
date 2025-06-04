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

		public bool TryGetEndpoints(VirtualSignalGroup virtualSignalGroup, out ICollection<ApiObjectReference<Endpoint>> endpoints)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			return _mapping.Forward.TryGetValue(virtualSignalGroup, out endpoints);
		}

		public bool TryGetVirtualSignalGroups(ApiObjectReference<Endpoint> endpoint, out ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			return _mapping.Reverse.TryGetValue(endpoint, out virtualSignalGroups);
		}

		public void Add(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			var endpoints = virtualSignalGroup.GetEndpoints()
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

			_mapping.RemoveForward(virtualSignalGroup);
		}

		public void Update(VirtualSignalGroup virtualSignalGroup)
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
