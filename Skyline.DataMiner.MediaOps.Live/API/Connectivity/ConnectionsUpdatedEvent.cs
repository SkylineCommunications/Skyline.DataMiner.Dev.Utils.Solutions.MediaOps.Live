namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;

	public sealed class ConnectionsUpdatedEvent
	{
		public ConnectionsUpdatedEvent(
			IReadOnlyCollection<EndpointConnectivity> endpoints,
			IReadOnlyCollection<VirtualSignalGroupConnectivity> virtualSignalGroups)
		{
			Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
			VirtualSignalGroups = virtualSignalGroups ?? throw new ArgumentNullException(nameof(virtualSignalGroups));
		}

		public IReadOnlyCollection<EndpointConnectivity> Endpoints { get; }

		public IReadOnlyCollection<VirtualSignalGroupConnectivity> VirtualSignalGroups { get; }
	}
}
