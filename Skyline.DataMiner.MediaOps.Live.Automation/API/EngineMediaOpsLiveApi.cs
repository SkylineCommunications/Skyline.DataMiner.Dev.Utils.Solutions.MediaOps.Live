namespace Skyline.DataMiner.MediaOps.Live.Automation.API
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.Net;

	public class EngineMediaOpsLiveApi : MediaOpsLiveApi
	{
		public EngineMediaOpsLiveApi(IEngine engine, IConnection connection) : base(connection)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		public EngineMediaOpsLiveApi(IEngine engine) : base(engine.GetUserConnection())
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		public IEngine Engine { get; }
	}
}
