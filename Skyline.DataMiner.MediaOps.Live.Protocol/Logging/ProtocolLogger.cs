namespace Skyline.DataMiner.Solutions.MediaOps.Live.Protocol.Logging
{
	using System;
	using Skyline.DataMiner.Scripting;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Logging;
	using LogType = Skyline.DataMiner.Solutions.MediaOps.Live.Logging.LogType;

	public class ProtocolLogger : LoggerBase
	{
		private readonly SLProtocol _protocol;

		public ProtocolLogger(SLProtocol protocol)
		{
			_protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
		}

		public override void Log(string message, LogType type = LogType.Information)
		{
			// Hide debug messages from the log by default.
			if (type == LogType.Debug)
			{
				_protocol.Log(message, ConvertLogType(type), LogLevel.Level1);
				return;
			}

			_protocol.Log(message, ConvertLogType(type));
		}

		private Scripting.LogType ConvertLogType(LogType type)
		{
			return type switch
			{
				LogType.Debug => Scripting.LogType.DebugInfo,
				LogType.Information => Scripting.LogType.Information,
				LogType.Warning or LogType.Error => Scripting.LogType.Error,
				_ => throw new InvalidOperationException($"Unknown log type: {type}"),
			};
		}
	}
}
