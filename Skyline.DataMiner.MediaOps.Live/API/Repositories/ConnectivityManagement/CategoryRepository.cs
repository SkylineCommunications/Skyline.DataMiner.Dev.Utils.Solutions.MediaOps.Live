namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	public class CategoryRepository : Repository<Category>
	{
		internal CategoryRepository(SlcConnectivityManagementHelper helper, IConnection connection) : base(helper, connection)
		{
		}

		protected internal override DomDefinitionId DomDefinition => Category.DomDefinition;

		protected internal override Category CreateInstance(DomInstance domInstance)
		{
			return new Category(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<Category> instances)
		{
			foreach (var instance in instances)
			{
				instance.Validate().ThrowIfInvalid();
			}

			CheckDuplicatesBeforeSave(instances);
		}

		protected override void ValidateBeforeDelete(ICollection<Category> instances)
		{
			CheckIfStillInUse(instances);
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(Category.Name):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.CategoryInfo.Name), comparer, value);
				case nameof(Category.ParentCategory):
					return FilterElementFactory.Create<Guid>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.CategoryInfo.ParentCategory), comparer, value);
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

		private void CheckDuplicatesBeforeSave(ICollection<Category> instances)
		{
			FilterElement<DomInstance> CreateFilter(Category c) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.Id.NotEqual(c.ID),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.CategoryInfo.Name).Equal(c.Name));

			var count = FilterQueryExecutor.CountFilteredItems(
				instances,
				x => CreateFilter(x),
				x => Count(x));

			if (count > 0)
			{
				throw new InvalidOperationException($"Category with same name already exists.");
			}
		}

		private void CheckIfStillInUse(ICollection<Category> instances)
		{
			FilterElement<DomInstance> CreateFilter(Category c) =>
				new ANDFilterElement<DomInstance>(
					DomInstanceExposers.DomDefinitionId.Equal(SlcConnectivityManagementIds.Definitions.VirtualSignalGroup.Id),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Categories).Equal(c.ID));

			var count = FilterQueryExecutor.CountFilteredItems(
				instances,
				x => CreateFilter(x),
				x => Helper.DomInstances.Count(x));

			if (count > 0)
			{
				var message = instances.Count == 1
					? $"Cannot delete category '{instances.First().Name}' because it is still in use."
					: "Cannot delete one or more categories because they are still in use.";

				throw new InvalidOperationException(message);
			}
		}
	}
}
