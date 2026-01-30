namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Tools;

	internal sealed class PendingConnectionActionMapping
	{
		private readonly ManyToManyMapping<PendingConnectionAction, ApiObjectReference<Endpoint>> _mapping =
			new(PropertyComparer<PendingConnectionAction>.Create(x => x.Destination));

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

		public IReadOnlyCollection<PendingConnectionAction> GetPendingConnectionActionsWithSource(ApiObjectReference<Endpoint> source)
		{
			var pendingConnectionActions = GetPendingConnectionActions(source);

			return pendingConnectionActions.Where(c => c.PendingSource == source).ToList();
		}

		public bool TryGetPendingConnectionActionForDestination(ApiObjectReference<Endpoint> destination, out PendingConnectionAction pendingConnectionAction)
		{
			var pendingConnectionActions = GetPendingConnectionActions(destination);

			pendingConnectionAction = pendingConnectionActions.SingleOrDefault(c => c.Destination == destination);
			return pendingConnectionAction != null;
		}

		public bool IsConnecting(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination)
		{
			var actions = GetPendingConnectionActions(destination);

			return actions.Any(x => x.Action == PendingConnectionActionType.Connect &&
				x.Destination == destination &&
				x.PendingSource == source);
		}

		public bool IsDisconnecting(ApiObjectReference<Endpoint> destination)
		{
			var actions = GetPendingConnectionActions(destination);

			return actions.Any(x => x.Action == PendingConnectionActionType.Disconnect &&
				x.Destination == destination);
		}

		public void Add(PendingConnectionAction pendingAction)
		{
			if (pendingAction is null)
			{
				throw new ArgumentNullException(nameof(pendingAction));
			}

			foreach (var endpoint in pendingAction.GetEndpoints())
			{
				_mapping.TryAdd(pendingAction, endpoint);
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
