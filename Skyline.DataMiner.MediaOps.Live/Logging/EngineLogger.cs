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

		public override void LogInternal(string message, LogType type)
		{
			message += $" (User: {_engine.UserDisplayName})";

			_engine.Log(message, ConvertLogType(type), -1);
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
