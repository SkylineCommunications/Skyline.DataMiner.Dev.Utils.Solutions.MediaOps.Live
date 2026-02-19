namespace Skyline.DataMiner.Solutions.MediaOps.Live.GQI.API
{
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;

	public interface IGqiMediaOpsLiveApi : IMediaOpsLiveApi
	{
		GQIDMS GqiDms { get; }
	}
}