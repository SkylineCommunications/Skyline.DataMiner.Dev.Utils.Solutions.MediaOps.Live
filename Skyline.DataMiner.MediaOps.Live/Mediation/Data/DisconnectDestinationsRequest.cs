namespace Skyline.DataMiner.MediaOps.Live.Mediation.Data
{
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;

	public class DisconnectDestinationsRequest : IConnectionHandlerRequest
	{
		public ICollection<EndpointInfo> Destinations { get; set; }
	}
}
