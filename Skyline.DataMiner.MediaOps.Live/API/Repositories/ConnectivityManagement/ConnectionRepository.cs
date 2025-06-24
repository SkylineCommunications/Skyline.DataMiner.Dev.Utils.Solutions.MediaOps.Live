namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	using ApiConnection = Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement.Connection;

	public class ConnectionRepository : Repository<Connection>
	{
		internal ConnectionRepository(SlcConnectivityManagementHelper helper, Net.IConnection connection) : base(helper, connection)
		{
		}

		protected internal override DomDefinitionId DomDefinition => ApiConnection.DomDefinition;

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

		public IEnumerable<Connection> GetByEndpointIds(IEnumerable<Guid> endpointIds)
		{
			if (endpointIds == null)
			{
				throw new ArgumentNullException(nameof(endpointIds));
			}

			FilterElement<DomInstance> CreateFilter(Guid id) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Connection.Id),
					new ORFilterElement<DomInstance>(
						DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.Destination).Equal(id),
						DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.ConnectedSource).Equal(id)));

			return FilterQueryExecutor.RetrieveFilteredItems(
				endpointIds,
				x => CreateFilter(x),
				x => Read(x));
		}

		protected internal override Connection CreateInstance(DomInstance domInstance)
		{
			return new Connection(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<Connection> instances)
		{
			foreach (var instance in instances)
			{
				instance.Validate().ThrowIfInvalid();
			}
		}

		protected override void ValidateBeforeDelete(ICollection<Connection> instances)
		{
			// no checks needed
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(ApiConnection.Destination):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.Destination), comparer, ApiObjectReference<Endpoint>.Convert(value));
				case nameof(ApiConnection.IsConnected):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.IsConnected), comparer, (bool)value);
				case nameof(ApiConnection.ConnectedSource):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.ConnectedSource), comparer, ApiObjectReference<Endpoint>.Convert(value));
			}

			return base.CreateFilter(fieldName, comparer, value);
		}

		protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(ApiConnection.Destination):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.Destination), sortOrder, naturalSort);
				case nameof(ApiConnection.IsConnected):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.IsConnected), sortOrder, naturalSort);
				case nameof(ApiConnection.ConnectedSource):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.ConnectedSource), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}
	}
}
