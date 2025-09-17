namespace Skyline.DataMiner.MediaOps.Live.Automation.API
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Automation.Take;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.Net;

	public class EngineMediaOpsLiveApi : MediaOpsLiveApi
	{
		private readonly IEngine _engine;

		public EngineMediaOpsLiveApi(IEngine engine, IConnection connection) : base(connection)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		public EngineMediaOpsLiveApi(IEngine engine) : this(engine, engine.GetUserConnection())
		{
		}

		public override TakeHelper GetConnectionHandler()
		{
			return new EngineTakeHelper(_engine, this);
		}
	}
}
