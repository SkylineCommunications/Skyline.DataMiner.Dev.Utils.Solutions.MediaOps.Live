namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public sealed class VirtualSignalGroupConnectivity
	{
		public VirtualSignalGroupConnectivity(
			VirtualSignalGroup virtualSignalGroup,
			IReadOnlyDictionary<ApiObjectReference<Level>, EndpointConnectivity> levelsConnectivity,
			IReadOnlyCollection<VirtualSignalGroup> connectedSources,
			IReadOnlyCollection<VirtualSignalGroup> pendingConnectedSources,
			IReadOnlyCollection<VirtualSignalGroup> connectedDestinations,
			IReadOnlyCollection<VirtualSignalGroup> pendingConnectedDestinations)
		{
			VirtualSignalGroup = virtualSignalGroup ?? throw new ArgumentNullException(nameof(virtualSignalGroup));
			Levels = levelsConnectivity ?? throw new ArgumentNullException(nameof(levelsConnectivity));
			ConnectedSources = connectedSources ?? [];
			PendingConnectedSources = pendingConnectedSources ?? [];
			ConnectedDestinations = connectedDestinations ?? [];
			PendingConnectedDestinations = pendingConnectedDestinations ?? [];
		}

		/// <summary>
		/// Gets the virtual signal group this connectivity information is for.
		/// </summary>
		public VirtualSignalGroup VirtualSignalGroup { get; }

		/// <summary>
		/// Gets the connectivity information for each level.
		/// </summary>
		public IReadOnlyDictionary<ApiObjectReference<Level>, EndpointConnectivity> Levels { get; }

		/// <summary>
		/// Gets the sources this virtual signal group is connected to.
		/// Empty if none.
		/// </summary>
		public IReadOnlyCollection<VirtualSignalGroup> ConnectedSources { get; }

		/// <summary>
		/// Gets the pending sources this virtual signal group is connecting to.
		/// Empty if none.
		/// </summary>
		public IReadOnlyCollection<VirtualSignalGroup> PendingConnectedSources { get; }

		/// <summary>
		/// Gets the destinations this virtual signal group is connected to.
		/// Empty if none.
		/// </summary>
		public IReadOnlyCollection<VirtualSignalGroup> ConnectedDestinations { get; }

		/// <summary>
		/// Gets the pending destinations this virtual signal group is connecting to.
		/// Empty if none.
		/// </summary>
		public IReadOnlyCollection<VirtualSignalGroup> PendingConnectedDestinations { get; }

		public ConnectionState ConnectedState =>
			Levels.Values.All(x => x.IsConnected)
				? ConnectionState.Connected
				: Levels.Values.Any(x => x.IsConnected)
					? ConnectionState.Partial
					: ConnectionState.Disconnected;

		public bool IsConnected => Levels.Values.Any(x => x.IsConnected);

		public bool IsPendingConnected => Levels.Values.Any(x => x.IsConnecting);

		public bool IsDisconnecting => Levels.Values.Any(x => x.IsDisconnecting);

		public override string ToString()
		{
			return $"{VirtualSignalGroup.Name} [{VirtualSignalGroup.ID}] - State: {ConnectedState}";
		}
	}
}
