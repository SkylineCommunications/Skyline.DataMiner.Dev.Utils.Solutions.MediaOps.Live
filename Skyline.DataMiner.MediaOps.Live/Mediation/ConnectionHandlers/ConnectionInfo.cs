namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class ConnectionInfo
	{
		public ConnectionInfo(Endpoint destination, Endpoint source)
		{
			DestinationEndpoint = destination ?? throw new ArgumentNullException(nameof(destination));
			SourceEndpoint = source;
			IsConnected = source != null;
		}

		public ConnectionInfo(Endpoint destination, bool isConnected)
		{
			DestinationEndpoint = destination ?? throw new ArgumentNullException(nameof(destination));
			SourceEndpoint = null;
			IsConnected = isConnected;
		}

		public Endpoint DestinationEndpoint { get; }

		public Endpoint SourceEndpoint { get; }

		public bool IsConnected { get; }
	}
}
