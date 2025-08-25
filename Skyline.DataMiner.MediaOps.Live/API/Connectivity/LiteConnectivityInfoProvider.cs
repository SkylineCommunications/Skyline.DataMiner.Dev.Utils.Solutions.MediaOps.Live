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
	using Skyline.DataMiner.MediaOps.Live.Tools;

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

		internal MediaOpsLiveApi Api { get; }

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

		public bool TryGetConnectionForDestination(ApiObjectReference<Endpoint> destination, out Connection connection)
		{
			lock (_lock)
			{
				return _connectionEndpointsMapping.TryGetConnectionForDestination(destination, out connection);
			}
		}

		public bool TryGetPendingConnectionActionForDestination(ApiObjectReference<Endpoint> destination, out PendingConnectionAction pendingAction)
		{
			lock (_lock)
			{
				return _pendingConnectionActionsMapping.TryGetPendingConnectionActionForDestination(destination, out pendingAction);
			}
		}

		public IEnumerable<Connection> GetConnectionsWithSource(ApiObjectReference<Endpoint> source)
		{
			lock (_lock)
			{
				return _connectionEndpointsMapping.GetConnectionsWithSource(source);
			}
		}

		public IEnumerable<PendingConnectionAction> GetPendingConnectionActionsWithSource(ApiObjectReference<Endpoint> source)
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

				foreach (var elementSubscription in _elementSubscriptions.Values.ToList())
				{
					UnsubscribeElement(elementSubscription.MediationElement);
				}

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
				var mediationElements = Api.MediationElements.GetAllElementsCached();

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
			if (_elementSubscriptions.ContainsKey(element.DmsElementId))
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

		private void Element_OnStateChanged(object sender, ElementStateChangeEvent e)
		{
			lock (_lock)
			{
				if (e.State == ElementState.Active)
				{
					MaybeAddNewMediationElement(e.ElementId);
				}
				else if (e.State == ElementState.Stopped)
				{
					HandleStoppedMediationElement(e.ElementId);
				}
			}
		}

		private void Connections_OnChanged(object sender, ConnectionsChangedEvent e)
		{
			lock (_lock)
			{
				UpdateConnections(e.UpdatedConnections, e.DeletedConnections);
			}
		}

		private void PendingConnectionActions_OnChanged(object sender, PendingConnectionActionsChangedEvent e)
		{
			lock (_lock)
			{
				UpdatePendingConnections(e.UpdatedPendingActions, e.DeletedPendingActions);
			}
		}

		private void UpdateConnections(ICollection<Connection> updated, ICollection<Connection> deleted)
		{
			var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

			foreach (var connection in deleted)
			{
				if (_connectionsByDestination.TryGetValue(connection.Destination, out var existingConnection))
				{
					impactedEndpoints.UnionWith(existingConnection.GetEndpoints());
					_connectionsByDestination.Remove(existingConnection.Destination);
					_connectionEndpointsMapping.Remove(existingConnection);
				}
			}

			foreach (var connection in updated)
			{
				impactedEndpoints.UnionWith(DetectImpactedEndpoints(connection));

				_connectionsByDestination[connection.Destination] = connection;
				_connectionEndpointsMapping.AddOrUpdate(connection);
			}

			if (impactedEndpoints.Count > 0)
			{
				EndpointsImpacted?.Invoke(this, impactedEndpoints);
			}
		}

		private void UpdatePendingConnections(ICollection<PendingConnectionAction> updated, ICollection<PendingConnectionAction> deleted)
		{
			var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

			foreach (var pendingAction in deleted)
			{
				if (_pendingActionsByDestination.TryGetValue(pendingAction.Destination, out var existingPendingAction))
				{
					impactedEndpoints.UnionWith(existingPendingAction.GetEndpoints());
					_pendingActionsByDestination.Remove(existingPendingAction.Destination);
					_pendingConnectionActionsMapping.Remove(existingPendingAction);
				}
			}

			foreach (var pendingAction in updated)
			{
				if (!IsRedundantPendingAction(pendingAction))
				{
					impactedEndpoints.UnionWith(DetectImpactedEndpoints(pendingAction));
				}

				_pendingActionsByDestination[pendingAction.Destination] = pendingAction;
				_pendingConnectionActionsMapping.AddOrUpdate(pendingAction);
			}

			if (impactedEndpoints.Count > 0)
			{
				EndpointsImpacted?.Invoke(this, impactedEndpoints);
			}
		}

		private ICollection<ApiObjectReference<Endpoint>> DetectImpactedEndpoints(Connection newConnection)
		{
			var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

			if (_connectionsByDestination.TryGetValue(newConnection.Destination, out var existingConnection))
			{
				if (existingConnection.IsConnected == newConnection.IsConnected &&
					existingConnection.ConnectedSource == newConnection.ConnectedSource)
				{
					// No change
					return impactedEndpoints;
				}

				impactedEndpoints.UnionWith(existingConnection.GetEndpoints());
			}

			impactedEndpoints.UnionWith(newConnection.GetEndpoints());

			return impactedEndpoints;
		}

		private ICollection<ApiObjectReference<Endpoint>> DetectImpactedEndpoints(PendingConnectionAction newPendingAction)
		{
			var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

			if (_pendingActionsByDestination.TryGetValue(newPendingAction.Destination, out var existingPendingConnectionAction))
			{
				if (existingPendingConnectionAction.Action == newPendingAction.Action &&
					existingPendingConnectionAction.PendingSource == newPendingAction.PendingSource)
				{
					// No change
					return impactedEndpoints;
				}

				impactedEndpoints.UnionWith(existingPendingConnectionAction.GetEndpoints());
			}

			impactedEndpoints.UnionWith(newPendingAction.GetEndpoints());

			return impactedEndpoints;
		}

		private bool IsRedundantPendingAction(PendingConnectionAction newPendingAction)
		{
			// Redundant pending "connect": already connected to the same source
			if (newPendingAction.Action == PendingConnectionActionType.Connect &&
				newPendingAction.PendingSource.HasValue &&
				IsConnected(newPendingAction.Destination, newPendingAction.PendingSource.Value))
			{
				return true;
			}

			// Redundant pending "disconnect": nothing connected
			if (newPendingAction.Action == PendingConnectionActionType.Disconnect &&
				!IsConnected(newPendingAction.Destination))
			{
				return true;
			}

			return false;
		}

		private void LoadDataFromMediationElement(MediationElement element)
		{
			var connections = element.GetConnections().ToList();
			var pendingActions = element.GetPendingConnectionActions().ToList();

			UpdateConnections(connections, []);
			UpdatePendingConnections(pendingActions, []);
		}

		private void MaybeAddNewMediationElement(DmsElementId elementId)
		{
			if (_elements.ContainsKey(elementId))
			{
				// Element already exists, nothing to do
				return;
			}

			var dms = Api.Connection.GetDms();

			var dmsElement = Retry.Do(
				() => dms.GetElement(elementId),
				TimeSpan.FromMilliseconds(250),
				10);

			if (dmsElement.Protocol.Name != MediationElement.ProtocolName)
			{
				// not a mediation element, ignore
				return;
			}

			var mediationElement = new MediationElement(Api, dmsElement);
			_elements[mediationElement.DmsElementId] = mediationElement;

			SubscribeElement(mediationElement);
			LoadDataFromMediationElement(mediationElement);
		}

		private void HandleStoppedMediationElement(DmsElementId elementId)
		{
			if (!_elements.ContainsKey(elementId))
			{
				// Element not found, nothing to do
				return;
			}

			if (_elementSubscriptions.TryGetValue(elementId, out var subscription))
			{
				UnsubscribeElement(subscription.MediationElement);
			}

			// Remove all connections and pending actions related to this element
			var pendingActions = _pendingActionsByDestination.Values
				.Where(x => x.MediationElement.DmsElementId == elementId)
				.ToList();

			var connections = _connectionsByDestination.Values
				.Where(x => x.MediationElement.DmsElementId == elementId)
				.ToList();

			UpdatePendingConnections([], pendingActions);
			UpdateConnections([], connections);

			_elements.Remove(elementId);
		}
	}
}
