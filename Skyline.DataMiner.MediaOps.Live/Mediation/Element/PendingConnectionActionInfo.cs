namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages;

	public sealed class PendingConnectionActionInfo
	{
		public Guid DestinationId { get; set; }

		public ConnectionAction Action { get; set; }

		public Guid? PendingSourceId { get; set; }
	}
}
