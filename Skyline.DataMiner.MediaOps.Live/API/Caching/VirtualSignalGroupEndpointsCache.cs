namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	using Categories = Skyline.DataMiner.Solutions.Categories.API;

	/// <summary>
	/// Coordinates caching and updates for both endpoints and virtual signal groups.
	/// Access the underlying caches via the <see cref="Endpoints"/> and <see cref="VirtualSignalGroups"/> properties.
	/// </summary>
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

		/// <summary>
		/// Gets the endpoints cache. Use this to access all endpoint-related query methods.
		/// </summary>
		public EndpointsCache Endpoints => _endpoints;

		/// <summary>
		/// Gets the virtual signal groups cache. Use this to access all virtual signal group-related query methods.
		/// </summary>
		public VirtualSignalGroupsCache VirtualSignalGroups => _virtualSignalGroups;

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

		public IReadOnlyCollection<Endpoint> GetEndpointsWithTransportType(ApiObjectReference<TransportType> transportType)
		{
			lock (_lock)
			{
				return _endpoints.GetEndpointsWithTransportType(transportType);
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

		/// <summary>
		/// Gets all endpoints that are part of the specified virtual signal group.
		/// </summary>
		/// <param name="virtualSignalGroupRef">The reference to the virtual signal group.</param>
		/// <returns>A read-only collection of <see cref="Endpoint"/> objects that belong to the specified virtual signal group.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the virtual signal group with the specified reference cannot be found,
		/// or if any endpoint referenced by the group cannot be found.
		/// </exception>
		public IReadOnlyCollection<Endpoint> GetEndpointsInVirtualSignalGroup(ApiObjectReference<VirtualSignalGroup> virtualSignalGroupRef)
		{
			lock (_lock)
			{
				var virtualSignalGroup = GetVirtualSignalGroup(virtualSignalGroupRef);

				var endpoints = virtualSignalGroup.GetLevelEndpoints()
					.Select(x => GetEndpoint(x.Endpoint))
					.Distinct()
					.ToList();

				return endpoints;
			}
		}

		public IReadOnlyCollection<VirtualSignalGroup> GetVirtualSignalGroupsInCategory(Categories.ApiObjectReference<Categories.Category> category)
		{
			lock (_lock)
			{
				return _virtualSignalGroups.GetVirtualSignalGroupsInCategory(category);
			}
		}

		public VirtualSignalGroupState GetVirtualSignalGroupState(ApiObjectReference<VirtualSignalGroup> virtualSignalGroup)
		{
			lock (_lock)
			{
				return _virtualSignalGroups.GetVirtualSignalGroupState(virtualSignalGroup);
			}
		}

		public bool TryGetVirtualSignalGroupState(ApiObjectReference<VirtualSignalGroup> virtualSignalGroup, out VirtualSignalGroupState state)
		{
			lock (_lock)
			{
				return _virtualSignalGroups.TryGetVirtualSignalGroupState(virtualSignalGroup, out state);
			}
		}

		public void LoadInitialData(IMediaOpsLiveApi api)
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
