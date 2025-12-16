namespace Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

	public class NotifyPendingConnectionActionMessage : Message
	{
		/// <summary>
		/// Gets or sets the collection of pending connection actions.
		/// </summary>
		public ICollection<PendingConnectionAction> Actions { get; set; }
	}
}
