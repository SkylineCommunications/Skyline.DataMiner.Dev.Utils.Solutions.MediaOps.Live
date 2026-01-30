namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tools
{
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Helper;

	internal static class CollectionEqualityHelper
	{
		public static bool Equals<T>(IEnumerable<T> items1, IEnumerable<T> items2, bool ignoreOrder = false)
		{
			if (ReferenceEquals(items1, items2))
			{
				return true;
			}

			if (items1 is null || items2 is null)
			{
				return false;
			}

			if (ignoreOrder)
			{
				return items1.ScrambledEquals(items2);
			}
			else
			{
				return items1.SequenceEqual(items2);
			}
		}

		public static int GetHashCode<T>(IEnumerable<T> items, bool ignoreOrder = false)
		{
			if (items is null)
			{
				return 0;
			}

			unchecked
			{
				int hash = 17;

				if (ignoreOrder)
				{
					// Order-independent hash: sum of individual hashes
					foreach (var item in items)
					{
						hash += EqualityComparer<T>.Default.GetHashCode(item);
					}
				}
				else
				{
					// Order-sensitive hash: rolling hash
					foreach (var item in items)
					{
						hash = (hash * 31) + EqualityComparer<T>.Default.GetHashCode(item);
					}
				}

				return hash;
			}
		}
	}
}
