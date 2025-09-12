namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Logging;

	public interface IConnectionHandlerEngine
	{
		IEngine Engine { get; }

		MediaOpsLiveApi Api { get; }

		ILogger Logger { get; }

		IDms Dms { get; }

		void Log(string message, LogLevel logLevel = LogLevel.Information);

		void RegisterConnection(ConnectionUpdate connection);

		void RegisterConnections(ICollection<ConnectionUpdate> connections);
	}
}
