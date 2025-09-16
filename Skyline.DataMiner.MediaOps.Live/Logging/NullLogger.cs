namespace Skyline.DataMiner.MediaOps.Live.Logging
{
	public class NullLogger : LoggerBase
	{
		public override void Log(string message, LogType type = LogType.Information)
		{
			// Intentionally left blank.
		}
	}
}
