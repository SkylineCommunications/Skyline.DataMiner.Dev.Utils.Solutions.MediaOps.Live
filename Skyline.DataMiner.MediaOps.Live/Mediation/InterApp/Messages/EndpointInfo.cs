namespace Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Contains endpoint information for inter-app messaging.
	/// </summary>
	public class EndpointInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EndpointInfo"/> class.
		/// </summary>
		public EndpointInfo()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EndpointInfo"/> class from an endpoint instance.
		/// </summary>
		/// <param name="endpoint">The endpoint instance.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoint"/> is null.</exception>
		public EndpointInfo(Endpoint endpoint)
		{
			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			ID = endpoint.ID;
			Name = endpoint.Name;
		}

		/// <summary>
		/// Gets or sets the endpoint ID.
		/// </summary>
		public Guid ID { get; set; }

		/// <summary>
		/// Gets or sets the endpoint name.
		/// </summary>
		public string Name { get; set; }
	}
}
