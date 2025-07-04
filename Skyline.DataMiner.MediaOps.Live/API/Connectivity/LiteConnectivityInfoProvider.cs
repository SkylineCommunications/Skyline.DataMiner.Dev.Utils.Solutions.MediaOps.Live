namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;

	public sealed class LiteConnectivityInfoProvider : IDisposable
	{
		private readonly object _lock = new();

		private readonly ICollection<MediationElement> _mediationElements;

		private readonly Dictionary<ApiObjectReference<Endpoint>, Connection> _connectionsByDestination = new();
		private readonly Dictionary<ApiObjectReference<Endpoint>, PendingConnectionAction> _pendingActionsByDestination = new();

		private readonly ConnectionEndpointsMapping _connectionEndpointsMapping = new();
		private readonly PendingConnectionActionMapping _pendingConnectionActionsMapping = new();

		public LiteConnectivityInfoProvider(MediaOpsLiveApi api, bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));

			_mediationElements = MediationElement.GetAllMediationElements(api).ToList();

			if (subscribe)
			{
				Subscribe();
			}
			else
			{
				LoadDataFromMediationElements();
			}
		}

		public event EventHandler<ICollection<ApiObjectReference<Endpoint>>> ConnectionsChanged;

		public event EventHandler<ICollection<ApiObjectReference<Endpoint>>> PendingConnectionActionsChanged;

		public MediaOpsLiveApi Api { get; }

		public bool IsSubscribed { get; private set; }

		public bool IsConnected(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination)
		{
			return _connectionsByDestination.TryGetValue(destination, out var connection) &&
				connection.IsConnected &&
				connection.ConnectedSource == source;
		}

		public bool IsConnected(ApiObjectReference<Endpoint> endpoint)
		{
			var connections = _connectionEndpointsMapping.GetConnections(endpoint);

			return connections.Any(
				x => x.IsConnected &&
					(x.Destination == endpoint || x.ConnectedSource == endpoint));
		}

		public void Subscribe()
		{
			lock (_lock)
			{
				if (IsSubscribed)
				{
					return;
				}

				foreach (var mediationElement in _mediationElements)
				{
					mediationElement.PendingConnectionActionsChanged += PendingConnectionActions_OnChanged;
					mediationElement.ConnectionsChanged += Connections_OnChanged;
					mediationElement.Subscribe();
				}

				IsSubscribed = true;
			}
		}

		public void Unsubscribe()
		{
			lock (_lock)
			{
				if (!IsSubscribed)
				{
					return;
				}

				foreach (var mediationElement in _mediationElements)
				{
					mediationElement.Unsubscribe();
					mediationElement.PendingConnectionActionsChanged -= PendingConnectionActions_OnChanged;
					mediationElement.ConnectionsChanged -= Connections_OnChanged;
				}

				IsSubscribed = false;
			}
		}

		public void Dispose()
		{
			Unsubscribe();
		}

		private void Connections_OnChanged(object sender, ConnectionsChangedEvent e)
		{
			lock (_lock)
			{
				var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

				foreach (var destinationId in e.DeletedConnections)
				{
					if (_connectionsByDestination.TryGetValue(destinationId, out var existingConnection))
					{
						impactedEndpoints.UnionWith(existingConnection.GetEndpoints());
						_connectionsByDestination.Remove(existingConnection.Destination);
						_connectionEndpointsMapping.Remove(existingConnection);
					}
				}

				foreach (var connection in e.UpdatedConnections)
				{
					if (_connectionsByDestination.TryGetValue(connection.Destination, out var existingConnection))
					{
						impactedEndpoints.UnionWith(existingConnection.GetEndpoints());
					}

					impactedEndpoints.UnionWith(connection.GetEndpoints());
					_connectionsByDestination[connection.Destination] = connection;
					_connectionEndpointsMapping.AddOrUpdate(connection);
				}

				if (impactedEndpoints.Count > 0)
				{
					ConnectionsChanged?.Invoke(this, impactedEndpoints);
				}
			}
		}

		private void PendingConnectionActions_OnChanged(object sender, PendingConnectionActionsChangedEvent e)
		{
			lock (_lock)
			{
				var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

				foreach (var destinationId in e.DeletedPendingActions)
				{
					if (_pendingActionsByDestination.TryGetValue(destinationId, out var existingPendingAction))
					{
						impactedEndpoints.UnionWith(existingPendingAction.GetEndpoints());
						_pendingActionsByDestination.Remove(existingPendingAction.Destination);
						_pendingConnectionActionsMapping.Remove(existingPendingAction);
					}
				}

				foreach (var pendingAction in e.UpdatedPendingActions)
				{
					if (_pendingActionsByDestination.TryGetValue(pendingAction.Destination, out var existingPendingConnectionAction))
					{
						impactedEndpoints.UnionWith(existingPendingConnectionAction.GetEndpoints());
					}

					impactedEndpoints.UnionWith(pendingAction.GetEndpoints());
					_pendingActionsByDestination[pendingAction.Destination] = pendingAction;
					_pendingConnectionActionsMapping.AddOrUpdate(pendingAction);
				}

				if (impactedEndpoints.Count > 0)
				{
					PendingConnectionActionsChanged?.Invoke(this, impactedEndpoints);
				}
			}
		}

		private void LoadDataFromMediationElements()
		{
			lock (_lock)
			{
				var connections = _mediationElements.AsParallel().SelectMany(x => x.GetConnections()).ToList();
				var pendingConnectionActions = _mediationElements.AsParallel().SelectMany(x => x.GetPendingConnectionActions()).ToList();

				foreach (var connection in connections)
				{
					_connectionsByDestination[connection.Destination] = connection;
					_connectionEndpointsMapping.AddOrUpdate(connection);
				}

				foreach (var action in pendingConnectionActions)
				{
					_pendingActionsByDestination[action.Destination] = action;
					_pendingConnectionActionsMapping.AddOrUpdate(action);
				}
			}
		}
	}
}
