namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.MediaOps.Live.Tools;

	public class ConnectionEndpointsMapping
	{
		private readonly ManyToManyMapping<Connection2, ApiObjectReference<Endpoint>> _mapping =
			new (PropertyComparer<Connection2>.Create(x => x.Destination));

		public int ConnectionCount => _mapping.Forward.Count;

		public int EndpointCount => _mapping.Reverse.Count;

		public IReadOnlyCollection<ApiObjectReference<Endpoint>> GetEndpoints(Connection2 connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return _mapping.Forward.TryGetValue(connection, out var endpoints)
				? endpoints.ToList()
				: Array.Empty<ApiObjectReference<Endpoint>>();
		}

		public IReadOnlyCollection<Connection2> GetConnections(ApiObjectReference<Endpoint> endpoint)
		{
			return _mapping.Reverse.TryGetValue(endpoint, out var connections)
				? connections.ToList()
				: Array.Empty<Connection2>();
		}

		public void Add(Connection2 connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			foreach (var endpoint in connection.GetEndpoints())
			{
				_mapping.TryAdd(connection, endpoint);
			}
		}

		public void Remove(Connection2 connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			_mapping.TryRemoveForward(connection);
		}

		public void AddOrUpdate(Connection2 connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			Remove(connection);
			Add(connection);
		}

		public void Clear()
		{
			_mapping.Clear();
		}

		public bool Contains(Connection2 connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return _mapping.Forward.ContainsKey(connection);
		}

		public bool Contains(ApiObjectReference<Endpoint> endpoint)
		{
			return _mapping.Reverse.ContainsKey(endpoint);
		}

		public bool Contains(Connection2 connection, ApiObjectReference<Endpoint> endpoint)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return _mapping.Contains(connection, endpoint);
		}
	}
}
