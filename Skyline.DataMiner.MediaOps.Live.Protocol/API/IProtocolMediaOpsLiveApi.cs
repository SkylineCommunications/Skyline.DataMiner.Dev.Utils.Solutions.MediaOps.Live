namespace Skyline.DataMiner.MediaOps.Live.Protocol.API
{
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.Scripting;

	public interface IProtocolMediaOpsLiveApi : IMediaOpsLiveApi
	{
		SLProtocol Protocol { get; }
	}
}