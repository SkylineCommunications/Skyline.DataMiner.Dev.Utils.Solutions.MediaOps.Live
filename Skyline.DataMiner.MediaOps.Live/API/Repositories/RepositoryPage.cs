namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;

	using SDM = Skyline.DataMiner.SDM;

	public class RepositoryPage<T> : SDM.IPagedResult<T>
		where T : ApiObject<T>
	{
		private readonly IList<T> _items;

		public RepositoryPage(IEnumerable<T> items, int pageNumber, bool hasNextPage)
		{
			if (items is null)
			{
				throw new ArgumentNullException(nameof(items));
			}

			_items = items is IList<T> itemList ? itemList : items.ToList();

			PageNumber = pageNumber;
			HasNextPage = hasNextPage;
		}

		/// <inheritdoc/>
		public int PageNumber { get; }

		/// <inheritdoc/>
		public bool HasNextPage { get; }

		/// <inheritdoc/>
		public T this[int index] => _items[index];

		/// <inheritdoc/>
		public int Count => _items.Count;

		/// <inheritdoc/>
		public IEnumerator<T> GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
