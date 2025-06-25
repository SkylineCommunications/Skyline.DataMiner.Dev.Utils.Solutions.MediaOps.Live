namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public sealed class EndpointConnectivity
	{
		public EndpointConnectivity(
			Endpoint endpoint,
			IReadOnlyCollection<VirtualSignalGroup> virtualSignalGroups,
			EndpointConnection connectedSource,
			Endpoint pendingConnectedSource,
			IReadOnlyCollection<EndpointConnection> destinationConnections)
		{
			Endpoint = endpoint;
			VirtualSignalGroups = virtualSignalGroups ?? [];
			ConnectedSource = connectedSource;
			PendingConnectedSource = pendingConnectedSource;
			DestinationConnections = destinationConnections ?? [];
		}

		/// <summary>
		/// Gets the endpoint this connectivity information is for.
		/// </summary>
		public Endpoint Endpoint { get; }

		/// <summary>
		/// Gets the virtual signal groups that contain this endpoint.
		/// </summary>
		public IReadOnlyCollection<VirtualSignalGroup> VirtualSignalGroups { get; }

		/// <summary>
		/// Gets the endpoint that this endpoint is connected to as a source.
		/// Null if not connected.
		/// </summary>
		public EndpointConnection ConnectedSource { get; }

		/// <summary>
		/// Gets the pending endpoint that this endpoint is connecting to as a source.
		/// Null if not connected.
		/// </summary>
		public Endpoint PendingConnectedSource { get; }

		/// <summary>
		/// Gets the connections to destinations this endpoint is connected to.
		/// Empty if none.
		/// </summary>
		public IReadOnlyCollection<EndpointConnection> DestinationConnections { get; }

		/// <summary>
		/// Gets the destinations this endpoint is connected to.
		/// Empty if none.
		/// </summary>
		public IEnumerable<Endpoint> ConnectedDestinations => DestinationConnections
			.Where(x => x.State == EndpointConnectionState.Connected)
			.Select(x => x.Endpoint);

		/// <summary>
		/// Gets the pending destinations this endpoint is connecting to.
		/// Empty if none.
		/// </summary>
		public IEnumerable<Endpoint> PendingConnectedDestinations => DestinationConnections
			.Where(x => x.State == EndpointConnectionState.Connecting)
			.Select(x => x.Endpoint);

		/// <summary>
		/// Gets the destinations this endpoint is disconnecting from.
		/// Empty if none.
		/// </summary>
		public IEnumerable<Endpoint> DisconnectingDestinations => DestinationConnections
			.Where(x => x.State == EndpointConnectionState.Disconnecting)
			.Select(x => x.Endpoint);

		public bool IsConnected =>
			ConnectedSource != null ||
			DestinationConnections.Any(c => c.State == EndpointConnectionState.Connected);

		public bool IsPendingConnected =>
			PendingConnectedSource != null ||
			DestinationConnections.Any(c => c.State == EndpointConnectionState.Connecting);

		public bool IsDisconnecting =>
			(ConnectedSource != null && ConnectedSource.State == EndpointConnectionState.Disconnecting) ||
			DestinationConnections.Any(c => c.State == EndpointConnectionState.Disconnecting);

		public override string ToString()
		{
			return $"{Endpoint.Name} [{Endpoint.ID}] - Connected: {IsConnected}";
		}
	}
}
