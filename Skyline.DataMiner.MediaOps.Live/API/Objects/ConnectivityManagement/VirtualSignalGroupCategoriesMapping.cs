namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Tools;

	using CategoryRef = Skyline.DataMiner.Solutions.Categories.API.ApiObjectReference<Skyline.DataMiner.Solutions.Categories.API.Category>;

	public class VirtualSignalGroupCategoriesMapping
	{
		private readonly ManyToManyMapping<VirtualSignalGroup, CategoryRef> _mapping = new();

		public VirtualSignalGroupCategoriesMapping()
		{
		}

		public VirtualSignalGroupCategoriesMapping(IEnumerable<VirtualSignalGroup> virtualSignalGroups)
		{
			if (virtualSignalGroups is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			foreach (var vsg in virtualSignalGroups)
			{
				Add(vsg);
			}
		}

		public int VirtualSignalGroupCount => _mapping.Forward.Count;

		public int CategoriesCount => _mapping.Reverse.Count;

		public IReadOnlyCollection<CategoryRef> GetCategories(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			return _mapping.Forward.TryGetValue(virtualSignalGroup, out var categories)
				? categories.ToList()
				: Array.Empty<CategoryRef>();
		}

		public IReadOnlyCollection<VirtualSignalGroup> GetVirtualSignalGroups(CategoryRef category)
		{
			return _mapping.Reverse.TryGetValue(category, out var virtualSignalGroups)
				? virtualSignalGroups.ToList()
				: Array.Empty<VirtualSignalGroup>();
		}

		public void Add(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			foreach (var category in virtualSignalGroup.Categories)
			{
				_mapping.TryAdd(virtualSignalGroup, category);
			}
		}

		public void Remove(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			_mapping.TryRemoveForward(virtualSignalGroup);
		}

		public void AddOrUpdate(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			Remove(virtualSignalGroup);
			Add(virtualSignalGroup);
		}

		public void Clear()
		{
			_mapping.Clear();
		}

		public bool Contains(VirtualSignalGroup virtualSignalGroup)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			return _mapping.Forward.ContainsKey(virtualSignalGroup);
		}

		public bool Contains(CategoryRef category)
		{
			return _mapping.Reverse.ContainsKey(category);
		}

		public bool Contains(VirtualSignalGroup virtualSignalGroup, CategoryRef category)
		{
			if (virtualSignalGroup is null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroup));
			}

			return _mapping.Contains(virtualSignalGroup, category);
		}
	}
}
