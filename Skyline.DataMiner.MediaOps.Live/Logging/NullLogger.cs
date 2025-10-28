namespace Skyline.DataMiner.MediaOps.Live.Logging
{
	/// <summary>
	/// A logger implementation that discards all log messages.
	/// </summary>
	public class NullLogger : LoggerBase
	{
		/// <summary>
		/// Logs a message. This implementation discards the message.
		/// </summary>
		/// <param name="message">The message to log.</param>
		/// <param name="type">The type of log message.</param>
		public override void Log(string message, LogType type = LogType.Information)
		{
			// Intentionally left blank.
		}
	}
}
