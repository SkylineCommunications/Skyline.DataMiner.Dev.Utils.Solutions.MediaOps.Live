namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API;

	public interface IConnectionHandlerEngine
	{
		IEngine Engine { get; }

		MediaOpsLiveApi Api { get; }

		void RegisterConnection(ConnectionInfo connection);

		void RegisterConnections(ICollection<ConnectionInfo> connections);
	}
}
