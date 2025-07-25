namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;

	public interface IConnectionHandlerEngine
	{
		IEngine Engine { get; }

		MediaOpsLiveApi Api { get; }

		IDms Dms { get; }

		void RegisterConnection(ConnectionUpdate connection);

		void RegisterConnections(ICollection<ConnectionUpdate> connections);
	}
}
