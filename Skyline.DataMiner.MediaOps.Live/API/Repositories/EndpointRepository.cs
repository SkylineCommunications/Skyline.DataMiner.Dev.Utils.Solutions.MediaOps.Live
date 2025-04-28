namespace Skyline.DataMiner.MediaOps.Live.API.Repositories
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Data;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class EndpointRepository : Repository<Endpoint>
	{
		public EndpointRepository(SlcConnectivityManagementHelper helper) : base(helper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => Endpoint.DomDefinition;

		public IEnumerable<Endpoint> GetByElement(string dmaElementId)
		{
			if (String.IsNullOrWhiteSpace(dmaElementId))
			{
				throw new ArgumentException($"'{nameof(dmaElementId)}' cannot be null or whitespace.", nameof(dmaElementId));
			}

			var filter = new ANDFilterElement<DomInstance>(
				DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id),
				DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Element).Equal(dmaElementId));

			return Read(filter);
		}

		public IEnumerable<Endpoint> GetByElementAndIdentifiers(string dmaElementId, IEnumerable<string> identifiers)
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
					x => Read(x));
		}

		public IEnumerable<Endpoint> GetByMulticasts(IEnumerable<Multicast> multicasts)
		{
			if (multicasts == null)
			{
				throw new ArgumentNullException(nameof(multicasts));
			}

			FilterElement<DomInstance> CreateFilter(Multicast multicast) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id),
					CreateMulticastFilter(multicast));

			return FilterQueryExecutor.RetrieveFilteredItems(
				multicasts,
				mc => CreateFilter(mc),
				f => Read(f));
		}

		public IEnumerable<Endpoint> GetByElementAndMulticasts(string dmaElementId, IEnumerable<Multicast> multicasts)
		{
			if (String.IsNullOrWhiteSpace(dmaElementId))
			{
				throw new ArgumentException($"'{nameof(dmaElementId)}' cannot be null or whitespace.", nameof(dmaElementId));
			}

			if (multicasts == null)
			{
				throw new ArgumentNullException(nameof(multicasts));
			}

			FilterElement<DomInstance> CreateFilter(Multicast multicast) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Element).Equal(dmaElementId),
					CreateMulticastFilter(multicast));

			return FilterQueryExecutor.RetrieveFilteredItems(
				multicasts,
				mc => CreateFilter(mc),
				f => Read(f));
		}

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
				case nameof(Endpoint.TransportTypeTSoIP.MulticastIP):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeTsoip.MulticastIP), comparer, (string)value);
				case nameof(Endpoint.TransportTypeTSoIP.Port):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeTsoip.Port), comparer, (int)value);
				case nameof(Endpoint.TransportTypeTSoIP.SourceIP):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeTsoip.SourceIP), comparer, (string)value);
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
				case nameof(Endpoint.TransportTypeTSoIP.MulticastIP):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeTsoip.MulticastIP), sortOrder, naturalSort);
				case nameof(Endpoint.TransportTypeTSoIP.Port):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeTsoip.Port), sortOrder, naturalSort);
				case nameof(Endpoint.TransportTypeTSoIP.SourceIP):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeTsoip.SourceIP), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}

		private static FilterElement<DomInstance> CreateMulticastFilter(Multicast multicast)
		{
			var filters = new List<FilterElement<DomInstance>>
			{
				DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeTsoip.MulticastIP).Equal(multicast.IpAddress),
			};

			if (multicast.Port > 0)
			{
				filters.Add(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeTsoip.SourceIP).Equal(multicast.SourceIP));
			}

			if (multicast.SourceIP != null)
			{
				filters.Add(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.TransportTypeTsoip.Port).Equal(multicast.Port));
			}

			return filters.Count == 1
				? filters[0]
				: new ANDFilterElement<DomInstance>(filters.ToArray());
		}
	}
}
