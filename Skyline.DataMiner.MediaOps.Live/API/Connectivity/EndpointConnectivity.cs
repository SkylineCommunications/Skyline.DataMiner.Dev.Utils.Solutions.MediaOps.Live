namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;

	public class EndpointConnectivity
	{
		public EndpointConnectivity(
			Endpoint connectedSource,
			Endpoint pendingConnectedSource,
			IReadOnlyCollection<Endpoint> connectedDestinations,
			IReadOnlyCollection<Endpoint> pendingConnectedDestinations)
		{
			ConnectedSource = connectedSource;
			PendingConnectedSource = pendingConnectedSource;
			ConnectedDestinations = connectedDestinations ?? Array.Empty<Endpoint>();
			PendingConnectedDestinations = pendingConnectedDestinations ?? Array.Empty<Endpoint>();
		}

		public bool IsConnected => ConnectedSource != null || ConnectedDestinations.Count > 0;

		public bool IsPendingConnected => PendingConnectedSource != null || PendingConnectedDestinations.Count > 0;

		/// <summary>
		/// Gets the endpoint that this endpoint is connected to as a source.
		/// Null if not connected.
		/// </summary>
		public Endpoint ConnectedSource { get; }

		/// <summary>
		/// Gets the endpoint that this endpoint is pending connected to as a source.
		/// Null if not connected.
		/// </summary>
		public Endpoint PendingConnectedSource { get; }

		/// <summary>
		/// Gets the destinations this endpoint is connected to.
		/// Empty if none.
		/// </summary>
		public IReadOnlyCollection<Endpoint> ConnectedDestinations { get; }

		/// <summary>
		/// Gets the destinations this endpoint is pending connected to.
		/// Empty if none.
		/// </summary>
		public IReadOnlyCollection<Endpoint> PendingConnectedDestinations { get; }
	}
}
