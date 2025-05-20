namespace Skyline.DataMiner.MediaOps.Live.API.Extensions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories;

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

			return levels.JoinInBatches(
				transportTypesRepository,
				level => level.TransportType,
				(l, t) => (l, t));
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

			return levels.Join(
				transportTypesRepository,
				level => level.TransportType,
				(l, t) => (l, t));
		}
	}
}
