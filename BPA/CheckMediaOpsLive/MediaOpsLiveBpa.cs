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

			var statistics = new MediaOpsMetrics(api);
			statistics.CollectStatistics();

			var result = new Result
			{
				Version = api.GetVersion(),
				Metrics = statistics.Results,
				Errors = analyzer.Errors,
			};

			return new BpaTestResult
			{
				TestExecuted = true,
				Outcome = result.Outcome,
				DetailedJsonResult = JsonConvert.SerializeObject(result),
			};
		}
	}
}
