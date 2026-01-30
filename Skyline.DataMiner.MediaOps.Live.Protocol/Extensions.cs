namespace Skyline.DataMiner.Solutions.MediaOps.Live.Protocol
{
	using System;
	using Skyline.DataMiner.Scripting;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Protocol.API;

	public static class Extensions
	{
		public static IProtocolMediaOpsLiveApi GetMediaOpsLiveApi(this SLProtocol protocol)
		{
			if (protocol is null)
			{
				throw new ArgumentNullException(nameof(protocol));
			}

			var api = new ProtocolMediaOpsLiveApi(protocol);

			return api;
		}

		public static MediaOpsLiveCache GetMediaOpsLiveCache(this SLProtocol protocol)
		{
			if (protocol is null)
			{
				throw new ArgumentNullException(nameof(protocol));
			}

			return MediaOpsLiveCache.GetOrCreate(protocol.SLNet.RawConnection);
		}
	}
}
