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
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	using Categories = Skyline.DataMiner.Utils.Categories.API.Objects;

	public class VirtualSignalGroupRepository : Repository<VirtualSignalGroup>
	{
		private readonly CategoriesHelper _categoriesHelper;

		internal VirtualSignalGroupRepository(SlcConnectivityManagementHelper helper, IConnection connection) : base(helper, connection)
		{
			_categoriesHelper = new CategoriesHelper(connection);
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

		public IEnumerable<VirtualSignalGroup> GetByCategory(Categories.ApiObjectReference<Categories.Category> category)
		{
			if (category == Categories.ApiObjectReference<Categories.Category>.Empty)
			{
				return [];
			}

			var filter = VirtualSignalGroupExposers.Categories.Contains(category.ID);
			return Read(filter);
		}

		public override VirtualSignalGroup Create(VirtualSignalGroup instance)
		{
			if (instance is null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			// Create the instance first
			var newInstance = base.Create(instance);

			// Then update linked category items
			_categoriesHelper.UpdateLinkedCategoryItems([newInstance]);

			return newInstance;
		}

		public override VirtualSignalGroup Update(VirtualSignalGroup instance)
		{
			if (instance is null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			// Update the instance first
			var newInstance = base.Update(instance);

			// Then update linked category items
			_categoriesHelper.UpdateLinkedCategoryItems([newInstance]);

			return newInstance;
		}

		public override IEnumerable<VirtualSignalGroup> CreateOrUpdate(IEnumerable<VirtualSignalGroup> instances)
		{
			if (instances is null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			var instancesCollection = instances.AsCollection();

			// First create or update the instances
			var result = base.CreateOrUpdate(instancesCollection);

			// Then update linked category items
			_categoriesHelper.UpdateLinkedCategoryItems(instancesCollection);

			return result;
		}

		public override void Delete(IEnumerable<VirtualSignalGroup> instances)
		{
			if (instances is null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			var instancesCollection = instances.AsCollection();

			// First remove linked category items
			_categoriesHelper.RemoveLinkedCategoryItems(instancesCollection);

			// Proceed with deletion
			base.Delete(instancesCollection);
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
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Name), comparer, value);
				case nameof(VirtualSignalGroup.Description):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Description), comparer, value);
				case nameof(VirtualSignalGroup.Role):
					return FilterElementFactory.Create<int>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Role), comparer, value);
				case nameof(LevelEndpoint.Level):
					return FilterElementFactory.Create<Guid>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevel.Level), comparer, value);
				case nameof(LevelEndpoint.Endpoint):
					return FilterElementFactory.Create<Guid>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevel.Endpoint), comparer, value);
				case nameof(VirtualSignalGroup.Categories):
					return FilterElementFactory.Create<Guid>(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Categories), comparer, value);
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
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevel.Level), sortOrder, naturalSort);
				case nameof(LevelEndpoint.Endpoint):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevel.Endpoint), sortOrder, naturalSort);
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

			var conflicts = FilterQueryExecutor.RetrieveFilteredItems(instances, CreateFilter, Read).ToList();

			if (conflicts.Count > 0)
			{
				var names = String.Join(", ", conflicts
					.Select(x => x.Name)
					.OrderBy(x => x, new NaturalSortComparer()));

				throw new InvalidOperationException($"Cannot save VSGs. The following names are already in use: {names}");
			}
		}
	}
}
