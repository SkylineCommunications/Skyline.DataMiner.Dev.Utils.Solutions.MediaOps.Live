namespace Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.InterApp.Messages
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

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
