namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public sealed class PendingConnectionAction
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

		public override string ToString()
		{
			return $"{DestinationName} ({Action})";
		}
	}
}
