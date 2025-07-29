namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public sealed class EndpointConnectivity
	{
		public EndpointConnectivity(
			Endpoint endpoint,
			bool isConnected,
			bool isConnecting,
			bool isDisconnecting,
			Endpoint connectedSource,
			Endpoint pendingConnectedSource,
			IReadOnlyCollection<VirtualSignalGroup> virtualSignalGroups,
			IReadOnlyCollection<EndpointConnection> destinationConnections)
		{
			Endpoint = endpoint;
			IsConnected = isConnected;
			IsConnecting = isConnecting;
			IsDisconnecting = isDisconnecting;
			ConnectedSource = connectedSource;
			PendingConnectedSource = pendingConnectedSource;
			VirtualSignalGroups = virtualSignalGroups ?? [];
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
		/// Gets whether this endpoint is connected to another endpoint.
		/// </summary>
		public bool IsConnected { get; }

		/// <summary>
		/// Gets whether this endpoint is connecting to another endpoint.
		/// </summary>
		public bool IsConnecting { get; }

		/// <summary>
		/// Gets whether this endpoint is disconnecting from another endpoint.
		/// </summary>
		public bool IsDisconnecting { get; }

		/// <summary>
		/// Gets the endpoint that this endpoint is connected to as a source.
		/// Null if not connected.
		/// </summary>
		public Endpoint ConnectedSource { get; }

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

		/// <summary>
		/// Gets the connection state of this endpoint.
		/// </summary>
		public EndpointConnectionState ConnectionState =>
			IsConnecting ? EndpointConnectionState.Connecting :
			IsDisconnecting ? EndpointConnectionState.Disconnecting :
			IsConnected ? EndpointConnectionState.Connected :
			EndpointConnectionState.Disconnected;

		public override string ToString()
		{
			return $"{Endpoint.Name} [{Endpoint.ID}] - State: {ConnectionState}";
		}
	}
}
