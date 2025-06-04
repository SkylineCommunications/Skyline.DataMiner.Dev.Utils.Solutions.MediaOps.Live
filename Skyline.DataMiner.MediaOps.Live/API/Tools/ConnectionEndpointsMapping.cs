namespace Skyline.DataMiner.MediaOps.Live.API.Tools
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.Tools;

	public class ConnectionEndpointsMapping
	{
		private readonly ManyToManyMapping<Connection, ApiObjectReference<Endpoint>> _mapping = new();

		public int ConnectionCount => _mapping.Forward.Count;

		public int EndpointCount => _mapping.Reverse.Count;

		public bool TryGetEndpoints(Connection connection, out ICollection<ApiObjectReference<Endpoint>> endpoints)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return _mapping.Forward.TryGetValue(connection, out endpoints);
		}

		public bool TryGetConnections(ApiObjectReference<Endpoint> endpoint, out ICollection<Connection> connections)
		{
			return _mapping.Reverse.TryGetValue(endpoint, out connections);
		}

		public void Add(Connection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			var endpoints = new[]
				{
					connection.Destination,
					connection.ConnectedSource,
					connection.PendingConnectedSource,
				}
				.Where(e => e.HasValue)
				.Select(e => e.Value);

			foreach (var endpoint in endpoints)
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

			_mapping.RemoveForward(connection);
		}

		public void Update(Connection connection)
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
