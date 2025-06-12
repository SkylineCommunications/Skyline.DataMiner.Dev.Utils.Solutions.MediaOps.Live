namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class ConnectionRequest
	{
		public ConnectionRequest(Endpoint source, Endpoint destination)
		{
			if (source == null)
			{
				// ignore
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

			Source = source;
			Destination = destination;
		}

		public Endpoint Source { get; }

		public Endpoint Destination { get; }
	}
}
