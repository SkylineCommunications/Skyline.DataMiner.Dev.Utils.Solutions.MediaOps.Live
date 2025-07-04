namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class PendingConnectionActionsChangedEvent
	{
		public PendingConnectionActionsChangedEvent(IEnumerable<PendingConnectionAction> updatedPendingActions, IEnumerable<Guid> deletedPendingActions)
		{
			UpdatedPendingActions = (updatedPendingActions ?? []).ToList();
			DeletedPendingActions = (deletedPendingActions ?? []).ToList();
		}

		/// <summary>
		/// Gets the updated pending connection actions.
		/// </summary>
		public ICollection<PendingConnectionAction> UpdatedPendingActions { get; }

		/// <summary>
		/// Gets the IDs of the deleted pending connection actions.
		/// The IDs correspond to the destination endpoint IDs.
		/// </summary>
		public ICollection<Guid> DeletedPendingActions { get; }
	}
}
