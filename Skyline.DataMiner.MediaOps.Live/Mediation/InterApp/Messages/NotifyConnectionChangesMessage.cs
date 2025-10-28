namespace Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

	/// <summary>
	/// Inter-app message for notifying connection changes.
	/// </summary>
	public class NotifyConnectionChangesMessage : Message
	{
		/// <summary>
		/// Gets or sets the collection of connection changes.
		/// </summary>
		public ICollection<ConnectionChange> Changes { get; set; }
	}
}
