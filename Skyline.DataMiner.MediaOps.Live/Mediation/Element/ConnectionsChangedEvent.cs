namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class ConnectionsChangedEvent
	{
		public ConnectionsChangedEvent(IEnumerable<Connection> updatedConnections, IEnumerable<Guid> deletedConnections)
		{
			UpdatedConnections = (updatedConnections ?? []).ToList();
			DeletedConnections = (deletedConnections ?? []).ToList();
		}

		/// <summary>
		/// Gets the updated connections.
		/// </summary>
		public ICollection<Connection> UpdatedConnections { get; }

		/// <summary>
		/// Gets the IDs of the deleted connections.
		/// The IDs correspond to the destination endpoint IDs.
		/// </summary>
		public ICollection<Guid> DeletedConnections { get; }

		public override string ToString()
		{
			return $"{nameof(ConnectionsChangedEvent)}: {UpdatedConnections.Count} updated, {DeletedConnections.Count} deleted";
		}
	}
}
