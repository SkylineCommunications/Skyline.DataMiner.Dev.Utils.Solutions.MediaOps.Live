namespace Skyline.DataMiner.Solutions.MediaOps.Live.Protocol.API
{
	using Skyline.DataMiner.Scripting;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;

	public interface IProtocolMediaOpsLiveApi : IMediaOpsLiveApi
	{
		SLProtocol Protocol { get; }
	}
}