namespace Skyline.DataMiner.MediaOps.Live.Mediation.InterApp
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class EndpointInfo
	{
		public EndpointInfo()
		{
		}

		public EndpointInfo(Endpoint endpoint)
		{
			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			ID = endpoint.ID;
			Name = endpoint.Name;
		}

		public Guid ID { get; set; }

		public string Name { get; set; }
	}
}
