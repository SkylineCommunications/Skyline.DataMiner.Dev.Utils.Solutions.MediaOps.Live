namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class CreateConnectionsRequest
	{
		public CreateConnectionsRequest(ICollection<ConnectionInfo> connections)
		{
			Connections = connections ?? throw new ArgumentNullException(nameof(connections));
		}

		public ICollection<ConnectionInfo> Connections { get; }

		public class ConnectionInfo
		{
			public ConnectionInfo(Endpoint source, Endpoint destination)
			{
				SourceEndpoint = source ?? throw new ArgumentNullException(nameof(source));
				DestinationEndpoint = destination ?? throw new ArgumentNullException(nameof(destination));
			}

			public Endpoint SourceEndpoint { get; }

			public Endpoint DestinationEndpoint { get; }
		}
	}
}
