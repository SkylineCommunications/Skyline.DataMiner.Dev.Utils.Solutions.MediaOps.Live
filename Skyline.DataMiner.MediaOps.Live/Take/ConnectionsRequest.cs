namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;

	public class ConnectionsRequest
	{
		public ConnectionsRequest(ConnectionRequest connectionRequest)
		{
			if (connectionRequest == null)
			{
				throw new ArgumentNullException(nameof(connectionRequest));
			}

			Connections.Add(connectionRequest);
		}

		public ConnectionsRequest(IEnumerable<ConnectionRequest> connectionRequests)
		{
			if (connectionRequests == null)
			{
				throw new ArgumentNullException(nameof(connectionRequests));
			}

			Connections.AddRange(connectionRequests);
		}

		public List<ConnectionRequest> Connections { get; } = new List<ConnectionRequest>();

		public ConnectionsRequest Create(Endpoint source, Endpoint destination)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var request = new ConnectionRequest(source, destination);

			return new ConnectionsRequest(request);
		}

		public ConnectionsRequest Create(MediaOpsLiveApi api, VirtualSignalGroup source, VirtualSignalGroup destination, ICollection<Level> levels = null)
		{
			if (api == null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var connectionRequests = new List<ConnectionRequest>();

			var endpointIds = new List<Guid>();
			endpointIds.AddRange(source.GetEndpoints().Select(x => x.ID));
			endpointIds.AddRange(destination.GetEndpoints().Select(x => x.ID));

			var endpoints = api.Endpoints.Read(endpointIds);

			var join = source.Levels.Join(
				destination.Levels,
				left => left.Level,
				right => right.Level,
				(left, right) => new { Level = left.Level, SourceLevel = left, DistinationLevel = right });

			foreach (var item in join)
			{
				if (levels != null && !levels.Any(l => l.ID == item.Level))
				{
					continue;
				}

				endpoints.TryGetValue((Guid)item.SourceLevel.Endpoint, out var sourceEndpoint);
				endpoints.TryGetValue((Guid)item.DistinationLevel.Endpoint, out var destinationEndpoint);

				var request = new ConnectionRequest(sourceEndpoint, destinationEndpoint);
				connectionRequests.Add(request);
			}

			return new ConnectionsRequest(connectionRequests);
		}
	}
}
