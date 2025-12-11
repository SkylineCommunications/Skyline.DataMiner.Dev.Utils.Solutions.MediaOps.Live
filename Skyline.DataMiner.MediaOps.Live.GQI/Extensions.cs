namespace Skyline.DataMiner.MediaOps.Live.GQI
{
	using System;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.GQI.API;

	public static class Extensions
	{
		public static MediaOpsLiveApi GetMediaOpsLiveApi(this GQIDMS gqiDms)
		{
			if (gqiDms is null)
			{
				throw new ArgumentNullException(nameof(gqiDms));
			}

			var api = new GqiMediaOpsLiveApi(gqiDms);

			return api;
		}

		public static MediaOpsLiveCache GetMediaOpsLiveCache(this GQIDMS gqiDms)
		{
			if (gqiDms is null)
			{
				throw new ArgumentNullException(nameof(gqiDms));
			}

			return MediaOpsLiveCache.GetOrCreate(gqiDms.GetConnection);
		}
	}
}
