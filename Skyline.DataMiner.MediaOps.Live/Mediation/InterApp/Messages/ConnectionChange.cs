namespace Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.InterApp.Messages
{
	using System;

	public class ConnectionChange
	{
		public DateTimeOffset Time { get; set; }

		public EndpointInfo Destination { get; set; }

		public EndpointInfo ConnectedSource { get; set; }

		public bool IsConnected { get; set; }
	}
}
