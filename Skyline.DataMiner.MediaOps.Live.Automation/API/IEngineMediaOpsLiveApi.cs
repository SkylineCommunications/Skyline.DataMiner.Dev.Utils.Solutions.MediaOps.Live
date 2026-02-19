namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.API
{
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;

	public interface IEngineMediaOpsLiveApi : IMediaOpsLiveApi
	{
		IEngine Engine { get; }
	}
}