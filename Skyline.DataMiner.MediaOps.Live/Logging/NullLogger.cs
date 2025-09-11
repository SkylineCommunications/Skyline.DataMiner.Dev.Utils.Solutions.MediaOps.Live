namespace Skyline.DataMiner.MediaOps.Live.Logging
{
	public class NullLogger : LoggerBase, ILogger
	{
		public override void LogInternal(string message)
		{
			// Intentionally left blank.
		}
	}
}
