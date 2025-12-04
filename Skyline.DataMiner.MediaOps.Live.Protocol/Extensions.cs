namespace Skyline.DataMiner.MediaOps.Live.Protocol
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.Protocol.API;
	using Skyline.DataMiner.MediaOps.Live.Protocol.Logging;
	using Skyline.DataMiner.Scripting;

	public static class Extensions
	{
		public static MediaOpsLiveApi GetMediaOpsLiveApi(this SLProtocol protocol)
		{
			if (protocol is null)
			{
				throw new ArgumentNullException(nameof(protocol));
			}

			var api = new ProtocolMediaOpsLiveApi(protocol);
			api.SetLogger(new ProtocolLogger(protocol));

			return api;
		}

		public static StaticMediaOpsLiveCache GetStaticMediaOpsLiveCache(this SLProtocol protocol)
		{
			if (protocol is null)
			{
				throw new ArgumentNullException(nameof(protocol));
			}

			return StaticMediaOpsLiveCache.GetOrCreate(protocol.SLNet.RawConnection);
		}
	}
}
