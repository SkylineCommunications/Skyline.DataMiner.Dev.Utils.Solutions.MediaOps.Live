namespace Skyline.DataMiner.MediaOps.Live.API.Repositories
{
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class VirtualSignalGroupRepository : Repository<VirtualSignalGroup>
	{
		public VirtualSignalGroupRepository(SlcConnectivityManagementHelper helper) : base(helper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => VirtualSignalGroup.DomDefinition;

		protected override VirtualSignalGroup CreateInstance(DomInstance domInstance)
		{
			return new VirtualSignalGroup(domInstance);
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
	}
}
