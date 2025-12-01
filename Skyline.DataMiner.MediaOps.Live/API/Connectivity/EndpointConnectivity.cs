namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Tools;

	public sealed class EndpointConnectivity : IEquatable<EndpointConnectivity>
	{
		public EndpointConnectivity(
			Endpoint endpoint,
			IReadOnlyCollection<VirtualSignalGroup> virtualSignalGroups,
			bool isConnected,
			bool isConnecting,
			bool isDisconnecting,
			Endpoint connectedSource,
			Endpoint pendingConnectedSource,
			IReadOnlyCollection<EndpointConnection> destinationConnections)
		{
			Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
			VirtualSignalGroups = virtualSignalGroups ?? [];
			IsConnected = isConnected;
			IsConnecting = isConnecting;
			IsDisconnecting = isDisconnecting;
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
		/// Gets a value indicating whether this endpoint is connected to another endpoint.
		/// </summary>
		public bool IsConnected { get; }

		/// <summary>
		/// Gets a value indicating whether this endpoint is connecting to another endpoint.
		/// </summary>
		public bool IsConnecting { get; }

		/// <summary>
		/// Gets a value indicating whether this endpoint is disconnecting from another endpoint.
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

		public override bool Equals(object obj)
		{
			return Equals(obj as EndpointConnectivity);
		}

		public bool Equals(EndpointConnectivity other)
		{
			return other is not null &&
				   IsConnected == other.IsConnected &&
				   IsConnecting == other.IsConnecting &&
				   IsDisconnecting == other.IsDisconnecting &&
				   EqualityComparer<Endpoint>.Default.Equals(Endpoint, other.Endpoint) &&
				   EqualityComparer<Endpoint>.Default.Equals(ConnectedSource, other.ConnectedSource) &&
				   EqualityComparer<Endpoint>.Default.Equals(PendingConnectedSource, other.PendingConnectedSource) &&
				   CollectionEqualityHelper.Equals(VirtualSignalGroups, other.VirtualSignalGroups, ignoreOrder: true) &&
				   CollectionEqualityHelper.Equals(DestinationConnections, other.DestinationConnections, ignoreOrder: true);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;

				hash = (hash * 31) + IsConnected.GetHashCode();
				hash = (hash * 31) + IsConnecting.GetHashCode();
				hash = (hash * 31) + IsDisconnecting.GetHashCode();
				hash = (hash * 31) + EqualityComparer<Endpoint>.Default.GetHashCode(Endpoint);
				hash = (hash * 31) + EqualityComparer<Endpoint>.Default.GetHashCode(ConnectedSource);
				hash = (hash * 31) + EqualityComparer<Endpoint>.Default.GetHashCode(PendingConnectedSource);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(VirtualSignalGroups, ignoreOrder: true);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(DestinationConnections, ignoreOrder: true);

				return hash;
			}
		}

		public override string ToString()
		{
			return $"{Endpoint.Name} [{Endpoint.ID}] - State: {ConnectionState}";
		}

		public static bool operator ==(EndpointConnectivity left, EndpointConnectivity right)
		{
			return EqualityComparer<EndpointConnectivity>.Default.Equals(left, right);
		}

		public static bool operator !=(EndpointConnectivity left, EndpointConnectivity right)
		{
			return !(left == right);
		}
	}
}
