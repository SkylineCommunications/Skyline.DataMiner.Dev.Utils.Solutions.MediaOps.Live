namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Logging;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Tools;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;

	internal static class PerformanceLoggerFactory
	{
		internal static IPerformanceLogger Create(string methodName)
		{
			if (UnitTestDetector.IsInUnitTest)
			{
				return new NullPerformanceLogger();
			}

			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			return new PerformanceFileLogger(methodName, performanceLogFilename);
		}
	}
}