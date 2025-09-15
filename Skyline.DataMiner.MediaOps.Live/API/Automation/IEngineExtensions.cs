namespace Skyline.DataMiner.MediaOps.Live.API.Automation
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.Logging;

	public static class IEngineExtensions
	{
		public static MediaOpsLiveApi GetMediaOpsLiveApi(this IEngine engine)
		{
			if (engine is null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			var api = new MediaOpsLiveApi(engine.GetUserConnection());
			api.SetEngine(engine);
			api.SetLogger(new EngineLogger(engine));

			return api;
		}

		public static StaticMediaOpsLiveCache GetStaticMediaOpsLiveApiCache(this IEngine engine)
		{
			if (engine is null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			return StaticMediaOpsLiveCache.GetOrCreate(Engine.SLNetRaw);
		}
	}
}
