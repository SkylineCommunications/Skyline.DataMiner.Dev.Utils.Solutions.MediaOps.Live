namespace Skyline.DataMiner.MediaOps.Live.Mediation.Data
{
	using System.Collections.Generic;

	public class CreateConnectionsRequest
	{
		public ICollection<ConnectionInfo> Connections { get; set; }
	}
}
