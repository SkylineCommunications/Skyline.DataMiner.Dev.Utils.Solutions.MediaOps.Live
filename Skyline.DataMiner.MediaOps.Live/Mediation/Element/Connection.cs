namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Represents a connection between endpoints in the mediation layer.
	/// </summary>
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
			if (!String.IsNullOrWhiteSpace(connectedSourceIdValue) &&
				Guid.TryParse(connectedSourceIdValue, out var parsedPendingSourceId))
			{
				ConnectedSource = parsedPendingSourceId;
			}

			var connectedSourceNameValue = Convert.ToString(row[4]);
			if (!String.IsNullOrWhiteSpace(connectedSourceNameValue))
			{
				ConnectedSourceName = connectedSourceNameValue;
			}
		}

		/// <summary>
		/// Gets the destination endpoint reference.
		/// </summary>
		public ApiObjectReference<Endpoint> Destination { get; }

		/// <summary>
		/// Gets the destination endpoint name.
		/// </summary>
		public string DestinationName { get; }

		/// <summary>
		/// Gets the connected source endpoint reference, if connected.
		/// </summary>
		public ApiObjectReference<Endpoint>? ConnectedSource { get; }

		/// <summary>
		/// Gets the connected source endpoint name, if connected.
		/// </summary>
		public string ConnectedSourceName { get; }

		/// <summary>
		/// Gets a value indicating whether the endpoint is currently connected.
		/// </summary>
		public bool IsConnected { get; }

		/// <summary>
		/// Gets all endpoint references involved in this connection.
		/// </summary>
		/// <returns>An enumerable of endpoint references.</returns>
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

		/// <summary>
		/// Determines whether the specified object is equal to the current connection.
		/// </summary>
		/// <param name="obj">The object to compare with the current connection.</param>
		/// <returns>true if the specified object is equal to the current connection; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			return Equals(obj as Connection);
		}

		/// <summary>
		/// Determines whether the specified connection is equal to the current connection.
		/// </summary>
		/// <param name="other">The connection to compare with the current connection.</param>
		/// <returns>true if the specified connection is equal to the current connection; otherwise, false.</returns>
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

		/// <summary>
		/// Serves as the default hash function.
		/// </summary>
		/// <returns>A hash code for the current connection.</returns>
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

		/// <summary>
		/// Returns a string that represents the current connection.
		/// </summary>
		/// <returns>A string that represents the current connection.</returns>
		public override string ToString()
		{
			if (IsConnected)
			{
				if (!String.IsNullOrWhiteSpace(ConnectedSourceName))
				{
					return $"{DestinationName} => {ConnectedSourceName} [Connected]";
				}

				return $"{DestinationName} [Connected]";
			}

			return $"{DestinationName} [Disconnected]";
		}

		/// <summary>
		/// Determines whether two connection instances are equal.
		/// </summary>
		/// <param name="left">The first connection to compare.</param>
		/// <param name="right">The second connection to compare.</param>
		/// <returns>true if the connections are equal; otherwise, false.</returns>
		public static bool operator ==(Connection left, Connection right)
		{
			return EqualityComparer<Connection>.Default.Equals(left, right);
		}

		/// <summary>
		/// Determines whether two connection instances are not equal.
		/// </summary>
		/// <param name="left">The first connection to compare.</param>
		/// <param name="right">The second connection to compare.</param>
		/// <returns>true if the connections are not equal; otherwise, false.</returns>
		public static bool operator !=(Connection left, Connection right)
		{
			return !(left == right);
		}
	}
}
