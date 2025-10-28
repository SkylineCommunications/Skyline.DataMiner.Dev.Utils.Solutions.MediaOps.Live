namespace Skyline.DataMiner.MediaOps.Live.API.Caching
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class VirtualSignalGroupEndpointsCache
	{
		private readonly object _lock = new();

		private readonly EndpointsCache _endpoints = new();
		private readonly VirtualSignalGroupsCache _virtualSignalGroups = new();


		public VirtualSignalGroupEndpointsCache()
		{
		}

		public VirtualSignalGroupEndpointsCache(IEnumerable<VirtualSignalGroup> virtualSignalGroups, IEnumerable<Endpoint> endpoints)
		{
			if (virtualSignalGroups != null)
			{
				UpdateVirtualSignalGroups(virtualSignalGroups, []);
			}

			if (endpoints != null)
			{
				UpdateEndpoints(endpoints, []);
			}
		}

		public IReadOnlyDictionary<ApiObjectReference<Endpoint>, Endpoint> Endpoints => _endpoints.Endpoints;

		public IReadOnlyDictionary<string, Endpoint> EndpointsByName => _endpoints.EndpointsByName;

		public IReadOnlyDictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> VirtualSignalGroups => _virtualSignalGroups.VirtualSignalGroups;

		public IReadOnlyDictionary<string, VirtualSignalGroup> VirtualSignalGroupsByName => _virtualSignalGroups.VirtualSignalGroupsByName;

		public Endpoint GetEndpoint(ApiObjectReference<Endpoint> id)
		{
			lock (_lock)
			{
				return _endpoints.GetEndpoint(id);
			}
		}

		public Endpoint GetEndpoint(string name)
		{
			lock (_lock)
			{
				return _endpoints.GetEndpoint(name);
			}
		}

		public bool TryGetEndpoint(ApiObjectReference<Endpoint> id, out Endpoint endpoint)
		{
			lock (_lock)
			{
				return _endpoints.TryGetEndpoint(id, out endpoint);
			}
		}

		public bool TryGetEndpoint(string name, out Endpoint endpoint)
		{
			lock (_lock)
			{
				return _endpoints.TryGetEndpoint(name, out endpoint);
			}
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithTransportMetadata(string fieldName, string value)
		{
			lock (_lock)
			{
				return _endpoints.GetEndpointsWithTransportMetadata(fieldName, value);
			}
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithTransportMetadata(params (string fieldName, string value)[] metadataFilters)
		{
			lock (_lock)
			{
				return _endpoints.GetEndpointsWithTransportMetadata(metadataFilters);
			}
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithElement(DmsElementId elementId)
		{
			lock (_lock)
			{
				return _endpoints.GetEndpointsWithElement(elementId);
			}
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithIdentifier(string identifier)
		{
			lock (_lock)
			{
				return _endpoints.GetEndpointsWithIdentifier(identifier);
			}
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithElementAndIdentifier(DmsElementId elementId, string identifier)
		{
			lock (_lock)
			{
				return _endpoints.GetEndpointsWithElementAndIdentifier(elementId, identifier);
			}
		}

		public VirtualSignalGroup GetVirtualSignalGroup(ApiObjectReference<VirtualSignalGroup> id)
		{
			lock (_lock)
			{
				return _virtualSignalGroups.GetVirtualSignalGroup(id);
			}
		}

		public VirtualSignalGroup GetVirtualSignalGroup(string name)
		{
			lock (_lock)
			{
				return _virtualSignalGroups.GetVirtualSignalGroup(name);
			}
		}

		public bool TryGetVirtualSignalGroup(ApiObjectReference<VirtualSignalGroup> id, out VirtualSignalGroup virtualSignalGroup)
		{
			lock (_lock)
			{
				return _virtualSignalGroups.TryGetVirtualSignalGroup(id, out virtualSignalGroup);
			}
		}

		public bool TryGetVirtualSignalGroup(string name, out VirtualSignalGroup virtualSignalGroup)
		{
			lock (_lock)
			{
				return _virtualSignalGroups.TryGetVirtualSignalGroup(name, out virtualSignalGroup);
			}
		}

		public IReadOnlyCollection<VirtualSignalGroup> GetVirtualSignalGroupsThatContainEndpoint(ApiObjectReference<Endpoint> endpoint)
		{
			lock (_lock)
			{
				return _virtualSignalGroups.GetVirtualSignalGroupsThatContainEndpoint(endpoint);
			}
		}

		public void LoadInitialData(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			lock (_lock)
			{
				var endpointsTask = Task.Run(() => _endpoints.LoadInitialData(api));
				var virtualSignalGroupsTask = Task.Run(() => _virtualSignalGroups.LoadInitialData(api));

				Task.WaitAll(endpointsTask, virtualSignalGroupsTask);
			}
		}

		public void UpdateEndpoints(IEnumerable<Endpoint> updated, IEnumerable<Endpoint> deleted)
		{
			lock (_lock)
			{
				_endpoints.UpdateEndpoints(updated, deleted);
			}
		}

		public void UpdateVirtualSignalGroups(IEnumerable<VirtualSignalGroup> updated, IEnumerable<VirtualSignalGroup> deleted)
		{
			lock (_lock)
			{
				_virtualSignalGroups.UpdateVirtualSignalGroups(updated, deleted);
			}
		}
	}
}
