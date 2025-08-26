namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Tools;

	public sealed class VirtualSignalGroupConnectivity : IEquatable<VirtualSignalGroupConnectivity>
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
			Levels = levelsConnectivity ?? new Dictionary<ApiObjectReference<Level>, EndpointConnectivity>();
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

		public bool IsConnecting => Levels.Values.Any(x => x.IsConnecting);

		public bool IsDisconnecting => Levels.Values.Any(x => x.IsDisconnecting);

		public override bool Equals(object obj)
		{
			return Equals(obj as VirtualSignalGroupConnectivity);
		}

		public bool Equals(VirtualSignalGroupConnectivity other)
		{
			return other is not null &&
				   EqualityComparer<VirtualSignalGroup>.Default.Equals(VirtualSignalGroup, other.VirtualSignalGroup) &&
				   CollectionEqualityHelper.Equals(Levels, other.Levels) &&
				   CollectionEqualityHelper.Equals(ConnectedSources, other.ConnectedSources) &&
				   CollectionEqualityHelper.Equals(PendingConnectedSources, other.PendingConnectedSources) &&
				   CollectionEqualityHelper.Equals(ConnectedDestinations, other.ConnectedDestinations) &&
				   CollectionEqualityHelper.Equals(PendingConnectedDestinations, other.PendingConnectedDestinations);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;

				hash = (hash * 31) + EqualityComparer<VirtualSignalGroup>.Default.GetHashCode(VirtualSignalGroup);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(Levels);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(ConnectedSources);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(PendingConnectedSources);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(ConnectedDestinations);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(PendingConnectedDestinations);

				return hash; 
			}
		}

		public override string ToString()
		{
			return $"{VirtualSignalGroup.Name} [{VirtualSignalGroup.ID}] - State: {ConnectedState}";
		}

		public static bool operator ==(VirtualSignalGroupConnectivity left, VirtualSignalGroupConnectivity right)
		{
			return EqualityComparer<VirtualSignalGroupConnectivity>.Default.Equals(left, right);
		}

		public static bool operator !=(VirtualSignalGroupConnectivity left, VirtualSignalGroupConnectivity right)
		{
			return !(left == right);
		}
	}
}
