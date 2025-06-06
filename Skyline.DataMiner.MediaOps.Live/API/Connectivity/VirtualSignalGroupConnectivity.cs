namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;

	public class VirtualSignalGroupConnectivity
	{
		public VirtualSignalGroupConnectivity(
			VirtualSignalGroup virtualSignalGroup,
			ConnectionStatus connectedStatus,
			ConnectionStatus pendingConnectedStatus,
			IReadOnlyCollection<VirtualSignalGroup> connectedSources,
			IReadOnlyCollection<VirtualSignalGroup> pendingConnectedSources,
			IReadOnlyCollection<VirtualSignalGroup> connectedDestinations,
			IReadOnlyCollection<VirtualSignalGroup> pendingConnectedDestinations)
		{
			VirtualSignalGroup = virtualSignalGroup ?? throw new ArgumentNullException(nameof(virtualSignalGroup));
			ConnectedStatus = connectedStatus;
			PendingConnectedStatus = pendingConnectedStatus;
			ConnectedSources = connectedSources ?? [];
			PendingConnectedSources = pendingConnectedSources ?? [];
			ConnectedDestinations = connectedDestinations ?? [];
			PendingConnectedDestinations = pendingConnectedDestinations ?? [];
		}

		public VirtualSignalGroup VirtualSignalGroup { get; }

		public ConnectionStatus ConnectedStatus { get; }

		public ConnectionStatus PendingConnectedStatus { get; }

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

		public bool IsConnected => ConnectedSources.Count > 0 || ConnectedDestinations.Count > 0;

		public bool IsPendingConnected => PendingConnectedSources.Count > 0 || PendingConnectedDestinations.Count > 0;

		public override string ToString()
		{
			return $"{VirtualSignalGroup.ID} - Connected: {ConnectedStatus}";
		}
	}
}
