namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.InterApp.Messages;

	public sealed class PendingConnectionAction : IEquatable<PendingConnectionAction>
	{
		internal PendingConnectionAction(object[] row)
		{
			if (row is null)
			{
				throw new ArgumentNullException(nameof(row));
			}

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

		internal PendingConnectionAction(ApiObjectReference<Endpoint> destination, ConnectionAction connectionAction, ApiObjectReference<Endpoint>? pendingSource)
		{
			Destination = destination;
			Action = (PendingConnectionActionType)connectionAction;
			PendingSource = pendingSource;
		}

		public PendingConnectionActionType Action { get; }

		public DateTime Time { get; }

		public ApiObjectReference<Endpoint> Destination { get; }

		public string DestinationName { get; }

		public ApiObjectReference<Endpoint>? PendingSource { get; }

		public string PendingSourceName { get; }

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

		public override bool Equals(object obj)
		{
			return Equals(obj as PendingConnectionAction);
		}

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

			return Action == other.Action &&
				   Time == other.Time &&
				   Destination == other.Destination &&
				   DestinationName == other.DestinationName &&
				   PendingSource == other.PendingSource &&
				   PendingSourceName == other.PendingSourceName;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = 17;
				hashCode = (hashCode * 23) + Action.GetHashCode();
				hashCode = (hashCode * 23) + Time.GetHashCode();
				hashCode = (hashCode * 23) + Destination.GetHashCode();
				hashCode = (hashCode * 23) + (DestinationName?.GetHashCode() ?? 0);
				hashCode = (hashCode * 23) + (PendingSource?.GetHashCode() ?? 0);
				hashCode = (hashCode * 23) + (PendingSourceName?.GetHashCode() ?? 0);
				return hashCode;
			}
		}

		public override string ToString()
		{
			return $"{DestinationName} ({Action})";
		}

		public static bool operator ==(PendingConnectionAction left, PendingConnectionAction right)
		{
			return EqualityComparer<PendingConnectionAction>.Default.Equals(left, right);
		}

		public static bool operator !=(PendingConnectionAction left, PendingConnectionAction right)
		{
			return !(left == right);
		}
	}
}
