namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	internal class PendingConnectionAction
	{
		public PendingConnectionAction(ApiObjectReference<Endpoint> destination, PendingActionType action, ApiObjectReference<Endpoint>? pendingSource)
		{
			if (destination == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("Destination cannot be an empty reference.");
			}

			if (pendingSource.HasValue && pendingSource == ApiObjectReference<Endpoint>.Empty)
			{
				throw new ArgumentException("Pending source cannot be an empty reference.");
			}

			Destination = destination;
			Action = action;
			PendingSource = pendingSource;
		}

		public ApiObjectReference<Endpoint> Destination { get; }

		public PendingActionType Action { get; }

		public ApiObjectReference<Endpoint>? PendingSource { get; }

		internal enum PendingActionType
		{
			Connect,
			Disconnect,
		}
	}
}
