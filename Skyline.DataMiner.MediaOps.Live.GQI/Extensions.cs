namespace Skyline.DataMiner.MediaOps.Live.GQI
{
	using System;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;

	public static class Extensions
	{
		public static MediaOpsLiveApi GetMediaOpsLiveApi(this GQIDMS gqiDms)
		{
			if (gqiDms is null)
			{
				throw new ArgumentNullException(nameof(gqiDms));
			}

			var api = new MediaOpsLiveApi(gqiDms.GetConnection());

			return api;
		}

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
