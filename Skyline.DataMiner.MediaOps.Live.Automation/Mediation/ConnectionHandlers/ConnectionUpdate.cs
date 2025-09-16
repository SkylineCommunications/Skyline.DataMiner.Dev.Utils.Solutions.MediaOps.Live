namespace Skyline.DataMiner.MediaOps.Live.Automation.Mediation.ConnectionHandlers
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class ConnectionUpdate
	{
		public ConnectionUpdate(Endpoint source, Endpoint destination)
		{
			if (source != null && !source.IsSource)
			{
				throw new ArgumentException("The source endpoint must be a source.", nameof(source));
			}

			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (!destination.IsDestination)
			{
				throw new ArgumentException("The destination endpoint must be a destination.", nameof(destination));
			}

			DestinationEndpoint = destination;
			SourceEndpoint = source;
			IsConnected = source != null;
		}

		public ConnectionUpdate(Endpoint destination, bool isConnected)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (!destination.IsDestination)
			{
				throw new ArgumentException("The endpoint must be a destination.", nameof(destination));
			}

			DestinationEndpoint = destination;
			SourceEndpoint = null;
			IsConnected = isConnected;
		}

		public Endpoint DestinationEndpoint { get; }

		public Endpoint SourceEndpoint { get; }

		public bool IsConnected { get; }
	}
}
