namespace Skyline.DataMiner.Solutions.MediaOps.Live.Extensions
{
	using System;
	using System.Collections.Generic;

	internal static class ICollectionExtensions
	{
		public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> newList)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (newList == null)
			{
				throw new ArgumentNullException(nameof(newList));
			}

			if (source is List<T> concreteList)
			{
				concreteList.AddRange(newList);
				return;
			}

			foreach (var element in newList)
			{
				source.Add(element);
			}
		}
	}
}
