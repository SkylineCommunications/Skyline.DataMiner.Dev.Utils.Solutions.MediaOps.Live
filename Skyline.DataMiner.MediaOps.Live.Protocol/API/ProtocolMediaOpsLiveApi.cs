namespace Skyline.DataMiner.MediaOps.Live.Protocol.API
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Scripting;

	public class ProtocolMediaOpsLiveApi : MediaOpsLiveApi
	{
		public ProtocolMediaOpsLiveApi(SLProtocol protocol, IConnection connection) : base(connection)
		{
			Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
		}

		public ProtocolMediaOpsLiveApi(SLProtocol protocol) : this(protocol, protocol.GetUserConnection())
		{
		}

		public SLProtocol Protocol { get; }

		public override StaticMediaOpsLiveCache GetStaticCache()
		{
			return Protocol.GetStaticMediaOpsLiveCache();
		}
	}
}
