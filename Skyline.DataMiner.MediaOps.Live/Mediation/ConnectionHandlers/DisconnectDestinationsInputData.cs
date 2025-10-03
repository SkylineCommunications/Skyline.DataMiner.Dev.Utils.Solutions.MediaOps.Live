namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data;

	public class DisconnectDestinationsInputData : IConnectionHandlerInputData
	{
		public ICollection<EndpointInfo> Destinations { get; set; }
	}
}
