namespace Skyline.DataMiner.MediaOps.Live.API.Objects
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class Category : ApiObject<Category>
	{
		private readonly CategoryInstance _domInstance;

		public Category() : this(new CategoryInstance())
		{
		}

		internal Category(CategoryInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal Category(DomInstance domInstance) : this(new CategoryInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcConnectivityManagementIds.Definitions.Category;

		public string Name
		{
			get
			{
				return _domInstance.CategoryInfo.Name;
			}

			set
			{
				_domInstance.CategoryInfo.Name = value;
			}
		}

		public ApiObjectReference<Category>? ParentCategory
		{
			get
			{
				return _domInstance.CategoryInfo.ParentCategory;
			}

			set
			{
				_domInstance.CategoryInfo.ParentCategory = value;
			}
		}

		public void Validate()
		{
			if (String.IsNullOrWhiteSpace(Name))
			{
				throw new InvalidOperationException($"{nameof(Name)} cannot be null, empty, or whitespace.");
			}
		}
	}

	public static class CategoryExposers
	{
		public static readonly Exposer<Category, Guid> ID = new Exposer<Category, Guid>(x => x.ID, nameof(Category.ID));
		public static readonly Exposer<Category, string> Name = new Exposer<Category, string>(x => x.Name, nameof(Category.Name));
		public static readonly Exposer<Category, ApiObjectReference<Category>?> ParentCategory = new Exposer<Category, ApiObjectReference<Category>?>(x => x.ParentCategory, nameof(Category.ParentCategory));
	}
}
