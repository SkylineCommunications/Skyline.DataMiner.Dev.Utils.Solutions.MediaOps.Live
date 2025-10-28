namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Represents a request to create a connection between two endpoints.
	/// </summary>
	public class ConnectionRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionRequest"/> class.
		/// </summary>
		/// <param name="source">The source endpoint.</param>
		/// <param name="destination">The destination endpoint.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the source endpoint does not have role 'Source' or the destination endpoint does not have role 'Destination'.</exception>
		public ConnectionRequest(Endpoint source, Endpoint destination)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (!source.IsSource)
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

		/// <summary>
		/// Gets the source endpoint.
		/// </summary>
		public Endpoint Source { get; }

		/// <summary>
		/// Gets the destination endpoint.
		/// </summary>
		public Endpoint Destination { get; }

		/// <summary>
		/// Gets or sets optional metadata associated with this connection request.
		/// </summary>
		public object MetaData { get; set; }
	}
}
