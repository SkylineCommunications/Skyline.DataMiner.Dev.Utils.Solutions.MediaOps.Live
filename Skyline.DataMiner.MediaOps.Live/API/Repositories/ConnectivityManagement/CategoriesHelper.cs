namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Analytics.GenericInterface.JoinFilter;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Utils.Categories.API;
	using Skyline.DataMiner.Utils.Categories.API.Extensions;
	using Skyline.DataMiner.Utils.Categories.API.Objects;

	internal class CategoriesHelper
	{
		private readonly CategoriesApi _api;

		public CategoriesHelper(IConnection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			_api = connection.GetCategoriesApi();
		}

		public void UpdateLinkedCategoryItems(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups == null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			var existingCategoryItems = RetrieveExistingCategoryItems(virtualSignalGroups)
				.SafeToDictionary(ci => (ci.Category, ci.ToIdentifier()));

			var categoryItemsToUpdate = new List<CategoryItem>();

			foreach (var vsg in virtualSignalGroups)
			{
				var categoryItemIdentifier = new CategoryItemIdentifier(
					SlcConnectivityManagementIds.ModuleId,
					Convert.ToString(vsg.ID));

				foreach (var category in vsg.Categories)
				{
					if (existingCategoryItems.TryGetValue((category, categoryItemIdentifier), out var existing))
					{
						// Already exists, keep it
						categoryItemsToUpdate.Add(existing);
						continue;
					}

					// New category item
					categoryItemsToUpdate.Add(categoryItemIdentifier.ToCategoryItem(category));
				}
			}

			var categoryItemsToRemove = existingCategoryItems.Values
				.Except(categoryItemsToUpdate).ToList();

			if (categoryItemsToUpdate.Count > 0)
			{
				_api.CategoryItems.CreateOrUpdate(categoryItemsToUpdate);
			}

			if (categoryItemsToRemove.Count > 0)
			{
				_api.CategoryItems.Delete(categoryItemsToRemove);
			}
		}

		public void RemoveLinkedCategoryItems(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups == null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			var categoryItemsToRemove = RetrieveExistingCategoryItems(virtualSignalGroups).ToList();

			if (categoryItemsToRemove.Count > 0)
			{
				_api.CategoryItems.Delete(categoryItemsToRemove);
			}
		}

		private ICollection<CategoryItem> RetrieveExistingCategoryItems(ICollection<VirtualSignalGroup> virtualSignalGroups)
		{
			static FilterElement<CategoryItem> BuildFilter(VirtualSignalGroup vsg)
			{
				return CategoryItemExposers.ModuleId.Equal(SlcConnectivityManagementIds.ModuleId)
					.AND(CategoryItemExposers.InstanceId.Equal(Convert.ToString(vsg.ID)));
			}

			var categoryItemsToRemove = FilterQueryExecutor.RetrieveFilteredItems(
				virtualSignalGroups,
				vsg => BuildFilter(vsg),
				filter => _api.CategoryItems.Read(filter))
			.ToList();

			return categoryItemsToRemove;
		}
	}
}
