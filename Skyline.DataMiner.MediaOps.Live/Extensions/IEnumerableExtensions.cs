namespace Skyline.DataMiner.MediaOps.Live.Extensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Extension methods for IEnumerable collections.
	/// </summary>
	public static class IEnumerableExtensions
	{
		/// <summary>
		/// Safely converts an enumerable collection to a dictionary, avoiding exceptions on duplicate keys by using the last occurrence.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
		/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
		/// <typeparam name="TElement">The type of the values in the dictionary.</typeparam>
		/// <param name="source">The source collection.</param>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="elementSelector">A function to transform each element into a dictionary value.</param>
		/// <param name="comparer">An optional equality comparer to compare keys.</param>
		/// <returns>A dictionary containing the transformed elements.</returns>
		public static Dictionary<TKey, TElement> SafeToDictionary<TSource, TKey, TElement>(
			 this IEnumerable<TSource> source,
			 Func<TSource, TKey> keySelector,
			 Func<TSource, TElement> elementSelector,
			 IEqualityComparer<TKey> comparer = null)
		{
			var dictionary = new Dictionary<TKey, TElement>(comparer);

			if (source == null)
			{
				return dictionary;
			}

			foreach (TSource element in source)
			{
				dictionary[keySelector(element)] = elementSelector(element);
			}

			return dictionary;
		}

		/// <summary>
		/// Safely converts an enumerable collection to a dictionary, avoiding exceptions on duplicate keys by using the last occurrence.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
		/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
		/// <param name="source">The source collection.</param>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="comparer">An optional equality comparer to compare keys.</param>
		/// <returns>A dictionary containing the source elements.</returns>
		public static Dictionary<TKey, TSource> SafeToDictionary<TSource, TKey>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			IEqualityComparer<TKey> comparer = null)
		{
			return source.SafeToDictionary(keySelector, x => x, comparer);
		}

		/// <summary>
		/// Flattens a nested enumerable collection into a single-level enumerable.
		/// </summary>
		/// <typeparam name="T">The type of elements in the collections.</typeparam>
		/// <param name="source">The nested enumerable collection to flatten.</param>
		/// <returns>A flattened enumerable containing all elements.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
		public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			return source.SelectMany(x => x);
		}
	}
}
