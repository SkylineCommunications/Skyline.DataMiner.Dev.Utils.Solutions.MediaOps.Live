namespace InterApp.Messages
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

	public class NotifyConnectionChangesMessage : Message
	{
		public ICollection<ConnectionChange> Changes { get; set; }
	}
}
