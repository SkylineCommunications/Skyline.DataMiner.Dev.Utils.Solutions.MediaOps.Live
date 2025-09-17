namespace Skyline.DataMiner.MediaOps.Live.Mediation.Data
{
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;

	public class CreateConnectionsRequest : IConnectionHandlerRequest
	{
		public ICollection<ConnectionInfo> Connections { get; set; }
	}
}
