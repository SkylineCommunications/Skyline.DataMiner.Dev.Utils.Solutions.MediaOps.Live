namespace Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages
{
	using System;

	/// <summary>
	/// Represents a connection change event.
	/// </summary>
	public class ConnectionChange
	{
		/// <summary>
		/// Gets or sets the timestamp of the connection change.
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
		/// Gets or sets a value indicating whether the connection is established.
		/// </summary>
		public bool IsConnected { get; set; }
	}
}
