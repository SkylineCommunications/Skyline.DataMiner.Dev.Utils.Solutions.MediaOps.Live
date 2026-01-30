namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Tools;

	public class ConnectionEndpointsMapping
	{
		private readonly ManyToManyMapping<Connection, ApiObjectReference<Endpoint>> _mapping =
			new(PropertyComparer<Connection>.Create(x => x.Destination));

		public int ConnectionCount => _mapping.Forward.Count;

		public int EndpointCount => _mapping.Reverse.Count;

		public IReadOnlyCollection<ApiObjectReference<Endpoint>> GetEndpoints(Connection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return _mapping.Forward.TryGetValue(connection, out var endpoints)
				? endpoints.ToList()
				: Array.Empty<ApiObjectReference<Endpoint>>();
		}

		public IReadOnlyCollection<Connection> GetConnections(ApiObjectReference<Endpoint> endpoint)
		{
			return _mapping.Reverse.TryGetValue(endpoint, out var connections)
				? connections.ToList()
				: Array.Empty<Connection>();
		}

		public IReadOnlyCollection<Connection> GetConnectionsWithSource(ApiObjectReference<Endpoint> source)
		{
			var connections = GetConnections(source);

			return connections.Where(c => c.ConnectedSource == source).ToList();
		}

		public bool TryGetConnectionForDestination(ApiObjectReference<Endpoint> destination, out Connection connection)
		{
			var connections = GetConnections(destination);

			connection = connections.SingleOrDefault(c => c.Destination == destination);
			return connection != null;
		}

		public bool IsConnected(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination)
		{
			return TryGetConnectionForDestination(destination, out var connection) &&
				connection.IsConnected &&
				connection.ConnectedSource == source;
		}

		public void Add(Connection connection)
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

		public void Remove(Connection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			_mapping.TryRemoveForward(connection);
		}

		public void AddOrUpdate(Connection connection)
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

		public bool Contains(Connection connection)
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

		public bool Contains(Connection connection, ApiObjectReference<Endpoint> endpoint)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return _mapping.Contains(connection, endpoint);
		}
	}
}
