namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public sealed class Connection2
	{
		internal Connection2(object[] row)
		{
			if (row is null)
			{
				throw new ArgumentNullException(nameof(row));
			}

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
	}
}
