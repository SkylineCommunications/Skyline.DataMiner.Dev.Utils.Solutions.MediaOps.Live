namespace Skyline.DataMiner.MediaOps.Live.GQI
{
	using System;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;

	/// <summary>
	/// Extension methods for GQI DMS operations.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Gets a MediaOps Live API instance from the GQI DMS.
		/// </summary>
		/// <param name="gqiDms">The GQI DMS instance.</param>
		/// <returns>A MediaOps Live API instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="gqiDms"/> is null.</exception>
		public static MediaOpsLiveApi GetMediaOpsLiveApi(this GQIDMS gqiDms)
		{
			if (gqiDms is null)
			{
				throw new ArgumentNullException(nameof(gqiDms));
			}

			var api = new MediaOpsLiveApi(gqiDms.GetConnection());

			return api;
		}

		/// <summary>
		/// Gets the static MediaOps Live cache from the GQI DMS.
		/// </summary>
		/// <param name="gqiDms">The GQI DMS instance.</param>
		/// <returns>The static MediaOps Live cache instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="gqiDms"/> is null.</exception>
		public static StaticMediaOpsLiveCache GetStaticMediaOpsLiveCache(this GQIDMS gqiDms)
		{
			if (gqiDms is null)
			{
				throw new ArgumentNullException(nameof(gqiDms));
			}

			return StaticMediaOpsLiveCache.GetOrCreate(gqiDms.GetConnection);
		}
	}
}
