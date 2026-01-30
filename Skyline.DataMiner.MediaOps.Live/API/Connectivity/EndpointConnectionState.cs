namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Connectivity
{
	public enum EndpointConnectionState
	{
		/// <summary>
		/// The endpoint is in the process of connecting to another endpoint.
		/// </summary>
		Connecting,

		/// <summary>
		/// The endpoint is connected to another endpoint.
		/// </summary>
		Connected,

		/// <summary>
		/// The endpoint is in the process of disconnecting from another endpoint.
		/// </summary>
		Disconnecting,

		/// <summary>
		/// The endpoint is not connected to any other endpoint.
		/// </summary>
		Disconnected,
	}
}
