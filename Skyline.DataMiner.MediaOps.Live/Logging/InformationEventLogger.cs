namespace Skyline.DataMiner.MediaOps.Live.Logging
{
	using System;

	using Skyline.DataMiner.Automation;

	public class InformationEventLogger : LoggerBase, ILogger
	{
		private readonly IEngine _engine;

		public InformationEventLogger(IEngine engine)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		public override void LogInternal(string message)
		{
			// Intentionally left blank.
		}
	}
}
