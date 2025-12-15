namespace Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages
{
	using System;

	public class PendingConnectionAction
	{
		public ConnectionAction Action { get; set; }

		public DateTimeOffset Time { get; set; }

		public EndpointInfo Destination { get; set; }

		public EndpointInfo PendingSource { get; set; }

		public TimeSpan Timeout { get; set; }
	}
}
