namespace Skyline.DataMiner.MediaOps.Live.Logging
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Models;

	/// <summary>
	/// A no-op implementation of <see cref="IPerformanceLogger"/> that does nothing when Report is called.
	/// Useful for unit testing to avoid file creation overhead.
	/// </summary>
	internal class NullPerformanceLogger : IPerformanceLogger
	{
		/// <inheritdoc/>
		public void Report(List<PerformanceData> data)
		{
			// Intentionally left blank.
		}
	}
}
