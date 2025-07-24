namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class ConnectionsChangedEvent
	{
		public ConnectionsChangedEvent(IEnumerable<Connection> updatedConnections, IEnumerable<Connection> deletedConnections)
		{
			UpdatedConnections = (updatedConnections ?? []).ToList();
			DeletedConnections = (deletedConnections ?? []).ToList();
		}

		/// <summary>
		/// Gets the updated connections.
		/// </summary>
		public ICollection<Connection> UpdatedConnections { get; }

		/// <summary>
		/// Gets the deleted connections.
		/// </summary>
		public ICollection<Connection> DeletedConnections { get; }

		public override string ToString()
		{
			return $"{nameof(ConnectionsChangedEvent)}: {UpdatedConnections.Count} updated, {DeletedConnections.Count} deleted";
		}
	}
}
