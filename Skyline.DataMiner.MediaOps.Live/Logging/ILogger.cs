namespace Skyline.DataMiner.Solutions.MediaOps.Live.Logging
{
	using System;

	public interface ILogger
	{
		void Log(string message, LogType type = LogType.Information);

		void Debug(string message);

		void Information(string message);

		void Warning(string message);

		void Error(string message, Exception exception = null);
	}
}
