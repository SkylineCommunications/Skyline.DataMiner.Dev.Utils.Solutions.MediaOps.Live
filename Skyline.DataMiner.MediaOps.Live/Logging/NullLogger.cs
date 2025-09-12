namespace Skyline.DataMiner.MediaOps.Live.Logging
{
	public class NullLogger : LoggerBase
	{
		public override void LogInternal(string message)
		{
			// Intentionally left blank.
		}
	}
}
