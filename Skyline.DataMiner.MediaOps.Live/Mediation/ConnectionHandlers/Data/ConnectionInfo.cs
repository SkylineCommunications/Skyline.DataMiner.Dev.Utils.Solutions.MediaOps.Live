namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Represents connection information between source and destination endpoints.
	/// </summary>
	public class ConnectionInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionInfo"/> class.
		/// </summary>
		public ConnectionInfo()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionInfo"/> class.
		/// </summary>
		/// <param name="source">The source endpoint. Can be null when the destination is disconnected.</param>
		/// <param name="destination">The destination endpoint.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the source endpoint doesn't have the 'Source' role or the destination endpoint doesn't have the 'Destination' role.</exception>
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

		/// <summary>
		/// Gets or sets the source endpoint information.
		/// </summary>
		public EndpointInfo SourceEndpoint { get; set; }

		/// <summary>
		/// Gets or sets the destination endpoint information.
		/// </summary>
		public EndpointInfo DestinationEndpoint { get; set; }
	}
}
