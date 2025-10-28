namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Represents a pending connection action in the mediation layer.
	/// </summary>
	public sealed class PendingConnectionAction : IEquatable<PendingConnectionAction>
	{
		internal PendingConnectionAction(MediationElement mediationElement, object[] row)
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

			Enum.TryParse<PendingConnectionActionType>(Convert.ToString(row[2]), out var action);
			Action = action;

			var timeValue = Convert.ToDouble(row[3]);
			Time = DateTime.FromOADate(timeValue);

			var pendingSourceIdValue = Convert.ToString(row[4]);
			if (!String.IsNullOrWhiteSpace(pendingSourceIdValue) &&
				Guid.TryParse(pendingSourceIdValue, out var parsedPendingSourceId))
			{
				PendingSource = parsedPendingSourceId;
			}

			var pendingSourceNameValue = Convert.ToString(row[5]);
			if (!String.IsNullOrWhiteSpace(pendingSourceNameValue))
			{
				PendingSourceName = pendingSourceNameValue;
			}
		}

		/// <summary>
		/// Gets the pending connection action type.
		/// </summary>
		public PendingConnectionActionType Action { get; }

		/// <summary>
		/// Gets the time when the action was initiated.
		/// </summary>
		public DateTime Time { get; }

		/// <summary>
		/// Gets the destination endpoint reference.
		/// </summary>
		public ApiObjectReference<Endpoint> Destination { get; }

		/// <summary>
		/// Gets the destination endpoint name.
		/// </summary>
		public string DestinationName { get; }

		/// <summary>
		/// Gets the pending source endpoint reference, if applicable.
		/// </summary>
		public ApiObjectReference<Endpoint>? PendingSource { get; }

		/// <summary>
		/// Gets the pending source endpoint name, if applicable.
		/// </summary>
		public string PendingSourceName { get; }

		/// <summary>
		/// Gets all endpoint references involved in this pending action.
		/// </summary>
		/// <returns>An enumerable of endpoint references.</returns>
		public IEnumerable<ApiObjectReference<Endpoint>> GetEndpoints()
		{
			if (Destination != ApiObjectReference<Endpoint>.Empty)
			{
				yield return Destination;
			}

			if (PendingSource.HasValue && PendingSource != ApiObjectReference<Endpoint>.Empty)
			{
				yield return PendingSource.Value;
			}
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current pending connection action.
		/// </summary>
		/// <param name="obj">The object to compare with the current pending connection action.</param>
		/// <returns>true if the specified object is equal to the current pending connection action; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			return Equals(obj as PendingConnectionAction);
		}

		/// <summary>
		/// Determines whether the specified pending connection action is equal to the current pending connection action.
		/// </summary>
		/// <param name="other">The pending connection action to compare with the current pending connection action.</param>
		/// <returns>true if the specified pending connection action is equal to the current pending connection action; otherwise, false.</returns>
		public bool Equals(PendingConnectionAction other)
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
				   Action == other.Action &&
				   Time == other.Time &&
				   Destination == other.Destination &&
				   DestinationName == other.DestinationName &&
				   PendingSource == other.PendingSource &&
				   PendingSourceName == other.PendingSourceName;
		}

		/// <summary>
		/// Serves as the default hash function.
		/// </summary>
		/// <returns>A hash code for the current pending connection action.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = 17;
				hashCode = (hashCode * 23) + MediationElement.GetHashCode();
				hashCode = (hashCode * 23) + Action.GetHashCode();
				hashCode = (hashCode * 23) + Time.GetHashCode();
				hashCode = (hashCode * 23) + Destination.GetHashCode();
				hashCode = (hashCode * 23) + (DestinationName?.GetHashCode() ?? 0);
				hashCode = (hashCode * 23) + (PendingSource?.GetHashCode() ?? 0);
				hashCode = (hashCode * 23) + (PendingSourceName?.GetHashCode() ?? 0);
				return hashCode;
			}
		}

		/// <summary>
		/// Returns a string that represents the current pending connection action.
		/// </summary>
		/// <returns>A string that represents the current pending connection action.</returns>
		public override string ToString()
		{
			return $"{DestinationName} ({Action})";
		}

		/// <summary>
		/// Determines whether two pending connection action instances are equal.
		/// </summary>
		/// <param name="left">The first pending connection action to compare.</param>
		/// <param name="right">The second pending connection action to compare.</param>
		/// <returns>true if the pending connection actions are equal; otherwise, false.</returns>
		public static bool operator ==(PendingConnectionAction left, PendingConnectionAction right)
		{
			return EqualityComparer<PendingConnectionAction>.Default.Equals(left, right);
		}

		/// <summary>
		/// Determines whether two pending connection action instances are not equal.
		/// </summary>
		/// <param name="left">The first pending connection action to compare.</param>
		/// <param name="right">The second pending connection action to compare.</param>
		/// <returns>true if the pending connection actions are not equal; otherwise, false.</returns>
		public static bool operator !=(PendingConnectionAction left, PendingConnectionAction right)
		{
			return !(left == right);
		}
	}
}
