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
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class VirtualSignalGroupRepository : Repository<VirtualSignalGroup>
	{
		internal VirtualSignalGroupRepository(SlcConnectivityManagementHelper helper, IConnection connection) : base(helper, connection)
		{
		}

		protected internal override DomDefinitionId DomDefinition => VirtualSignalGroup.DomDefinition;

		public IEnumerable<VirtualSignalGroup> GetByEndpointIds(IEnumerable<Guid> endpointIds)
		{
			if (endpointIds == null)
			{
				throw new ArgumentNullException(nameof(endpointIds));
			}

			var vsgs = FilterQueryExecutor.RetrieveFilteredItems(
				endpointIds,
				x => VirtualSignalGroupExposers.Endpoint.Equal(x),
				x => Read(x));

			return vsgs;
		}

		public IEnumerable<VirtualSignalGroup> GetByEndpoints(IEnumerable<Endpoint> endpoints)
		{
			if (endpoints == null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			return GetByEndpointIds(endpoints.Select(x => x.ID));
		}

		protected internal override VirtualSignalGroup CreateInstance(DomInstance domInstance)
		{
			return new VirtualSignalGroup(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<VirtualSignalGroup> instances)
		{
			foreach (var instance in instances)
			{
				instance.Validate().ThrowIfInvalid();
			}

			CheckDuplicatesBeforeSave(instances);
		}

		protected override void ValidateBeforeDelete(ICollection<VirtualSignalGroup> instances)
		{
			// no checks needed
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(VirtualSignalGroup.Name):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Name), comparer, (string)value);
				case nameof(VirtualSignalGroup.Description):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Description), comparer, (string)value);
				case nameof(VirtualSignalGroup.Role):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Role), comparer, (int)value);
				case nameof(LevelEndpoint.Level):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevels.Level), comparer, ApiObjectReference<Level>.Convert(value));
				case nameof(LevelEndpoint.Endpoint):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevels.Endpoint), comparer, ApiObjectReference<Endpoint>.Convert(value));
				case nameof(VirtualSignalGroup.Categories):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Categories), comparer, ApiObjectReference<Category>.Convert(value));
			}

			return base.CreateFilter(fieldName, comparer, value);
		}

		protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(VirtualSignalGroup.Name):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Name), sortOrder, naturalSort);
				case nameof(VirtualSignalGroup.Description):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Description), sortOrder, naturalSort);
				case nameof(VirtualSignalGroup.Role):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Role), sortOrder, naturalSort);
				case nameof(LevelEndpoint.Level):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevels.Level), sortOrder, naturalSort);
				case nameof(LevelEndpoint.Endpoint):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevels.Endpoint), sortOrder, naturalSort);
				case nameof(VirtualSignalGroup.Categories):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Categories), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}

		private void CheckDuplicatesBeforeSave(ICollection<VirtualSignalGroup> instances)
		{
			FilterElement<DomInstance> CreateFilter(VirtualSignalGroup vsg) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.Id.NotEqual(vsg.ID),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Name).Equal(vsg.Name));

			var count = FilterQueryExecutor.CountFilteredItems(
				instances,
				x => CreateFilter(x),
				x => Count(x));

			if (count > 0)
			{
				throw new InvalidOperationException($"Virtual signal group with same name already exists.");
			}
		}
	}
}
