namespace InterApp.Messages
{
	using System;

	public class ConnectionChange
	{
		public DateTimeOffset Time { get; set; }

		public EndpointInfo Destination { get; set; }

		public EndpointInfo ConnectedSource { get; set; }
	}
}
