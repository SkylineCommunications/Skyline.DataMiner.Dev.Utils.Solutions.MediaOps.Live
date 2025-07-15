namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;

	public sealed class ConnectionInfo
	{
		public Guid DestinationId { get; set; }

		public bool IsConnected { get; set; }

		public Guid? ConnectedSource { get; set; }
	}
}
