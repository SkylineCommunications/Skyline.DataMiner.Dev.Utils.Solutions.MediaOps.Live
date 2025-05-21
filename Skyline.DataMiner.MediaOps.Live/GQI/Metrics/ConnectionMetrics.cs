namespace Skyline.DataMiner.MediaOps.Live.GQI.Metrics
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;

	public class ConnectionMetrics
	{
		private readonly object _lock = new object();
		private readonly ConnectionInterceptor _connection;

		public ConnectionMetrics(ConnectionInterceptor connection)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));

			_connection.MessagesProcessed += OnMessagesProcessed;
		}

		public ulong NumberOfRequests { get; private set; }

		public ulong NumberOfDomRequests { get; private set; }

		public ulong NumberOfDomInstancesRetrieved { get; private set; }

		public TimeSpan TotalRequestDuration { get; private set; }

		public TimeSpan MaxRequestDuration { get; private set; }

		public TimeSpan AvgRequestDuration =>
			NumberOfRequests > 0
				? TimeSpan.FromTicks(TotalRequestDuration.Ticks / (long)NumberOfRequests)
				: TimeSpan.Zero;

		private void OnMessagesProcessed(object sender, ProcessedMessages e)
		{
			lock (_lock)
			{
				NumberOfRequests += (ulong)e.Requests.Count;
				TotalRequestDuration += e.Duration;

				if (e.Duration > MaxRequestDuration)
				{
					MaxRequestDuration = e.Duration;
				}

				UpdateDomMetrics(e.Requests);
				UpdateDomMetrics(e.Responses);
			}
		}

		private void UpdateDomMetrics(IEnumerable<DMSMessage> messages)
		{
			foreach (var message in messages)
			{
				switch (message)
				{
					case ManagerStoreReadRequest<DomInstance> m:
						NumberOfDomRequests++;
						break;

					case ManagerStoreCreateRequest<DomInstance> m:
						NumberOfDomRequests++;
						break;

					case ManagerStoreUpdateRequest<DomInstance> m:
						NumberOfDomRequests++;
						break;

					case ManagerStoreCountRequest<DomInstance> m:
						NumberOfDomRequests++;
						break;

					case ManagerStoreBulkCreateOrUpdateRequest<DomInstance> m:
						NumberOfDomRequests++;
						break;

					case ManagerStoreBulkDeleteRequest<DomInstance> m:
						NumberOfDomRequests++;
						break;

					case ManagerStoreStartPagingRequest<DomInstance> m:
						NumberOfDomRequests++;
						break;

					case ManagerStoreNextPagingRequest<DomInstance> m:
						NumberOfDomRequests++;
						break;

					case ManagerStorePagingResponse<DomInstance> m:
						NumberOfDomInstancesRetrieved += (ulong)(m.Objects?.Count ?? 0);
						break;

					case ManagerStoreCountResponse<DomInstance> m:
						NumberOfDomInstancesRetrieved += (ulong)(m.Objects?.Count ?? 0);
						break;

					case ManagerStoreCrudResponse<DomInstance> m:
						NumberOfDomInstancesRetrieved += (ulong)(m.Objects?.Count ?? 0);
						break;
				}
			}
		}
	}
}
