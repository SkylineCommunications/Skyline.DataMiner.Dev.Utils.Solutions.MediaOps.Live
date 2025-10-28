namespace Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

	/// <summary>
	/// Inter-app message for notifying pending connection actions.
	/// </summary>
	public class NotifyPendingConnectionActionMessage : Message
	{
		/// <summary>
		/// Gets or sets the collection of pending connection actions.
		/// </summary>
		public ICollection<PendingConnectionAction> Actions { get; set; }
	}
}
