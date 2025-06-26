namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	internal sealed class PendingConnectionAction
	{
		public PendingConnectionAction(object[] row)
		{
			if (row is null)
			{
				throw new ArgumentNullException(nameof(row));
			}

			var destinationIdValue = Convert.ToString(row[0]);
			Guid.TryParse(destinationIdValue, out var destinationId);
			Destination = destinationId;

			var actionValue = Convert.ToString(row[2]);
			Enum.TryParse<PendingActionType>(actionValue, out var action);
			Action = action;

			var timeValue = Convert.ToDouble(row[3]);
			Time = DateTime.FromOADate(timeValue);

			var pendingSourceIdValue = Convert.ToString(row[4]);
			if (!String.IsNullOrWhiteSpace(pendingSourceIdValue) &&
				Guid.TryParse(pendingSourceIdValue, out var parsedPendingSourceId))
			{
				PendingSource = parsedPendingSourceId;
			}
		}

		public PendingActionType Action { get; }

		public DateTime Time { get; }

		public ApiObjectReference<Endpoint> Destination { get; }

		public ApiObjectReference<Endpoint>? PendingSource { get; }

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

		internal enum PendingActionType
		{
			Connect,
			Disconnect,
		}
	}
}
