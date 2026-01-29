namespace Skyline.DataMiner.MediaOps.Live.GQI.API
{
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.MediaOps.Live.API;

	public interface IGqiMediaOpsLiveApi : IMediaOpsLiveApi
	{
		GQIDMS GqiDms { get; }
	}
}