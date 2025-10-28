namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Represents a request to disconnect an endpoint.
	/// </summary>
	public class DisconnectRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DisconnectRequest"/> class.
		/// </summary>
		/// <param name="destination">The destination endpoint to disconnect.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the destination endpoint does not have role 'Destination'.</exception>
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

		/// <summary>
		/// Gets the destination endpoint to disconnect.
		/// </summary>
		public Endpoint Destination { get; }

		/// <summary>
		/// Gets or sets optional metadata associated with this disconnect request.
		/// </summary>
		public object MetaData { get; set; }
	}
}
