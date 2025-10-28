namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data;

	/// <summary>
	/// Input data for creating connections.
	/// </summary>
	public class CreateConnectionsInputData : IConnectionHandlerInputData
	{
		/// <summary>
		/// Gets or sets the collection of connections to create.
		/// </summary>
		public ICollection<ConnectionInfo> Connections { get; set; }
	}
}
