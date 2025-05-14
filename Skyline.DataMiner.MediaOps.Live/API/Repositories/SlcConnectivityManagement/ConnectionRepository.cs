namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class ConnectionRepository : Repository<Connection>
	{
		internal ConnectionRepository(SlcConnectivityManagementHelper helper) : base(helper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => Connection.DomDefinition;

		public IDictionary<Guid, Connection> GetByDestinationIds(IEnumerable<Guid> destinationEndpointIds)
		{
			if (destinationEndpointIds == null)
			{
				throw new ArgumentNullException(nameof(destinationEndpointIds));
			}

			FilterElement<DomInstance> CreateFilter(Guid destinationId) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Connection.Id),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.Destination).Equal(destinationId));

			return FilterQueryExecutor.RetrieveFilteredItems(
					destinationEndpointIds,
					x => CreateFilter(x),
					x => Read(x))
				.SafeToDictionary(x => (Guid)x.Destination);
		}

		public IDictionary<Guid, Connection> GetByDestinations(IEnumerable<Endpoint> destinationEndpoints)
		{
			if (destinationEndpoints == null)
			{
				throw new ArgumentNullException(nameof(destinationEndpoints));
			}

			return GetByDestinationIds(destinationEndpoints.Select(x => x.ID));
		}

		public Connection GetByDestinationId(Guid destinationEndpointId)
		{
			return GetByDestinationIds(new[] { destinationEndpointId }).Values.SingleOrDefault();
		}

		public Connection GetByDestination(Endpoint destinationEndpoint)
		{
			if (destinationEndpoint == null)
			{
				throw new ArgumentNullException(nameof(destinationEndpoint));
			}

			return GetByDestinationId(destinationEndpoint.ID);
		}

		protected override Connection CreateInstance(DomInstance domInstance)
		{
			return new Connection(domInstance);
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(Connection.Destination):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.Destination), comparer, ApiObjectReference<Endpoint>.Convert(value));
				case nameof(Connection.IsConnected):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.IsConnected), comparer, (bool)value);
				case nameof(Connection.ConnectedSource):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.ConnectedSource), comparer, ApiObjectReference<Endpoint>.Convert(value));
				case nameof(Connection.PendingConnectedSource):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.PendingConnectedSource), comparer, ApiObjectReference<Endpoint>.Convert(value));
			}

			return base.CreateFilter(fieldName, comparer, value);
		}

		protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(Connection.Destination):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.Destination), sortOrder, naturalSort);
				case nameof(Connection.IsConnected):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.IsConnected), sortOrder, naturalSort);
				case nameof(Connection.ConnectedSource):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.ConnectedSource), sortOrder, naturalSort);
				case nameof(Connection.PendingConnectedSource):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.PendingConnectedSource), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}
	}
}
