namespace Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.InterApp.Messages
{
	using System;

	public class PendingConnectionAction
	{
		public ConnectionAction Action { get; set; }

		public DateTimeOffset Time { get; set; }

		public EndpointInfo Destination { get; set; }

		public EndpointInfo PendingSource { get; set; }

		public TimeSpan Timeout { get; set; }

		/// <summary>
		/// Gets or sets the connection handler script that will handle this action.
		/// </summary>
		public string ConnectionHandlerScript { get; set; }
	}
}
