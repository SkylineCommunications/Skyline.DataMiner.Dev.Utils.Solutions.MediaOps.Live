namespace InterApp.Messages
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

	public class ClearPendingConnectionActionMessage : Message
	{
		public ICollection<ClearPendingConnectionActionRequest> Requests { get; set; }
	}
}
