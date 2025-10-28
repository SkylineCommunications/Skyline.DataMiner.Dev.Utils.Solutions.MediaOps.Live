namespace Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages
{
	using System;

	/// <summary>
	/// Represents a pending connection action for inter-app messaging.
	/// </summary>
	public class PendingConnectionAction
	{
		/// <summary>
		/// Gets or sets the connection action.
		/// </summary>
		public ConnectionAction Action { get; set; }

		/// <summary>
		/// Gets or sets the time when the action was initiated.
		/// </summary>
		public DateTimeOffset Time { get; set; }

		/// <summary>
		/// Gets or sets the destination endpoint information.
		/// </summary>
		public EndpointInfo Destination { get; set; }

		/// <summary>
		/// Gets or sets the pending source endpoint information.
		/// </summary>
		public EndpointInfo PendingSource { get; set; }
	}
}
