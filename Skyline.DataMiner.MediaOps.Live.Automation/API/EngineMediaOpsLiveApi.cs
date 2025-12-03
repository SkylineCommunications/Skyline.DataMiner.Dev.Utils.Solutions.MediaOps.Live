namespace Skyline.DataMiner.MediaOps.Live.Automation.API
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.Automation.Take;
	using Skyline.DataMiner.MediaOps.Live.Automation.Tools;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Net;

	public class EngineMediaOpsLiveApi : MediaOpsLiveApi
	{
		public EngineMediaOpsLiveApi(IEngine engine, IConnection connection) : base(connection)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		public EngineMediaOpsLiveApi(IEngine engine) : this(engine, engine.GetUserConnection())
		{
		}

		public IEngine Engine { get; }

		public override StaticMediaOpsLiveCache GetStaticCache()
		{
			return Engine.GetStaticMediaOpsLiveApiCache();
		}

		public override TakeHelper GetConnectionHandler()
		{
			return new EngineTakeHelper(Engine, this);
		}

		internal override MediaOpsPlanHelper GetMediaOpsPlanHelper()
		{
			return new EngineMediaOpsPlanHelper(Engine);
		}
	}
}
