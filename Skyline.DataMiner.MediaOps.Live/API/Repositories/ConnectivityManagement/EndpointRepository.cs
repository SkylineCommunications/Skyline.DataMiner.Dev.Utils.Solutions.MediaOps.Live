namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class EndpointRepository : Repository<Endpoint>
	{
		internal EndpointRepository(SlcConnectivityManagementHelper helper, IConnection connection) : base(helper, connection)
		{
		}

		protected internal override DomDefinitionId DomDefinition => Endpoint.DomDefinition;

		public Endpoint GetByRoleElementAndIdentifier(EndpointRole role, DmsElementId elementId, string identifier)
		{
			if (String.IsNullOrWhiteSpace(identifier))
			{
				throw new ArgumentException($"'{nameof(identifier)}' cannot be null or whitespace.", nameof(identifier));
			}

			var filter = new ANDFilterElement<DomInstance>(
				DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id),
				DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Role).Equal((int)role),
				DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Element).Equal(elementId.Value),
				DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Identifier).Equal(identifier));

			var endpoints = Read(filter).Take(2).ToList();

			if (endpoints.Count > 1)
			{
				throw new InvalidOperationException($"Multiple endpoints found with role '{role}', element ID '{elementId}' and identifier '{identifier}'.");
			}

			return endpoints.FirstOrDefault();
		}

		public IEnumerable<Endpoint> GetByElement(DmsElementId elementId)
		{
			var filter = new ANDFilterElement<DomInstance>(
				DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id),
				DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Element).Equal(elementId.Value));

			return Read(filter);
		}

		public IEnumerable<Endpoint> GetByElementAndIdentifiers(DmsElementId elementId, IEnumerable<string> identifiers)
		{
			if (identifiers == null)
			{
				throw new ArgumentNullException(nameof(identifiers));
			}

			if (identifiers.Any(x => String.IsNullOrWhiteSpace(x)))
			{
				throw new ArgumentException($"'{nameof(identifiers)}' cannot contain null or whitespace values.", nameof(identifiers));
			}

			FilterElement<DomInstance> CreateFilter(string identifier) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Element).Equal(elementId.Value),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Identifier).Equal(identifier));

			return FilterQueryExecutor.RetrieveFilteredItems(
					identifiers,
					x => CreateFilter(x),
					x => Read(x));
		}

		public IEnumerable<Endpoint> GetByTransportMetadata(params (string fieldName, string value)[] metadataFilters)
		{
			if (metadataFilters is null)
			{
				throw new ArgumentNullException(nameof(metadataFilters));
			}

			if (!metadataFilters.Any())
			{
				return Enumerable.Empty<Endpoint>();
			}

			if (metadataFilters.Any(x => String.IsNullOrWhiteSpace(x.fieldName)))
			{
				throw new ArgumentException($"'{nameof(metadataFilters)}' cannot contain null or whitespace field names.", nameof(metadataFilters));
			}

			var filters = new List<FilterElement<DomInstance>>
			{
				DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Endpoint.Id),
			};

			foreach (var (fieldName, value) in metadataFilters)
			{
				var pairFilter = new ANDFilterElement<DomInstance>(
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointTransportMetadata.FieldName).Equal(fieldName),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointTransportMetadata.Value).Equal(value));

				filters.Add(pairFilter);
			}

			var filter = new ANDFilterElement<DomInstance>(filters.ToArray());
			var endpoints = Read(filter);

			// DOM doesn't support field name/value matching in the same section, so we need to do some post-filtering
			endpoints = endpoints.WithTransportMetadata(metadataFilters);

			return endpoints;
		}

		public IEnumerable<Endpoint> GetByTransportMetadata(string fieldName, string value)
		{
			if (String.IsNullOrWhiteSpace(fieldName))
			{
				throw new ArgumentException($"'{nameof(fieldName)}' cannot be null or whitespace.", nameof(fieldName));
			}

			return GetByTransportMetadata([(fieldName, value)]);
		}

		protected internal override Endpoint CreateInstance(DomInstance domInstance)
		{
			return new Endpoint(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<Endpoint> instances)
		{
			foreach (var instance in instances)
			{
				instance.Validate().ThrowIfInvalid();
			}

			CheckDuplicatesBeforeSave(instances);
		}

		protected override void ValidateBeforeDelete(ICollection<Endpoint> instances)
		{
			CheckIfStillInUse(instances);
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(Endpoint.Name):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Name), comparer, value);
				case nameof(Endpoint.Role):
					return FilterElementFactory.Create<int>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Role), comparer, value);
				case nameof(Endpoint.Element):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Element), comparer, value);
				case nameof(Endpoint.Identifier):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Identifier), comparer, value);
				case nameof(Endpoint.ControlElement):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.ControlElement), comparer, value);
				case nameof(Endpoint.ControlIdentifier):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.ControlIdentifier), comparer, value);
				case nameof(Endpoint.TransportType):
					return FilterElementFactory.Create<Guid>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.TransportType), comparer, value);
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

		private void CheckDuplicatesBeforeSave(ICollection<Endpoint> instances)
		{
			FilterElement<DomInstance> CreateFilter(Endpoint e) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.Id.NotEqual(e.ID),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Name).Equal(e.Name));

			var conflicts = FilterQueryExecutor.RetrieveFilteredItems(instances, CreateFilter, Read).ToList();

			if (conflicts.Count > 0)
			{
				var names = String.Join(", ", conflicts
					.Select(x => x.Name)
					.OrderBy(x => x, new NaturalSortComparer()));

				throw new InvalidOperationException($"Cannot save endpoints. The following names are already in use: {names}");
			}
		}

		private void CheckIfStillInUse(ICollection<Endpoint> instances)
		{
			FilterElement<DomInstance> CreateFilter(Endpoint e) =>
				new ORFilterElement<DomInstance>(
					new ANDFilterElement<DomInstance>(
						DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.VirtualSignalGroup.Id),
						DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevel.Endpoint).Equal(e.ID)));

			var count = FilterQueryExecutor.CountFilteredItems(instances, CreateFilter, Helper.DomInstances.Count);

			if (count > 0)
			{
				throw new InvalidOperationException("One or more endpoints are still in use");
			}
		}
	}
}
