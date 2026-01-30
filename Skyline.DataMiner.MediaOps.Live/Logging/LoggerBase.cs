namespace Skyline.DataMiner.Solutions.MediaOps.Live.Logging
{
	using System;

	public abstract class LoggerBase : ILogger
	{
		public abstract void Log(string message, LogType type = LogType.Information);

		public virtual void Debug(string message)
		{
			Log(message, LogType.Debug);
		}

		public virtual void Information(string message)
		{
			Log(message, LogType.Information);
		}

		public virtual void Warning(string message)
		{
			Log(message, LogType.Warning);
		}

		public virtual void Error(string message, Exception exception = null)
		{
			var fullMessage = message;

			if (exception != null)
			{
				fullMessage += Environment.NewLine +
					"Exception: " + exception;
			}

			Log(fullMessage, LogType.Error);
		}

		protected string FormatDateTime(DateTime dateTime)
		{
			return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
		}

		protected string FormatDateTimeNow()
		{
			return FormatDateTime(DateTime.Now);
		}

		protected string GetLogTypeAbbreviation(LogType type)
		{
			return type switch
			{
				LogType.Debug => "DBG",
				LogType.Information => "INF",
				LogType.Warning => "WRN",
				LogType.Error => "ERR",
				_ => throw new InvalidOperationException($"Unknown log type: {type}"),
			};
		}
	}
}
