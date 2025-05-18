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
		public static IEnumerable<IEnumerable<TResult>> Join<TLeft, TRight, TResult>(
			this IEnumerable<IEnumerable<TLeft>> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, IEnumerable<ApiObjectReference<TRight>?>> rightIdsSelector,
			Func<TLeft, IEnumerable<TRight>, TResult> resultSelector)
			where TLeft : ApiObject<TLeft>
			where TRight : ApiObject<TRight>
		{
			var cache = new Dictionary<Guid, TRight>();

			foreach (var page in leftSource)
			{
				var idsToRetrieve = page
					.SelectMany(rightIdsSelector)
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

				foreach (var left in page)
				{
					var rightIds = rightIdsSelector(left)
						?.Where(x => x != null && x.Value.ID != Guid.Empty)
						.Select(x => x.Value.ID)
						.ToList() ?? new List<Guid>();

					var rights = rightIds.Select(id => cache.TryGetValue(id, out var r) ? r : null)
						.Where(x => x != null)
						.ToList();

					result.Add(resultSelector(left, rights));
				}

				yield return result;
			}
		}

		public static IEnumerable<IEnumerable<TResult>> Join<TLeft, TRight, TResult>(
			this IEnumerable<IEnumerable<TLeft>> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, ApiObjectReference<TRight>?> rightIdSelector,
			Func<TLeft, TRight, TResult> resultSelector)
			where TLeft : ApiObject<TLeft>
			where TRight : ApiObject<TRight>
		{
			var cache = new Dictionary<Guid, TRight>();

			foreach (var page in leftSource)
			{
				var idsToRetrieve = page
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

				foreach (var left in page)
				{
					var rightId = rightIdSelector(left) ?? Guid.Empty;

					cache.TryGetValue(rightId, out var right);

					result.Add(resultSelector(left, right));
				}

				yield return result;
			}
		}

		public static IEnumerable<TResult> Join<TLeft, TRight, TResult>(
			this IEnumerable<TLeft> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, ApiObjectReference<TRight>?> rightIdSelector,
			Func<TLeft, TRight, TResult> resultSelector,
			int batchSize = 250)
			where TLeft : ApiObject<TLeft>
			where TRight : ApiObject<TRight>
		{
			return leftSource
				.Batch(batchSize)
				.Join(rightRepository, rightIdSelector, resultSelector)
				.Flatten();
		}

		public static IEnumerable<TResult> Join<TLeft, TRight, TResult>(
			this IEnumerable<TLeft> leftSource,
			Repository<TRight> rightRepository,
			Func<TLeft, IEnumerable<ApiObjectReference<TRight>?>> rightIdsSelector,
			Func<TLeft, IEnumerable<TRight>, TResult> resultSelector,
			int batchSize = 250)
			where TLeft : ApiObject<TLeft>
			where TRight : ApiObject<TRight>
		{
			return leftSource
				.Batch(batchSize)
				.Join(rightRepository, rightIdsSelector, resultSelector)
				.Flatten();
		}
	}
}
