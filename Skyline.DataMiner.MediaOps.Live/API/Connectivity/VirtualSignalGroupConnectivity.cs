namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Tools;

	public sealed class VirtualSignalGroupConnectivity : IEquatable<VirtualSignalGroupConnectivity>
	{
		public VirtualSignalGroupConnectivity(
			VirtualSignalGroup virtualSignalGroup,
			IReadOnlyDictionary<Level, EndpointConnectivity> levelsConnectivity,
			IReadOnlyCollection<VirtualSignalGroup> connectedSources,
			IReadOnlyCollection<VirtualSignalGroup> pendingConnectedSources,
			IReadOnlyCollection<VirtualSignalGroup> connectedDestinations,
			IReadOnlyCollection<VirtualSignalGroup> pendingConnectedDestinations,
			IReadOnlyCollection<string> warnings)
		{
			VirtualSignalGroup = virtualSignalGroup ?? throw new ArgumentNullException(nameof(virtualSignalGroup));
			Levels = levelsConnectivity ?? new Dictionary<Level, EndpointConnectivity>();
			ConnectedSources = connectedSources ?? [];
			PendingConnectedSources = pendingConnectedSources ?? [];
			ConnectedDestinations = connectedDestinations ?? [];
			PendingConnectedDestinations = pendingConnectedDestinations ?? [];
			Warnings = warnings ?? [];
		}

		/// <summary>
		/// Gets the virtual signal group this connectivity information is for.
		/// </summary>
		public VirtualSignalGroup VirtualSignalGroup { get; }

		/// <summary>
		/// Gets the connectivity information for each level.
		/// </summary>
		public IReadOnlyDictionary<Level, EndpointConnectivity> Levels { get; }

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

		/// <summary>
		/// Gets the warnings that occurred while building the connectivity information.
		/// For example, when an endpoint or level referenced by the virtual signal group could not be found.
		/// Empty if there are no warnings.
		/// </summary>
		public IReadOnlyCollection<string> Warnings { get; }

		/// <summary>
		/// Gets a value indicating whether there are any warnings.
		/// </summary>
		public bool HasWarnings => Warnings.Count > 0;

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
				   CollectionEqualityHelper.Equals(PendingConnectedDestinations, other.PendingConnectedDestinations) &&
				   CollectionEqualityHelper.Equals(Warnings, other.Warnings);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;

				hash = (hash * 31) + EqualityComparer<VirtualSignalGroup>.Default.GetHashCode(VirtualSignalGroup);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(Levels, ignoreOrder: true);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(ConnectedSources, ignoreOrder: true);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(PendingConnectedSources, ignoreOrder: true);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(ConnectedDestinations, ignoreOrder: true);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(PendingConnectedDestinations, ignoreOrder: true);
				hash = (hash * 31) + CollectionEqualityHelper.GetHashCode(Warnings, ignoreOrder: true);

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
