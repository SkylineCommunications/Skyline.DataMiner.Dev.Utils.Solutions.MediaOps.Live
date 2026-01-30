namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.API
{
	using System;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Logging;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Plan;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Take;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Plan;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;

	public class EngineMediaOpsLiveApi : MediaOpsLiveApi, IEngineMediaOpsLiveApi
	{
		public EngineMediaOpsLiveApi(IEngine engine, IConnection connection) : base(connection)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		public EngineMediaOpsLiveApi(IEngine engine) : this(engine, engine.GetUserConnection())
		{
			SetLogger(new EngineLogger(engine));
		}

		public IEngine Engine { get; }

		public override MediaOpsLiveCache GetCache()
		{
			return Engine.GetMediaOpsLiveCache();
		}

		public override TakeHelper GetConnectionHandler()
		{
			return new EngineTakeHelper(this);
		}

		internal override MediaOpsPlanHelper GetMediaOpsPlanHelper()
		{
			return new EngineMediaOpsPlanHelper(this);
		}
	}
}
