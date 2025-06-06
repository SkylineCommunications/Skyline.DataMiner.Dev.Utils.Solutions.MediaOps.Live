namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	public enum ConnectionStatus
	{
		/// <summary>
		/// None of the endpoints are connected.
		/// </summary>
		Disconnected,

		/// <summary>
		/// Some, but not all, endpoints are connected.
		/// </summary>
		Partial,

		/// <summary>
		/// All endpoints are connected.
		/// </summary>
		Connected,
	}
}
