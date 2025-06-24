namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class VirtualSignalGroupConnectivity
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

		public ConnectionStatus ConnectedStatus =>
			Levels.Values.All(x => x.IsConnected)
				? ConnectionStatus.Connected
				: Levels.Values.Any(x => x.IsConnected)
					? ConnectionStatus.Partial
					: ConnectionStatus.Disconnected;

		public ConnectionStatus PendingConnectedStatus =>
			Levels.Values.All(x => x.IsPendingConnected)
				? ConnectionStatus.Connected
				: Levels.Values.Any(x => x.IsPendingConnected)
					? ConnectionStatus.Partial
					: ConnectionStatus.Disconnected;

		public bool IsConnected => ConnectedSources.Count > 0 || ConnectedDestinations.Count > 0;

		public bool IsPendingConnected => PendingConnectedSources.Count > 0 || PendingConnectedDestinations.Count > 0;

		public override string ToString()
		{
			return $"{VirtualSignalGroup.Name} [{VirtualSignalGroup.Name}] - Connected: {ConnectedStatus}";
		}
	}
}
