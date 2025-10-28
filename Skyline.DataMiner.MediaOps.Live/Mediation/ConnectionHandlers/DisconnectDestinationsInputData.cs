namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data;

	/// <summary>
	/// Input data for disconnecting destinations.
	/// </summary>
	public class DisconnectDestinationsInputData : IConnectionHandlerInputData
	{
		/// <summary>
		/// Gets or sets the collection of destinations to disconnect.
		/// </summary>
		public ICollection<EndpointInfo> Destinations { get; set; }
	}
}
