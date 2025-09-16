namespace Skyline.DataMiner.MediaOps.Live.Automation.Mediation.ConnectionHandlers
{
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Logging;

	using LogType = Skyline.DataMiner.MediaOps.Live.Logging.LogType;

	public interface IConnectionHandlerEngine
	{
		MediaOpsLiveApi Api { get; }

		ILogger Logger { get; }

		void Log(string message, LogType logLevel = LogType.Information);

		void RegisterConnection(ConnectionUpdate connection);

		void RegisterConnections(ICollection<ConnectionUpdate> connections);
	}
}
