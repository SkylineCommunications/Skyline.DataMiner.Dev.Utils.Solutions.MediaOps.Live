namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;

	public interface IConnectionHandlerEngine
	{
		IEngine Engine { get; }

		MediaOpsLiveApi Api { get; }

		void RegisterConnection(ConnectionInfo connectionInfo);

		void RegisterConnections(ICollection<ConnectionInfo> connectionInfos);
	}
}
