namespace Skyline.DataMiner.Solutions.MediaOps.Live.Protocol.API
{
	using System;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Scripting;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Protocol.Logging;

	public class ProtocolMediaOpsLiveApi : MediaOpsLiveApi, IProtocolMediaOpsLiveApi
	{
		public ProtocolMediaOpsLiveApi(SLProtocol protocol, IConnection connection) : base(connection)
		{
			Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));

			SetLogger(new ProtocolLogger(protocol));
		}

		public ProtocolMediaOpsLiveApi(SLProtocol protocol) : this(protocol, protocol.GetUserConnection())
		{
		}

		public SLProtocol Protocol { get; }

		public override MediaOpsLiveCache GetCache()
		{
			return Protocol.GetMediaOpsLiveCache();
		}
	}
}
