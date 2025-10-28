namespace Skyline.DataMiner.MediaOps.Live.Logging
{
	/// <summary>
	/// A logger implementation that does not log anything (null object pattern).
	/// </summary>
	public class NullLogger : LoggerBase
	{
		/// <summary>
		/// Logs a message (no-op implementation).
		/// </summary>
		/// <param name="message">The message to log (ignored).</param>
		/// <param name="type">The log type/severity level (ignored).</param>
		public override void Log(string message, LogType type = LogType.Information)
		{
			// Intentionally left blank.
		}
	}
}
