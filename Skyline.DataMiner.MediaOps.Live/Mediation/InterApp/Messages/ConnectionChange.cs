namespace Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages
{
	using System;

	/// <summary>
	/// Represents a connection change notification for inter-app messaging.
	/// </summary>
	public class ConnectionChange
	{
		/// <summary>
		/// Gets or sets the time when the change occurred.
		/// </summary>
		public DateTimeOffset Time { get; set; }

		/// <summary>
		/// Gets or sets the destination endpoint information.
		/// </summary>
		public EndpointInfo Destination { get; set; }

		/// <summary>
		/// Gets or sets the connected source endpoint information.
		/// </summary>
		public EndpointInfo ConnectedSource { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the endpoint is currently connected.
		/// </summary>
		public bool IsConnected { get; set; }
	}
}
