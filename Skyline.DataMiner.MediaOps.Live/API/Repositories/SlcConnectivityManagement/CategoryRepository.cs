namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcConnectivityManagement
{
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class CategoryRepository : Repository<Category>
	{
		public CategoryRepository(SlcConnectivityManagementHelper helper) : base(helper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => Category.DomDefinition;

		protected override Category CreateInstance(DomInstance domInstance)
		{
			return new Category(domInstance);
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(Category.Name):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.CategoryInfo.Name), comparer, (string)value);
				case nameof(Category.ParentCategory):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.CategoryInfo.ParentCategory), comparer, ApiObjectReference<Category>.Convert(value));
			}

			return base.CreateFilter(fieldName, comparer, value);
		}

		protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(Category.Name):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.CategoryInfo.Name), sortOrder, naturalSort);
				case nameof(Category.ParentCategory):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.CategoryInfo.ParentCategory), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}
	}
}
