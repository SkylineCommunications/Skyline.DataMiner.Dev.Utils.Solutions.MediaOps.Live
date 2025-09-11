namespace Skyline.DataMiner.MediaOps.Live.Logging
{
	using System;

	public abstract class LoggerBase : ILogger
	{
		public virtual void Log(string message, LogLevel level = LogLevel.Information)
		{
			var formattedMessage = FormatMessage(message, level);
			LogInternal(formattedMessage);
		}

		public virtual void Debug(string message)
		{
			Log(message, LogLevel.Debug);
		}

		public virtual void Information(string message)
		{
			Log(message, LogLevel.Information);
		}

		public virtual void Warning(string message)
		{
			Log(message, LogLevel.Warning);
		}

		public virtual void Error(string message)
		{
			Log(message, LogLevel.Error);
		}

		public virtual void Error(string message, Exception exception)
		{
			var fullMessage = message;

			if (exception != null)
			{
				fullMessage += Environment.NewLine +
					"Exception: " + exception;
			}

			Log(fullMessage, LogLevel.Error);
		}

		public abstract void LogInternal(string message);

		protected string FormatMessage(string message, LogLevel level)
		{
			var date = FormatDateTimeNow();
			var thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
			var abr = GetLogLevelAbbreviation(level);

			return $"{date}|{thread}|{abr}|{message}";
		}

		protected string FormatDateTime(DateTime dateTime)
		{
			return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
		}

		protected string FormatDateTimeNow()
		{
			return FormatDateTime(DateTime.Now);
		}

		protected string GetLogLevelAbbreviation(LogLevel level)
		{
			return level switch
			{
				LogLevel.Debug => "DBG",
				LogLevel.Information => "INF",
				LogLevel.Warning => "WRN",
				LogLevel.Error => "ERR",
				_ => throw new InvalidOperationException($"Unknown log level: {level}"),
			};
		}
	}
}
