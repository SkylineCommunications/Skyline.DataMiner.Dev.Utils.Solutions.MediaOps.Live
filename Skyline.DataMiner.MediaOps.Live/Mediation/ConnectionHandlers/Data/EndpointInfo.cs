namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Represents endpoint information including ID and name.
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
		/// <param name="instance">The endpoint instance to copy information from.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> is null.</exception>
		public EndpointInfo(Endpoint instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			ID = instance.ID;
			Name = instance.Name;
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
