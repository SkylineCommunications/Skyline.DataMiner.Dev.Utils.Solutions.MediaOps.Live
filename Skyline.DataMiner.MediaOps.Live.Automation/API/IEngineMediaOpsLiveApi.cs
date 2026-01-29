namespace Skyline.DataMiner.MediaOps.Live.Automation.API
{
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API;

	public interface IEngineMediaOpsLiveApi : IMediaOpsLiveApi
	{
		IEngine Engine { get; }
	}
}