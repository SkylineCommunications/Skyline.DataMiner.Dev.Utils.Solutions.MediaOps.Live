namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public sealed class Connection : IEquatable<Connection>
	{
		internal Connection(MediationElement mediationElement, object[] row)
		{
			if (mediationElement is null)
			{
				throw new ArgumentNullException(nameof(mediationElement));
			}

			if (row is null)
			{
				throw new ArgumentNullException(nameof(row));
			}

			MediationElement = mediationElement;

			Guid.TryParse(Convert.ToString(row[0]), out var destinationId);
			Destination = destinationId;

			DestinationName = Convert.ToString(row[1]);

			IsConnected = Convert.ToInt32(row[2]) == 1;

			var connectedSourceIdValue = Convert.ToString(row[3]);
			if (!string.IsNullOrWhiteSpace(connectedSourceIdValue) &&
				Guid.TryParse(connectedSourceIdValue, out var parsedPendingSourceId))
			{
				ConnectedSource = parsedPendingSourceId;
			}

			var connectedSourceNameValue = Convert.ToString(row[4]);
			if (!string.IsNullOrWhiteSpace(connectedSourceNameValue))
			{
				ConnectedSourceName = connectedSourceNameValue;
			}
		}

		internal MediationElement MediationElement { get; }

		public ApiObjectReference<Endpoint> Destination { get; }

		public string DestinationName { get; }

		public ApiObjectReference<Endpoint>? ConnectedSource { get; }

		public string ConnectedSourceName { get; }

		public bool IsConnected { get; }

		public IEnumerable<ApiObjectReference<Endpoint>> GetEndpoints()
		{
			if (Destination != ApiObjectReference<Endpoint>.Empty)
			{
				yield return Destination;
			}

			if (ConnectedSource.HasValue && ConnectedSource != ApiObjectReference<Endpoint>.Empty)
			{
				yield return ConnectedSource.Value;
			}
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as Connection);
		}

		public bool Equals(Connection other)
		{
			if (other is null)
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return MediationElement == other.MediationElement &&
				   Destination == other.Destination &&
				   DestinationName == other.DestinationName &&
				   ConnectedSource == other.ConnectedSource &&
				   ConnectedSourceName == other.ConnectedSourceName &&
				   IsConnected == other.IsConnected;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = 17;
				hashCode = (hashCode * 23) + MediationElement.GetHashCode();
				hashCode = (hashCode * 23) + Destination.GetHashCode();
				hashCode = (hashCode * 23) + (DestinationName?.GetHashCode() ?? 0);
				hashCode = (hashCode * 23) + (ConnectedSource?.GetHashCode() ?? 0);
				hashCode = (hashCode * 23) + (ConnectedSourceName?.GetHashCode() ?? 0);
				hashCode = (hashCode * 23) + IsConnected.GetHashCode();
				return hashCode;
			}
		}

		public override string ToString()
		{
			if (IsConnected)
			{
				if (!string.IsNullOrWhiteSpace(ConnectedSourceName))
				{
					return $"{DestinationName} => {ConnectedSourceName} [Connected]";
				}

				return $"{DestinationName} [Connected]";
			}

			return $"{DestinationName} [Disconnected]";
		}

		public static bool operator ==(Connection left, Connection right)
		{
			return EqualityComparer<Connection>.Default.Equals(left, right);
		}

		public static bool operator !=(Connection left, Connection right)
		{
			return !(left == right);
		}
	}
}
