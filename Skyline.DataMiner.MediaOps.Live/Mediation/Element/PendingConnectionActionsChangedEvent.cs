namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class PendingConnectionActionsChangedEvent
	{
		public PendingConnectionActionsChangedEvent(IEnumerable<PendingConnectionAction> updatedPendingActions, IEnumerable<PendingConnectionAction> deletedPendingActions)
		{
			UpdatedPendingActions = (updatedPendingActions ?? []).ToList();
			DeletedPendingActions = (deletedPendingActions ?? []).ToList();
		}

		/// <summary>
		/// Gets the updated pending connection actions.
		/// </summary>
		public ICollection<PendingConnectionAction> UpdatedPendingActions { get; }

		/// <summary>
		/// Gets the deleted pending connection actions.
		/// </summary>
		public ICollection<PendingConnectionAction> DeletedPendingActions { get; }

		public override string ToString()
		{
			return $"{nameof(PendingConnectionActionsChangedEvent)}: {UpdatedPendingActions.Count} updated, {DeletedPendingActions.Count} deleted";
		}
	}
}
