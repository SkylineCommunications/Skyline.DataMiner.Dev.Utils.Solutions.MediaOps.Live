namespace CheckMediaOpsLive
{
	using System;

	using Skyline.DataMiner.BpaLib;

	public sealed class BpaTestResult : ABpaTestResult
	{
		public BpaTestResult()
		{
			TestExecuted = true;
		}

		public BpaTestResult(Exception ex)
		{
			TestExecuted = false;
			Outcome = BpaTestOutcome.IssuesDetected;
			DetailedJsonResult = ex.ToString();
			ResultMessage = $"Exception during test execution: {ex.Message}.";
		}
	}
}
