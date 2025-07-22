namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class DisconnectRequest
	{
		public DisconnectRequest(Endpoint destination)
		{
			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (!destination.IsDestination)
			{
				throw new ArgumentException("Destination endpoint must have role 'Destination'", nameof(destination));
			}

			Destination = destination;
		}

		public object MetaData { get; set; }

		public Endpoint Destination { get; }
	}
}
