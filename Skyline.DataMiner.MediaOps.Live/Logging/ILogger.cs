namespace Skyline.DataMiner.MediaOps.Live.Logging
{
	using System;

	public interface ILogger
	{
		void Log(string message, LogLevel level = LogLevel.Information);

		void Debug(string message);

		void Information(string message);

		void Warning(string message);

		void Error(string message, Exception exception = null);
	}
}
