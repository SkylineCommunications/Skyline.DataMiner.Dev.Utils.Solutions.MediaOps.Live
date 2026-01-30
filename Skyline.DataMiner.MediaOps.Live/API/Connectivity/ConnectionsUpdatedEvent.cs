namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Connectivity
{
	using System.Collections.Generic;
	using System.Linq;

	public sealed class ConnectionsUpdatedEvent
	{
		public ConnectionsUpdatedEvent(
			IEnumerable<EndpointConnectivity> endpoints,
			IEnumerable<VirtualSignalGroupConnectivity> virtualSignalGroups)
		{
			Endpoints = (endpoints ?? []).ToList();
			VirtualSignalGroups = (virtualSignalGroups ?? []).ToList();
		}

		public IReadOnlyCollection<EndpointConnectivity> Endpoints { get; }

		public IReadOnlyCollection<VirtualSignalGroupConnectivity> VirtualSignalGroups { get; }

		public override string ToString()
		{
			return $"{nameof(ConnectionsUpdatedEvent)}: {Endpoints.Count} endpoints, {VirtualSignalGroups.Count} VSGs";
		}
	}
}
