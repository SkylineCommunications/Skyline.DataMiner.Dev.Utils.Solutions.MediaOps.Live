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

			var analyzer = new ErrorAnalyzer(api, context.SLNet);
			analyzer.Analyze();

			var statisticsCollector = new MediaOpsLiveStatisticsCollector(api);
			var statistics = statisticsCollector.CollectStatistics();

			var result = new Result
			{
				Version = api.GetVersion(),
				Statistics = statistics,
				Errors = analyzer.Errors,
			};

			return new BpaTestResult
			{
				TestExecuted = true,
				Outcome = result.Outcome,
				ResultMessage = result.Message,
				DetailedJsonResult = JsonConvert.SerializeObject(result),
			};
		}
	}
}
