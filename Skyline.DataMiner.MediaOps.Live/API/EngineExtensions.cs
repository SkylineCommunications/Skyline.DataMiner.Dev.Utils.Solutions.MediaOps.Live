namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;

	using Skyline.DataMiner.Automation;

	public static class EngineExtensions
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
	}
}
