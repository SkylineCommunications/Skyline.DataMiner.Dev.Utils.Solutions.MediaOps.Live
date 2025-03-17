namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	public class FetchingItemsCache<TKey, TItem>
	{
		private readonly ConcurrentDictionary<TKey, TItem> items = new ConcurrentDictionary<TKey, TItem>();

		private readonly Func<IEnumerable<TKey>, IEnumerable<TItem>> getItems;
		private readonly Func<TItem, TKey> getKey;

		public FetchingItemsCache(Func<TItem, TKey> getKey, Func<IEnumerable<TKey>, IEnumerable<TItem>> getItems)
		{
			this.getItems = getItems;
			this.getKey = getKey;
		}

		public int Count => items.Count;

		public TItem Get(TKey key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (!items.TryGetValue(key, out var item))
			{
				item = getItems(new[] { key }).FirstOrDefault();
				items[key] = item;
			}

			return item;
		}

		public IDictionary<TKey, TItem> Get(IEnumerable<TKey> keys)
		{
			if (keys == null)
			{
				throw new ArgumentNullException(nameof(keys));
			}

			var result = new Dictionary<TKey, TItem>();
			var keysToRetrieve = new List<TKey>();

			foreach (var key in keys.Distinct())
			{
				if (items.TryGetValue(key, out var instance))
					result[key] = instance;
				else
					keysToRetrieve.Add(key);
			}

			if (keysToRetrieve.Count > 0)
			{
				// retrieve the remaining items
				var newItems = getItems(keysToRetrieve);

				foreach (var item in newItems)
				{
					var key = getKey(item);

					result[key] = item;
					items[key] = item;
				}
			}

			return result;
		}

		public void Store(TItem item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			var key = getKey(item);

			if (key != null)
			{
				items[key] = item;
			}
		}

		public void Store(IEnumerable<TItem> items)
		{
			if (items == null)
			{
				throw new ArgumentNullException(nameof(items));
			}

			foreach (var item in items)
			{
				Store(item);
			}
		}

		public void Clear()
		{
			items.Clear();
		}
	}
}
