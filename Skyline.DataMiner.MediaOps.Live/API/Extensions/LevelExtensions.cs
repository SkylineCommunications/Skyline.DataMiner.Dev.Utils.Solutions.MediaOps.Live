namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Extensions
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Extensions;

	public static class LevelExtensions
	{
		public static IEnumerable<(Level Level, TransportType TransportType)> JoinTransportTypes(
			this IEnumerable<Level> levels,
			Repository<TransportType> transportTypesRepository)
		{
			if (levels == null)
			{
				throw new ArgumentNullException(nameof(levels));
			}

			if (transportTypesRepository == null)
			{
				throw new ArgumentNullException(nameof(transportTypesRepository));
			}

			return levels
				.Batch(100)
				.JoinInBatches(
					transportTypesRepository,
					level => level.TransportType,
					(l, t) => (l, t))
				.Flatten();
		}

		public static IEnumerable<IEnumerable<(Level Level, TransportType TransportType)>> JoinTransportTypes(
			this IEnumerable<IEnumerable<Level>> levels,
			Repository<TransportType> transportTypesRepository)
		{
			if (levels == null)
			{
				throw new ArgumentNullException(nameof(levels));
			}

			if (transportTypesRepository == null)
			{
				throw new ArgumentNullException(nameof(transportTypesRepository));
			}

			return levels.JoinInBatches(
				transportTypesRepository,
				level => level.TransportType,
				(l, t) => (l, t));
		}
	}
}
