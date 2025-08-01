namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.MediaOps.Live.Subscriptions;

	using ElementState = Skyline.DataMiner.Net.Messages.ElementState;

	public sealed class LiteConnectivityInfoProvider : IDisposable
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<Endpoint>, Connection> _connectionsByDestination = new();
		private readonly Dictionary<ApiObjectReference<Endpoint>, PendingConnectionAction> _pendingActionsByDestination = new();

		private readonly ConnectionEndpointsMapping _connectionEndpointsMapping = new();
		private readonly PendingConnectionActionMapping _pendingConnectionActionsMapping = new();

		private readonly Dictionary<DmsElementId, MediationElement> _elements = new();
		private readonly Dictionary<DmsElementId, MediationElementSubscription> _elementSubscriptions = new();
		private ElementStateSubscription _elementStateSubscription;

		public LiteConnectivityInfoProvider(MediaOpsLiveApi api, bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));

			Initialize(subscribe);
		}

		public event EventHandler<ICollection<ApiObjectReference<Endpoint>>> EndpointsImpacted;

		public MediaOpsLiveApi Api { get; }

		public bool IsSubscribed { get; private set; }

		public bool IsConnected(ApiObjectReference<Endpoint> source, ApiObjectReference<Endpoint> destination)
		{
			lock (_lock)
			{
				return _connectionsByDestination.TryGetValue(destination, out var connection) &&
					connection.IsConnected &&
					connection.ConnectedSource == source;
			}
		}

		public bool IsConnected(ApiObjectReference<Endpoint> endpoint)
		{
			lock (_lock)
			{
				var connections = _connectionEndpointsMapping.GetConnections(endpoint);

				return connections.Any(
					x => x.IsConnected &&
						(x.Destination == endpoint || x.ConnectedSource == endpoint));
			}
		}

		public bool TryGetConnectionForDestination(Endpoint destination, out Connection connection)
		{
			lock (_lock)
			{
				return _connectionEndpointsMapping.TryGetConnectionForDestination(destination, out connection);
			}
		}

		public bool TryGetPendingConnectionActionForDestination(Endpoint destination, out PendingConnectionAction pendingAction)
		{
			lock (_lock)
			{
				return _pendingConnectionActionsMapping.TryGetPendingConnectionActionForDestination(destination, out pendingAction);
			}
		}

		public IEnumerable<Connection> GetConnectionsWithSource(Endpoint source)
		{
			lock (_lock)
			{
				return _connectionEndpointsMapping.GetConnectionsWithSource(source);
			}
		}

		public IEnumerable<PendingConnectionAction> GetPendingConnectionActionsWithSource(Endpoint source)
		{
			lock (_lock)
			{
				return _pendingConnectionActionsMapping.GetPendingConnectionActionsWithSource(source);
			}
		}

		public void Subscribe()
		{
			lock (_lock)
			{
				if (IsSubscribed)
				{
					return;
				}

				_elementStateSubscription = new ElementStateSubscription(Api.Connection);
				_elementStateSubscription.OnStateChanged += Element_OnStateChanged;

				foreach (var element in _elements.Values)
				{
					SubscribeElement(element);
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

				_elementStateSubscription.OnStateChanged -= Element_OnStateChanged;
				_elementStateSubscription.Dispose();
				_elementStateSubscription = null;

				foreach (var elementSubscription in _elementSubscriptions.Values)
				{
					UnsubscribeElement(elementSubscription.MediationElement);
				}

				_elementSubscriptions.Clear();

				IsSubscribed = false;
			}
		}

		public void Dispose()
		{
			Unsubscribe();
		}

		private void Initialize(bool subscribe)
		{
			lock (_lock)
			{
				var mediationElements = Api.MediationElements.AllElements;

				foreach (var element in mediationElements)
				{
					_elements[element.DmsElementId] = element;
				}

				if (subscribe)
				{
					Subscribe();
				}

				Parallel.ForEach(mediationElements, LoadDataFromMediationElement);
			}
		}

		private void SubscribeElement(MediationElement element)
		{
			if (!_elementSubscriptions.ContainsKey(element.DmsElementId))
			{
				// Already subscribed
				return;
			}

			var subscription = element.CreateSubscription();
			subscription.ConnectionsChanged += Connections_OnChanged;
			subscription.PendingConnectionActionsChanged += PendingConnectionActions_OnChanged;
			subscription.Subscribe();

			_elementSubscriptions[element.DmsElementId] = subscription;
		}

		private void UnsubscribeElement(MediationElement element)
		{
			if (!_elementSubscriptions.TryGetValue(element.DmsElementId, out var subscription))
			{
				// Already unsubscribed or not found
				return;
			}

			subscription.Unsubscribe();
			subscription.ConnectionsChanged -= Connections_OnChanged;
			subscription.PendingConnectionActionsChanged -= PendingConnectionActions_OnChanged;
			subscription.Dispose();

			_elementSubscriptions.Remove(element.DmsElementId);
		}

		private void Element_OnStateChanged(object sender, ElementStateChange e)
		{
			lock (_lock)
			{
				if (e.Element.Protocol.Name != MediationElement.ProtocolName)
				{
					// not a MediationElement, ignore
					return;
				}

				if (e.State == ElementState.Active)
				{
					if (!_elements.ContainsKey(e.Element.DmsElementId))
					{
						var mediationElement = new MediationElement(Api, e.Element);
						_elements[mediationElement.DmsElementId] = mediationElement;

						SubscribeElement(mediationElement);
						LoadDataFromMediationElement(mediationElement);
					}
				}
				else if (e.State == ElementState.Deleted)
				{
					_elements.Remove(e.Element.DmsElementId);

					if (_elementSubscriptions.TryGetValue(e.Element.DmsElementId, out var subscription))
					{
						UnsubscribeElement(subscription.MediationElement);
					}

					var pendingActions = _pendingActionsByDestination.Values
						.Where(x => x.MediationElement.DmsElementId == e.Element.DmsElementId)
						.ToList();

					PendingConnectionActions_OnChanged(this, new PendingConnectionActionsChangedEvent([], pendingActions));

					var connections = _connectionsByDestination.Values
						.Where(x => x.MediationElement.DmsElementId == e.Element.DmsElementId)
						.ToList();

					Connections_OnChanged(this, new ConnectionsChangedEvent([], connections));
				}
			}
		}

		private void Connections_OnChanged(object sender, ConnectionsChangedEvent e)
		{
			lock (_lock)
			{
				var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

				foreach (var connection in e.DeletedConnections)
				{
					if (_connectionsByDestination.TryGetValue(connection.Destination, out var existingConnection))
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
						if (existingConnection == connection)
						{
							// No change
							continue;
						}

						impactedEndpoints.UnionWith(existingConnection.GetEndpoints());
					}

					impactedEndpoints.UnionWith(connection.GetEndpoints());
					_connectionsByDestination[connection.Destination] = connection;
					_connectionEndpointsMapping.AddOrUpdate(connection);
				}

				if (impactedEndpoints.Count > 0)
				{
					EndpointsImpacted?.Invoke(this, impactedEndpoints);
				}
			}
		}

		private void PendingConnectionActions_OnChanged(object sender, PendingConnectionActionsChangedEvent e)
		{
			lock (_lock)
			{
				var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

				foreach (var pendingAction in e.DeletedPendingActions)
				{
					if (_pendingActionsByDestination.TryGetValue(pendingAction.Destination, out var existingPendingAction))
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
						if (existingPendingConnectionAction != pendingAction)
						{
							// No change
							continue;
						}

						impactedEndpoints.UnionWith(existingPendingConnectionAction.GetEndpoints());
					}

					impactedEndpoints.UnionWith(pendingAction.GetEndpoints());
					_pendingActionsByDestination[pendingAction.Destination] = pendingAction;
					_pendingConnectionActionsMapping.AddOrUpdate(pendingAction);
				}

				if (impactedEndpoints.Count > 0)
				{
					EndpointsImpacted?.Invoke(this, impactedEndpoints);
				}
			}
		}

		private void LoadDataFromMediationElement(MediationElement element)
		{
			foreach (var connection in element.GetConnections())
			{
				_connectionsByDestination[connection.Destination] = connection;
				_connectionEndpointsMapping.AddOrUpdate(connection);
			}

			foreach (var action in element.GetPendingConnectionActions())
			{
				_pendingActionsByDestination[action.Destination] = action;
				_pendingConnectionActionsMapping.AddOrUpdate(action);
			}
		}
	}
}
