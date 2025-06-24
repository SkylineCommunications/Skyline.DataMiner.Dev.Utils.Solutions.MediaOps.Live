namespace InterApp.Messages
{
	using System;

	public class ClearPendingConnectionActionRequest
	{
		public DateTimeOffset StartTime { get; set; }

		public Endpoint Destination { get; set; }

		public Endpoint ConnectedSource { get; set; }
	}
}
