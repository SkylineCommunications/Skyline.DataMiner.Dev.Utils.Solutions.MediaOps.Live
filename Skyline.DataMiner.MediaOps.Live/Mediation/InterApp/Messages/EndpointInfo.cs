namespace Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Represents endpoint information for inter-app communication.
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
		/// Initializes a new instance of the <see cref="EndpointInfo"/> class.
		/// </summary>
		/// <param name="endpoint">The endpoint to copy information from.</param>
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
		/// Gets or sets the unique identifier of the endpoint.
		/// </summary>
		public Guid ID { get; set; }

		/// <summary>
		/// Gets or sets the name of the endpoint.
		/// </summary>
		public string Name { get; set; }
	}
}
