namespace Skyline.DataMiner.MediaOps.Live.DOM.Helpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Core.DataMinerSystem.Common;

	using Net;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class SlcConnectivityManagementHelper : DomModuleHelperBase
	{
		public SlcConnectivityManagementHelper(ICommunication communication) : base(SlcConnectivityManagementIds.ModuleId, communication.SendMessages)
		{
		}

		public SlcConnectivityManagementHelper(IConnection connection) : base(SlcConnectivityManagementIds.ModuleId, connection.HandleMessages)
		{
		}

		#region Transport Types

		public IEnumerable<TransportTypeInstance> GetAllTransportTypes()
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.TransportType.Id);

			return GetTransportTypeIterator(filter);
		}

		#endregion

		#region Levels

		public IEnumerable<LevelInstance> GetAllLevels()
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Level.Id);

			return GetLevelsIterator(filter);
		}

		#endregion

		#region Endpoints

		public IEnumerable<EndpointInstance> GetAllEndpoints()
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id);

			return GetEndpointsIterator(filter);
		}

		public IEnumerable<EndpointInstance> GetEndpoints(SlcConnectivityManagementIds.Enums.Role role)
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Role).Equal((int)role));

			return GetEndpointsIterator(filter);
		}

		public IEnumerable<EndpointInstance> GetEndpoints(SlcConnectivityManagementIds.Enums.Role role, string nameFilter)
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Role).Equal((int)role))
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Name).Contains(nameFilter));

			return GetEndpointsIterator(filter);
		}

		public IEnumerable<EndpointInstance> GetEndpoints(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetEndpointsIterator(filter);
		}

		public IDictionary<Guid, EndpointInstance> GetEndpoints(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			FilterElement<DomInstance> CreateFilter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
					ids,
					x => CreateFilter(x),
					x => GetEndpoints(x))
				.SafeToDictionary(x => x.ID.Id);
		}

		public ILookup<VirtualSignalGroupInstance, EndpointInstance> GetEndpoints(ICollection<VirtualSignalGroupInstance> vsgs)
		{
			if (vsgs == null)
			{
				throw new ArgumentNullException(nameof(vsgs));
			}

			var endpointIds = vsgs
				.SelectMany(vsg => vsg.VirtualSignalGroupLevels
					.Select(level => level.Endpoint ?? Guid.Empty)
					.Where(id => id != Guid.Empty))
				.Distinct();

			var endpoints = GetEndpoints(endpointIds);

			return vsgs
				.SelectMany(vsg => vsg.VirtualSignalGroupLevels
					.Where(level => level.Endpoint != null)
					.Select(level => new
					{
						VirtualSignalGroup = vsg,
						Endpoint = endpoints.TryGetValue(level.Endpoint ?? Guid.Empty, out var endpoint) ? endpoint : null,
					}))
				.Where(x => x.Endpoint != null)
				.ToLookup(x => x.VirtualSignalGroup, x => x.Endpoint);
		}

		public EndpointInstance GetEndpoint(Guid id)
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return GetEndpointsIterator(filter).FirstOrDefault();
		}

		public EndpointInstance GetEndpoint(SlcConnectivityManagementIds.Enums.Role role, int dmaId, int elementId, string identifier)
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Role).Equal((int)role))
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Element).Equal($"{dmaId}/{elementId}"))
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Identifier).Equal(identifier));

			return GetEndpointsIterator(filter).FirstOrDefault();
		}

		public IEnumerable<EndpointInstance> GetElementEndpoints(string dmaElementId)
		{
			if (String.IsNullOrWhiteSpace(dmaElementId))
			{
				throw new ArgumentException($"'{nameof(dmaElementId)}' cannot be null or whitespace.", nameof(dmaElementId));
			}

			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Element).Equal(dmaElementId));

			return GetEndpointsIterator(filter);
		}

		public IEnumerable<EndpointInstance> GetElementEndpoints(string dmaElementId, IEnumerable<string> identifiers)
		{
			if (String.IsNullOrWhiteSpace(dmaElementId))
			{
				throw new ArgumentException($"'{nameof(dmaElementId)}' cannot be null or whitespace.", nameof(dmaElementId));
			}

			if (identifiers == null)
			{
				throw new ArgumentNullException(nameof(identifiers));
			}

			FilterElement<DomInstance> CreateFilter(string identifier) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Element).Equal(dmaElementId),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Identifier).Equal(identifier));

			return FilterQueryExecutor.RetrieveFilteredItems(
					identifiers,
					x => CreateFilter(x),
					x => GetEndpoints(x));
		}

		#endregion

		#region Virtual Signal Groups

		public IEnumerable<VirtualSignalGroupInstance> GetAllVirtualSignalGroups()
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.VirtualSignalGroup.Id);

			return GetVirtualSignalGroupsIterator(filter);
		}

		public IEnumerable<VirtualSignalGroupInstance> GetVirtualSignalGroups(SlcConnectivityManagementIds.Enums.Role role, string nameFilter)
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.VirtualSignalGroup.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Role).Equal((int)role))
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Name).Contains(nameFilter));

			return GetVirtualSignalGroupsIterator(filter);
		}

		public IEnumerable<VirtualSignalGroupInstance> GetVirtualSignalGroups(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetVirtualSignalGroupsIterator(filter);
		}

		public IDictionary<Guid, VirtualSignalGroupInstance> GetVirtualSignalGroups(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			FilterElement<DomInstance> CreateFilter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.VirtualSignalGroup.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
					ids,
					x => CreateFilter(x),
					x => GetVirtualSignalGroups(x))
				.SafeToDictionary(x => x.ID.Id);
		}

		public IEnumerable<VirtualSignalGroupInstance> GetVirtualSignalGroupsContainingEndpoints(IEnumerable<Guid> endpointIds)
		{
			if (endpointIds == null)
			{
				throw new ArgumentNullException(nameof(endpointIds));
			}

			FilterElement<DomInstance> CreateFilter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.VirtualSignalGroup.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevels.Endpoint).Equal(id));

			var vsgs = FilterQueryExecutor.RetrieveFilteredItems(
					endpointIds,
					x => CreateFilter(x),
					x => GetVirtualSignalGroups(x));

			return vsgs;
		}

		public IEnumerable<VirtualSignalGroupInstance> GetVirtualSignalGroupsContainingEndpoints(IEnumerable<EndpointInstance> endpoints)
		{
			if (endpoints == null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			return GetVirtualSignalGroupsContainingEndpoints(endpoints.Select(x => x.ID.Id));
		}

		#endregion

		#region Connections

		public IEnumerable<ConnectionInstance> GetAllConnections()
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Connection.Id);

			return GetConnectionsIterator(filter);
		}

		public IEnumerable<ConnectionInstance> GetConnections(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetConnectionsIterator(filter);
		}

		public IDictionary<Guid, ConnectionInstance> GetConnections(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			FilterElement<DomInstance> CreateFilter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Connection.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
					ids,
					x => CreateFilter(x),
					x => GetConnections(x))
				.SafeToDictionary(x => x.ID.Id);
		}

		public ConnectionInstance GetConnection(Guid id)
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Connection.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return GetConnections(filter).FirstOrDefault();
		}

		public IDictionary<Guid, ConnectionInstance> GetConnectionsForDestinations(IEnumerable<Guid> destinationEndpointIds)
		{
			if (destinationEndpointIds == null)
			{
				throw new ArgumentNullException(nameof(destinationEndpointIds));
			}

			FilterElement<DomInstance> CreateFilter(Guid destinationEndpointId) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Connection.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.Destination).Equal(destinationEndpointId));

			return FilterQueryExecutor.RetrieveFilteredItems(
					destinationEndpointIds,
					x => CreateFilter(x),
					x => GetConnections(x))
				.SafeToDictionary(x => (Guid)x.ConnectionInfo.Destination);
		}

		public IDictionary<Guid, ConnectionInstance> GetConnectionsForDestinations(IEnumerable<EndpointInstance> destinationEndpoints)
		{
			if (destinationEndpoints == null)
			{
				throw new ArgumentNullException(nameof(destinationEndpoints));
			}

			return GetConnectionsForDestinations(destinationEndpoints.Select(x => x.ID.Id));
		}

		public ConnectionInstance GetConnectionForDestination(Guid destinationEndpointId)
		{
			return GetConnectionsForDestinations(new[] { destinationEndpointId }).Values.SingleOrDefault();
		}

		#endregion

		#region Iterators

		private IEnumerable<TransportTypeInstance> GetTransportTypeIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new TransportTypeInstance(x));
		}

		private IEnumerable<LevelInstance> GetLevelsIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new LevelInstance(x));
		}

		private IEnumerable<EndpointInstance> GetEndpointsIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new EndpointInstance(x));
		}

		private IEnumerable<VirtualSignalGroupInstance> GetVirtualSignalGroupsIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new VirtualSignalGroupInstance(x));
		}

		private IEnumerable<ConnectionInstance> GetConnectionsIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new ConnectionInstance(x));
		}

		#endregion
	}
}
