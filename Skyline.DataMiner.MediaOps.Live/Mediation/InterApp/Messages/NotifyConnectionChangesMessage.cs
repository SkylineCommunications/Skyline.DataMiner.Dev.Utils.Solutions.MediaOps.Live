namespace Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.InterApp.Messages
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

	public class NotifyConnectionChangesMessage : Message
	{
		/// <summary>
		/// Gets or sets the name of the connection handler script that sends the message.
		/// </summary>
		public string ConnectionHandlerScript { get; set; }

		public ICollection<ConnectionChange> Changes { get; set; }
	}
}
