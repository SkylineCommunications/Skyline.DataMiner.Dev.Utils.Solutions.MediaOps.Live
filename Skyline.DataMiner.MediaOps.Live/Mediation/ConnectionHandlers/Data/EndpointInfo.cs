namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Contains information about an endpoint.
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
		/// <param name="instance">The endpoint instance.</param>
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
		/// Gets or sets the endpoint ID.
		/// </summary>
		public Guid ID { get; set; }

		/// <summary>
		/// Gets or sets the endpoint name.
		/// </summary>
		public string Name { get; set; }
	}
}
