namespace Skyline.DataMiner.MediaOps.Live.Automation.Logging
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Logging;

	using Automation = Skyline.DataMiner.Automation;
	using LogType = Skyline.DataMiner.MediaOps.Live.Logging.LogType;

	public class EngineLogger : LoggerBase
	{
		public EngineLogger(IEngine engine)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		public IEngine Engine { get; }

		public override void Log(string message, LogType type = LogType.Information)
		{
			// Hide debug messages from the log by default.
			var logLevel = type == LogType.Debug ? 1 : -1;

			Engine.Log(message, ConvertLogType(type), logLevel);

			if (type == LogType.Error)
			{
				Engine.GenerateInformation($"An error occurred: {message}");
			}
		}

		private Automation.LogType ConvertLogType(LogType type)
		{
			return type switch
			{
				LogType.Debug => Automation.LogType.Debug,
				LogType.Information => Automation.LogType.Information,
				LogType.Warning or LogType.Error => Automation.LogType.Error,
				_ => throw new InvalidOperationException($"Unknown log type: {type}"),
			};
		}
	}
}
