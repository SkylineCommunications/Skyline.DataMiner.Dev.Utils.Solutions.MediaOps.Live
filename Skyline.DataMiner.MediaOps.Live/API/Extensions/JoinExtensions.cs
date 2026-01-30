namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Extensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories;

	public static class JoinExtensions
	{
		/// <summary>
		/// Joins each batch of items from the left source with their related items in the repository using a collection of references.
		/// </summary>
		/// <typeparam name="TLeft">The type of the elements in the left sequence.</typeparam>
		/// <typeparam name="TRight">The type of the elements in the right repository.</typeparam>
		/// <typeparam name="TResult">The type of the result element.</typeparam>
		/// <param name="leftSource">A sequence of pages containing the left elements.</param>
		/// <param name="rightRepository">The repository to retrieve right elements from.</param>
		/// <param name="rightIdsSelector">Function to select one or more references from each left element.</param>
		/// <param name="resultSelector">Function to project the result from a left element and its matched right elements.</param>
		/// <returns>A sequence of result pages.</returns>
		/// <exception cref="ArgumentNullException">Thrown if any of the input parameters is null.</exception>
		public static IEnumerable<IEnumerable<TResult>> JoinInBatches<TLeft, TRight, TResult>(
			this IEnumerable<IEnumerable<TLeft>> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, IEnumerable<ApiObjectReference<TRight>>> rightIdsSelector,
			Func<TLeft, IEnumerable<TRight>, TResult> resultSelector)
			where TRight : ApiObject<TRight>
		{
			if (leftSource == null)
			{
				throw new ArgumentNullException(nameof(leftSource));
			}

			if (rightRepository == null)
			{
				throw new ArgumentNullException(nameof(rightRepository));
			}

			if (rightIdsSelector == null)
			{
				throw new ArgumentNullException(nameof(rightIdsSelector));
			}

			if (resultSelector == null)
			{
				throw new ArgumentNullException(nameof(resultSelector));
			}

			IEnumerable<Guid> RightKeysSelector(TLeft left) =>
				rightIdsSelector(left)
				.Where(x => x != null)
				.Select(x => x.ID);

			return JoinIterator(
				leftSource,
				RightKeysSelector,
				ids => rightRepository.Read(ids),
				resultSelector);
		}

		/// <summary>
		/// Joins each batch of items from the left source with their related items in the repository using a single optional reference.
		/// </summary>
		/// <typeparam name="TLeft">The type of the elements in the left sequence.</typeparam>
		/// <typeparam name="TRight">The type of the elements in the right repository.</typeparam>
		/// <typeparam name="TResult">The type of the result element.</typeparam>
		/// <param name="leftSource">A sequence of pages containing the left elements.</param>
		/// <param name="rightRepository">The repository to retrieve right elements from.</param>
		/// <param name="rightIdSelector">Function to select a reference from each left element.</param>
		/// <param name="resultSelector">Function to project the result from a left element and its matched right element.</param>
		/// <returns>A sequence of result pages.</returns>
		/// <exception cref="ArgumentNullException">Thrown if any of the input parameters is null.</exception>
		public static IEnumerable<IEnumerable<TResult>> JoinInBatches<TLeft, TRight, TResult>(
			this IEnumerable<IEnumerable<TLeft>> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, ApiObjectReference<TRight>?> rightIdSelector,
			Func<TLeft, TRight, TResult> resultSelector)
			where TRight : ApiObject<TRight>
		{
			if (leftSource == null)
			{
				throw new ArgumentNullException(nameof(leftSource));
			}

			if (rightRepository == null)
			{
				throw new ArgumentNullException(nameof(rightRepository));
			}

			if (rightIdSelector == null)
			{
				throw new ArgumentNullException(nameof(rightIdSelector));
			}

			if (resultSelector == null)
			{
				throw new ArgumentNullException(nameof(resultSelector));
			}

			IEnumerable<Guid> RightKeysSelector(TLeft left) =>
				new[] { rightIdSelector(left) }
				.Where(x => x != null)
				.Select(x => x.Value.ID);

			return JoinIterator(
				leftSource,
				RightKeysSelector,
				ids => rightRepository.Read(ids),
				(left, rights) => resultSelector(left, rights.FirstOrDefault()));
		}

		/// <summary>
		/// Joins each batch of items from the left source with related items retrieved by key using a custom selector and batched retrieval.
		/// </summary>
		/// <typeparam name="TLeft">The type of the elements in the left sequence.</typeparam>
		/// <typeparam name="TRight">The type of the elements in the right sequence.</typeparam>
		/// <typeparam name="TKey">The type of the key used to join left and right elements.</typeparam>
		/// <typeparam name="TResult">The type of the result element.</typeparam>
		/// <param name="leftSource">A sequence of pages containing the left elements.</param>
		/// <param name="rightKeysSelector">Function to select one or more keys from each left element.</param>
		/// <param name="retrieveRightItems">Function that retrieves right elements by a batch of keys.</param>
		/// <param name="resultSelector">Function to project the result from a left element and its matched right elements.</param>
		/// <returns>A sequence of result pages, where each page contains joined elements.</returns>
		/// <exception cref="ArgumentNullException">Thrown if any of the input parameters is null.</exception>
		public static IEnumerable<IEnumerable<TResult>> JoinInBatches<TLeft, TRight, TKey, TResult>(
			this IEnumerable<IEnumerable<TLeft>> leftSource,
			Func<TLeft, IEnumerable<TKey>> rightKeysSelector,
			Func<IEnumerable<TKey>, IDictionary<TKey, TRight>> retrieveRightItems,
			Func<TLeft, IEnumerable<TRight>, TResult> resultSelector)
			where TRight : ApiObject<TRight>
		{
			if (leftSource == null)
			{
				throw new ArgumentNullException(nameof(leftSource));
			}

			if (rightKeysSelector == null)
			{
				throw new ArgumentNullException(nameof(rightKeysSelector));
			}

			if (retrieveRightItems == null)
			{
				throw new ArgumentNullException(nameof(retrieveRightItems));
			}

			if (resultSelector == null)
			{
				throw new ArgumentNullException(nameof(resultSelector));
			}

			return JoinIterator(
				leftSource,
				rightKeysSelector,
				retrieveRightItems,
				resultSelector);
		}

		/// <summary>
		/// Joins each batch of items from the left source with related items retrieved by key using a single key selector and batched retrieval.
		/// </summary>
		/// <typeparam name="TLeft">The type of the elements in the left sequence.</typeparam>
		/// <typeparam name="TRight">The type of the elements in the right sequence.</typeparam>
		/// <typeparam name="TKey">The type of the key used to join left and right elements.</typeparam>
		/// <typeparam name="TResult">The type of the result element.</typeparam>
		/// <param name="leftSource">A sequence of pages containing the left elements.</param>
		/// <param name="rightKeySelector">Function to select a single key from each left element.</param>
		/// <param name="retrieveRightItems">Function that retrieves right elements by a batch of keys.</param>
		/// <param name="resultSelector">Function to project the result from a left element and its matched right element.</param>
		/// <returns>A sequence of result pages, where each page contains joined elements.</returns>
		/// <exception cref="ArgumentNullException">Thrown if any of the input parameters is null.</exception>
		public static IEnumerable<IEnumerable<TResult>> JoinInBatches<TLeft, TRight, TKey, TResult>(
			this IEnumerable<IEnumerable<TLeft>> leftSource,
			Func<TLeft, TKey> rightKeySelector,
			Func<IEnumerable<TKey>, IDictionary<TKey, TRight>> retrieveRightItems,
			Func<TLeft, TRight, TResult> resultSelector)
			where TRight : ApiObject<TRight>
		{
			if (leftSource == null)
			{
				throw new ArgumentNullException(nameof(leftSource));
			}

			if (rightKeySelector == null)
			{
				throw new ArgumentNullException(nameof(rightKeySelector));
			}

			if (retrieveRightItems == null)
			{
				throw new ArgumentNullException(nameof(retrieveRightItems));
			}

			if (resultSelector == null)
			{
				throw new ArgumentNullException(nameof(resultSelector));
			}

			return JoinIterator(
				leftSource,
				left => new[] { rightKeySelector(left) },
				retrieveRightItems,
				(left, rights) => resultSelector(left, rights.FirstOrDefault()));
		}

		private static IEnumerable<IEnumerable<TResult>> JoinIterator<TLeft, TRight, TKey, TResult>(
			IEnumerable<IEnumerable<TLeft>> leftSource,
			Func<TLeft, IEnumerable<TKey>> rightKeysSelector,
			Func<IEnumerable<TKey>, IDictionary<TKey, TRight>> retrieveRightItems,
			Func<TLeft, ICollection<TRight>, TResult> resultSelector)
			where TRight : ApiObject<TRight>
		{
			var cache = new Dictionary<TKey, TRight>();

			foreach (var page in leftSource)
			{
				var pageCollection = page is ICollection<TLeft> collection ? collection : page.ToList();

				var keysToRetrieve = pageCollection
					.SelectMany(rightKeysSelector)
					.Where(key => !Equals(key, default) && !cache.ContainsKey(key))
					.Distinct()
					.ToList();

				if (keysToRetrieve.Count > 0)
				{
					var retrieved = retrieveRightItems(keysToRetrieve);

					foreach (var id in keysToRetrieve)
					{
						cache[id] = retrieved.TryGetValue(id, out var b) ? b : null;
					}
				}

				var result = new List<TResult>();

				foreach (var left in pageCollection)
				{
					var rightKeys = rightKeysSelector(left)
						.Where(key => !Equals(key, default));

					var rights = rightKeys
						.Select(id => cache.TryGetValue(id, out var r) ? r : null)
						.Where(x => x != null)
						.ToList();

					result.Add(resultSelector(left, rights));
				}

				yield return result;
			}
		}
	}
}
