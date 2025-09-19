namespace CheckMediaOpsLive
{
	using Skyline.DataMiner.BpaLib;
	using Skyline.DataMiner.MediaOps.Live.API.Extensions;
	using Newtonsoft.Json;

	public static class MediaOpsLiveBpa
	{
		public static BpaTestResult Execute(BpaExecuteContext context)
		{
			var api = context.SLNet.GetMediaOpsLiveApi();

			var statistics = Statistics.CollectStatistics(api);

			var result = new Result
			{
				Version = api.GetVersion(),
				Metrics = statistics,
			};

			return new BpaTestResult
			{
				Outcome = BpaTestOutcome.NoIssues,
				DetailedJsonResult = JsonConvert.SerializeObject(result),
			};
		}
	}
}
