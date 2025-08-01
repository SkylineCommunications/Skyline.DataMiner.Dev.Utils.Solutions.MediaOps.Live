namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Tools;

	public static class GetApiExtensions
	{
		public static MediaOpsLiveApi GetMediaOpsLiveApi(this IEngine engine)
		{
			if (engine is null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			var api = new MediaOpsLiveApi(engine.GetUserConnection());
			api.SetEngine(engine);

			return api;
		}

		public static MediaOpsLiveApi GetMediaOpsLiveApi(this GQIDMS gqidms)
		{
			if (gqidms is null)
			{
				throw new ArgumentNullException(nameof(gqidms));
			}

			var api = new MediaOpsLiveApi(gqidms.GetConnection());

			return api;
		}

		public static StaticMediaOpsLiveCache GetStaticMediaOpsLiveApiCache(this IEngine engine)
		{
			if (engine is null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			return StaticMediaOpsLiveCache.GetOrCreate(() => Engine.SLNetRaw);
		}

		public static StaticMediaOpsLiveCache GetStaticMediaOpsLiveCache(this GQIDMS gqidms)
		{
			if (gqidms is null)
			{
				throw new ArgumentNullException(nameof(gqidms));
			}

			return StaticMediaOpsLiveCache.GetOrCreate(
				() =>
				{
					var baseConnection = gqidms.GetConnection();
					return ConnectionHelper.CloneConnection(baseConnection, "MediaOps.Live - Connection");
				});
		}
	}
}
