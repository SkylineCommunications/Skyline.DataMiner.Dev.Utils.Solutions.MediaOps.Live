namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class EndpointInfo
	{
		public EndpointInfo()
		{
		}

		public EndpointInfo(Endpoint instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			ID = instance.ID;
			Name = instance.Name;
		}

		public Guid ID { get; set; }

		public string Name { get; set; }
	}
}
