namespace Skyline.DataMiner.MediaOps.Live.API.Repositories
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Data;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using SLDataGateway.API.Types.Querying;

	public class EndpointRepository : Repository<Endpoint>
	{
		internal EndpointRepository(SlcConnectivityManagementHelper helper) : base(helper)
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

		public override void Delete(Endpoint instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			CleanupLinkedConnections(new[] { instance });

			base.Delete(instance);
		}

		public override void Delete(IEnumerable<Endpoint> instances)
		{
			if (instances == null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			CleanupLinkedConnections(instances);

			base.Delete(instances);
		}

		protected override Endpoint CreateInstance(DomInstance domInstance)
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

		private void CheckDuplicatesBeforeSave(ICollection<Endpoint> instances)
		{
			FilterElement<DomInstance> CreateFilter(Endpoint e) =>
				DomInstanceExposers.Id.NotEqual(e.ID)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.EndpointInfo.Name).Equal(e.Name));

			var count = FilterQueryExecutor.CountFilteredItems(
				instances,
				x => CreateFilter(x),
				x => Helper.DomInstances.Count(x));

			if (count > 0)
			{
				throw new InvalidOperationException($"Endpoint with same name already exists.");
			}
		}

		private void CheckIfStillInUse(ICollection<Endpoint> instances)
		{
			FilterElement<DomInstance> CreateFilter(Endpoint e) =>
				new ORFilterElement<DomInstance>(
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevels.Endpoint).Equal(e.ID),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.Destination).Equal(e.ID),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.ConnectedSource).Equal(e.ID),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.PendingConnectedSource).Equal(e.ID));

			var count = FilterQueryExecutor.CountFilteredItems(
				instances,
				x => CreateFilter(x),
				x => Helper.DomInstances.Count(x));

			if (count > 0)
			{
				var message = instances.Count == 1
					? $"Cannot delete endpoint '{instances.First().Name}' because it is still in use."
					: "Cannot delete one or more endpoints because they are still in use.";

				throw new InvalidOperationException(message);
			}
		}

		private void CleanupLinkedConnections(IEnumerable<Endpoint> instances)
		{
			FilterElement<DomInstance> CreateFilter(Endpoint e) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.Connection.Id),
					new ORFilterElement<DomInstance>(
						DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.Destination).Equal(e.ID),
						DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.ConnectionInfo.ConnectedSource).Equal(e.ID)));

			var connectionInstances = FilterQueryExecutor.RetrieveFilteredItems(
					instances,
					x => CreateFilter(x),
					x => Helper.DomInstances.Read(x))
				.ToList();

			if (connectionInstances.Count > 0)
			{
				Helper.DomInstances.DeleteInBatches(connectionInstances);
			}
		}
	}
}
