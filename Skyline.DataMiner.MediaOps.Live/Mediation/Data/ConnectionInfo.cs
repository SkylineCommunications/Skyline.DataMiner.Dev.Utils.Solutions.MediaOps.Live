namespace Skyline.DataMiner.MediaOps.Live.Mediation.Data
{
	using System;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement;

	public class ConnectionInfo
	{
		public EndpointInfo SourceEndpoint { get; set; }

		public EndpointInfo DestinationEndpoint { get; set; }

		public static ConnectionInfo Create(Endpoint source, Endpoint destination)
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

			return new ConnectionInfo
			{
				SourceEndpoint = source != null ? EndpointInfo.Create(source) : null,
				DestinationEndpoint = EndpointInfo.Create(destination),
			};
		}
	}
}
