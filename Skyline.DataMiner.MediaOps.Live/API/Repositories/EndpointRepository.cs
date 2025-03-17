namespace Skyline.DataMiner.MediaOps.Live.API.Repositories
{
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class EndpointRepository : Repository<Endpoint>
	{
		public EndpointRepository(SlcConnectivityManagementHelper helper) : base(helper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => Endpoint.DomDefinition;

		protected override Endpoint CreateInstance(DomInstance domInstance)
		{
			return new Endpoint(domInstance);
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(Endpoint.Name):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Name), comparer, (string)value);
				case nameof(Endpoint.Role):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Role), comparer, (int)value);
				case nameof(Endpoint.Element):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Element), comparer, (string)value);
				case nameof(Endpoint.Identifier):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Identifier), comparer, (string)value);
				case nameof(Endpoint.ControlElement):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.ControlElement), comparer, (string)value);
				case nameof(Endpoint.ControlIdentifier):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.ControlIdentifier), comparer, (string)value);
				case nameof(Endpoint.TransportType):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.TransportType), comparer, ApiObjectReference<TransportType>.Convert(value));
			}

			return base.CreateFilter(fieldName, comparer, value);
		}

		protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(Endpoint.Name):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Name), sortOrder, naturalSort);
				case nameof(Endpoint.Role):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Role), sortOrder, naturalSort);
				case nameof(Endpoint.Element):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Element), sortOrder, naturalSort);
				case nameof(Endpoint.Identifier):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Identifier), sortOrder, naturalSort);
				case nameof(Endpoint.ControlElement):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.ControlElement), sortOrder, naturalSort);
				case nameof(Endpoint.ControlIdentifier):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.ControlIdentifier), sortOrder, naturalSort);
				case nameof(Endpoint.TransportType):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.TransportType), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}
	}
}
