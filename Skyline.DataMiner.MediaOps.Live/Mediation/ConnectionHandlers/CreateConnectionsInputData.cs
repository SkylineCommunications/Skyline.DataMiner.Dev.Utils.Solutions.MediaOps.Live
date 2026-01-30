namespace Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.ConnectionHandlers.Data;

	public class CreateConnectionsInputData : IConnectionHandlerInputData
	{
		public ICollection<ConnectionInfo> Connections { get; set; }
	}
}
