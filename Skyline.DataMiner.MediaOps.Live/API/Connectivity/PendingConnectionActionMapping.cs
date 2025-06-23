namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Tools;

	internal class PendingConnectionActionMapping
	{
		private readonly ManyToManyMapping<PendingConnectionAction, ApiObjectReference<Endpoint>> _mapping = new();

		public int PendingConnectionActionCount => _mapping.Forward.Count;

		public int EndpointCount => _mapping.Reverse.Count;

		public IReadOnlyCollection<ApiObjectReference<Endpoint>> GetEndpoints(PendingConnectionAction pendingAction)
		{
			if (pendingAction is null)
			{
				throw new ArgumentNullException(nameof(pendingAction));
			}

			return _mapping.Forward.TryGetValue(pendingAction, out var endpoints)
				? endpoints.ToList()
				: Array.Empty<ApiObjectReference<Endpoint>>();
		}

		public IReadOnlyCollection<PendingConnectionAction> GetPendingConnectionActions(ApiObjectReference<Endpoint> endpoint)
		{
			return _mapping.Reverse.TryGetValue(endpoint, out var connections)
				? connections.ToList()
				: Array.Empty<PendingConnectionAction>();
		}

		public void Add(PendingConnectionAction pendingAction)
		{
			if (pendingAction is null)
			{
				throw new ArgumentNullException(nameof(pendingAction));
			}

			_mapping.TryAdd(pendingAction, pendingAction.Destination);

			if (pendingAction.PendingSource.HasValue)
			{
				_mapping.TryAdd(pendingAction, pendingAction.PendingSource.Value);
			}
		}

		public void Remove(PendingConnectionAction pendingAction)
		{
			if (pendingAction is null)
			{
				throw new ArgumentNullException(nameof(pendingAction));
			}

			_mapping.TryRemoveForward(pendingAction);
		}

		public void AddOrUpdate(PendingConnectionAction pendingAction)
		{
			if (pendingAction is null)
			{
				throw new ArgumentNullException(nameof(pendingAction));
			}

			Remove(pendingAction);
			Add(pendingAction);
		}

		public void Clear()
		{
			_mapping.Clear();
		}

		public bool Contains(PendingConnectionAction pendingAction)
		{
			if (pendingAction is null)
			{
				throw new ArgumentNullException(nameof(pendingAction));
			}

			return _mapping.Forward.ContainsKey(pendingAction);
		}

		public bool Contains(ApiObjectReference<Endpoint> endpoint)
		{
			return _mapping.Reverse.ContainsKey(endpoint);
		}

		public bool Contains(PendingConnectionAction pendingAction, ApiObjectReference<Endpoint> endpoint)
		{
			if (pendingAction is null)
			{
				throw new ArgumentNullException(nameof(pendingAction));
			}

			return _mapping.Contains(pendingAction, endpoint);
		}
	}
}
