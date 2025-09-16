namespace Skyline.DataMiner.MediaOps.Live.Logging
{
	using System;

	using Skyline.DataMiner.Automation;

	public class EngineLogger : LoggerBase
	{
		private readonly IEngine _engine;

		public EngineLogger(IEngine engine)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		public override void Log(string message, LogType type = LogType.Information)
		{
			var logLevel = type == LogType.Debug ? 1 : -1;

			_engine.Log(message, ConvertLogType(type), logLevel);
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
