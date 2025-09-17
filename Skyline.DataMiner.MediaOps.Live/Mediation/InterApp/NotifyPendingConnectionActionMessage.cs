namespace Skyline.DataMiner.MediaOps.Live.Mediation.InterApp
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

	public class NotifyPendingConnectionActionMessage : Message
	{
		public ICollection<PendingConnectionAction> Actions { get; set; }
	}
}
