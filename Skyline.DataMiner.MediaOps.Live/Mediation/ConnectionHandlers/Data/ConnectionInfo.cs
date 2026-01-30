namespace Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.ConnectionHandlers.Data
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class ConnectionInfo
	{
		public ConnectionInfo()
		{
		}

		public ConnectionInfo(Endpoint source, Endpoint destination)
		{
			if (source == null)
			{
				// source is allowed to be null, when the destination is disconnected
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (source != null && !source.IsSource)
			{
				throw new ArgumentException("Source endpoint must have role 'Source'", nameof(source));
			}

			if (!destination.IsDestination)
			{
				throw new ArgumentException("Destination endpoint must have role 'Destination'", nameof(destination));
			}

			SourceEndpoint = source != null ? new EndpointInfo(source) : null;
			DestinationEndpoint = new EndpointInfo(destination);
		}

		public EndpointInfo SourceEndpoint { get; set; }

		public EndpointInfo DestinationEndpoint { get; set; }
	}
}
