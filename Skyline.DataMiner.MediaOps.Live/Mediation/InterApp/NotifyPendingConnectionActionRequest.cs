namespace InterApp.Messages
{
	using System;

	public class NotifyPendingConnectionActionRequest
	{
		public ConnectionAction Action { get; set; }

		public DateTimeOffset StartTime { get; set; }

		public Endpoint Destination { get; set; }

		public Endpoint PendingSource { get; set; }
	}
}
