namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class DisconnectDestinationsRequest
	{
		public DisconnectDestinationsRequest(ICollection<Endpoint> destinations)
		{
			Destinations = destinations ?? throw new ArgumentNullException(nameof(destinations));
		}

		public ICollection<Endpoint> Destinations { get; }
	}
}
