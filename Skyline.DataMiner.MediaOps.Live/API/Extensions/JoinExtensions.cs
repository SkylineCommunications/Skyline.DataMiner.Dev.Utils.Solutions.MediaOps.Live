namespace Skyline.DataMiner.MediaOps.Live.API.Extensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Net.Helper;

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
		public static IEnumerable<IEnumerable<TResult>> Join<TLeft, TRight, TResult>(
			this IEnumerable<IEnumerable<TLeft>> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, IEnumerable<ApiObjectReference<TRight>>> rightIdsSelector,
			Func<TLeft, IEnumerable<TRight>, TResult> resultSelector)
			where TLeft : ApiObject<TLeft>
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

			return JoinIterator(leftSource, rightRepository, rightIdsSelector, resultSelector);
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
		public static IEnumerable<IEnumerable<TResult>> Join<TLeft, TRight, TResult>(
			this IEnumerable<IEnumerable<TLeft>> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, ApiObjectReference<TRight>?> rightIdSelector,
			Func<TLeft, TRight, TResult> resultSelector)
			where TLeft : ApiObject<TLeft>
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

			return JoinIterator(leftSource, rightRepository, rightIdSelector, resultSelector);
		}

		/// <summary>
		/// Joins a collection of items from the left source with their related items in the repository in batches using multiple references.
		/// </summary>
		/// <typeparam name="TLeft">The type of the elements in the left sequence.</typeparam>
		/// <typeparam name="TRight">The type of the elements in the right repository.</typeparam>
		/// <typeparam name="TResult">The type of the result element.</typeparam>
		/// <param name="leftSource">A sequence of left elements.</param>
		/// <param name="rightRepository">The repository to retrieve right elements from.</param>
		/// <param name="rightIdsSelector">Function to select one or more references from each left element.</param>
		/// <param name="resultSelector">Function to project the result from a left element and its matched right elements.</param>
		/// <param name="batchSize">The batch size for pagination. Default is 250.</param>
		/// <returns>A sequence of joined results.</returns>
		/// <exception cref="ArgumentNullException">Thrown if any of the input parameters is null.</exception>
		public static IEnumerable<TResult> JoinInBatches<TLeft, TRight, TResult>(
			this IEnumerable<TLeft> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, IEnumerable<ApiObjectReference<TRight>>> rightIdsSelector,
			Func<TLeft, IEnumerable<TRight>, TResult> resultSelector,
			int batchSize = 250)
			where TLeft : ApiObject<TLeft>
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

			return leftSource
				.Batch(batchSize)
				.Join(rightRepository, rightIdsSelector, resultSelector)
				.Flatten();
		}

		/// <summary>
		/// Joins a collection of items from the left source with their related items in the repository in batches using a single optional reference.
		/// </summary>
		/// <typeparam name="TLeft">The type of the elements in the left sequence.</typeparam>
		/// <typeparam name="TRight">The type of the elements in the right repository.</typeparam>
		/// <typeparam name="TResult">The type of the result element.</typeparam>
		/// <param name="leftSource">A sequence of left elements.</param>
		/// <param name="rightRepository">The repository to retrieve right elements from.</param>
		/// <param name="rightIdSelector">Function to select a reference from each left element.</param>
		/// <param name="resultSelector">Function to project the result from a left element and its matched right element.</param>
		/// <param name="batchSize">The batch size for pagination. Default is 250.</param>
		/// <returns>A sequence of joined results.</returns>
		/// <exception cref="ArgumentNullException">Thrown if any of the input parameters is null.</exception>
		public static IEnumerable<TResult> JoinInBatches<TLeft, TRight, TResult>(
			this IEnumerable<TLeft> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, ApiObjectReference<TRight>?> rightIdSelector,
			Func<TLeft, TRight, TResult> resultSelector,
			int batchSize = 250)
			where TLeft : ApiObject<TLeft>
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

			return leftSource
				.Batch(batchSize)
				.Join(rightRepository, rightIdSelector, resultSelector)
				.Flatten();
		}

		private static IEnumerable<IEnumerable<TResult>> JoinIterator<TLeft, TRight, TResult>(
			IEnumerable<IEnumerable<TLeft>> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, IEnumerable<ApiObjectReference<TRight>>> rightIdsSelector,
			Func<TLeft, IEnumerable<TRight>, TResult> resultSelector)
			where TLeft : ApiObject<TLeft>
			where TRight : ApiObject<TRight>
		{
			var cache = new Dictionary<Guid, TRight>();

			foreach (var page in leftSource)
			{
				var pageCollection = page is ICollection<TLeft> collection ? collection : page.ToList();

				var idsToRetrieve = pageCollection
					.SelectMany(rightIdsSelector)
					.Select(x => x.ID)
					.Where(id => id != Guid.Empty && !cache.ContainsKey(id))
					.Distinct()
					.ToList();

				if (idsToRetrieve.Count > 0)
				{
					var retrieved = rightRepository.Read(idsToRetrieve);

					foreach (var id in idsToRetrieve)
					{
						cache[id] = retrieved.TryGetValue(id, out var b) ? b : null;
					}
				}

				var result = new List<TResult>();

				foreach (var left in pageCollection)
				{
					var rightIds = rightIdsSelector(left)
						.Where(x => x != null && x.ID != Guid.Empty)
						.Select(x => x.ID);

					var rights = rightIds.Select(id => cache.TryGetValue(id, out var r) ? r : null)
						.Where(x => x != null)
						.ToList();

					result.Add(resultSelector(left, rights));
				}

				yield return result;
			}
		}

		private static IEnumerable<IEnumerable<TResult>> JoinIterator<TLeft, TRight, TResult>(
			IEnumerable<IEnumerable<TLeft>> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, ApiObjectReference<TRight>?> rightIdSelector,
			Func<TLeft, TRight, TResult> resultSelector)
			where TLeft : ApiObject<TLeft>
			where TRight : ApiObject<TRight>
		{
			var cache = new Dictionary<Guid, TRight>();

			foreach (var page in leftSource)
			{
				var pageCollection = page is ICollection<TLeft> collection ? collection : page.ToList();

				var idsToRetrieve = pageCollection
					.Select(rightIdSelector)
					.Where(x => x != null && x.Value.ID != Guid.Empty)
					.Select(x => x.Value.ID)
					.Where(id => !cache.ContainsKey(id))
					.Distinct()
					.ToList();

				if (idsToRetrieve.Count > 0)
				{
					var retrieved = rightRepository.Read(idsToRetrieve);

					foreach (var id in idsToRetrieve)
					{
						cache[id] = retrieved.TryGetValue(id, out var b) ? b : null;
					}
				}

				var result = new List<TResult>();

				foreach (var left in pageCollection)
				{
					var refObj = rightIdSelector(left);
					var rightId = refObj?.ID ?? Guid.Empty;

					cache.TryGetValue(rightId, out var right);

					result.Add(resultSelector(left, right));
				}

				yield return result;
			}
		}
	}
}
