namespace Skyline.DataMiner.MediaOps.Live.Protocol.API
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Scripting;

	public class ProtocolMediaOpsLiveApi : MediaOpsLiveApi
	{
		private readonly SLProtocol _protocol;

		public ProtocolMediaOpsLiveApi(SLProtocol protocol, IConnection connection) : base(connection)
		{
			_protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
		}

		public ProtocolMediaOpsLiveApi(SLProtocol protocol) : this(protocol, protocol.GetUserConnection())
		{
		}
	}
}
