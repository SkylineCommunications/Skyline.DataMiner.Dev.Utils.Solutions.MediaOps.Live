namespace Skyline.DataMiner.MediaOps.Live.API.Connectivity
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
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

		private bool _isDisposed;

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
			if (_isDisposed)
			{
				return;
			}

			Unsubscribe();

			_isDisposed = true;
		}

		private void Initialize(bool subscribe)
		{
			IReadOnlyCollection<MediationElement> mediationElements;

			lock (_lock)
			{
				mediationElements = Api.MediationElements.GetAllElementsCached();

				foreach (var element in mediationElements)
				{
					_elements[element.DmsElementId] = element;
				}

				if (subscribe)
				{
					Subscribe();
				}
			}

			Parallel.ForEach(mediationElements, LoadDataFromMediationElement);
		}

		private void SubscribeElement(MediationElement element)
		{
			lock (_lock)
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
		}

		private void UnsubscribeElement(MediationElement element)
		{
			lock (_lock)
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
		}

		private void Element_OnStateChanged(object sender, ElementStateChangeEvent e)
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

		private void Connections_OnChanged(object sender, ConnectionsChangedEvent e)
		{
			UpdateConnections(e.UpdatedConnections, e.DeletedConnections);
		}

		private void PendingConnectionActions_OnChanged(object sender, PendingConnectionActionsChangedEvent e)
		{
			UpdatePendingConnections(e.UpdatedPendingActions, e.DeletedPendingActions);
		}

		private void UpdateConnections(ICollection<Connection> updated, ICollection<Connection> deleted)
		{
			var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

			lock (_lock)
			{
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
					if (_connectionsByDestination.TryGetValue(connection.Destination, out var existingConnection))
					{
						if (existingConnection.IsConnected == connection.IsConnected &&
							existingConnection.ConnectedSource == connection.ConnectedSource)
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
			}

			RaiseEndpointsImpacted(impactedEndpoints);
		}

		private void UpdatePendingConnections(ICollection<PendingConnectionAction> updated, ICollection<PendingConnectionAction> deleted)
		{
			var impactedEndpoints = new HashSet<ApiObjectReference<Endpoint>>();

			lock (_lock)
			{
				foreach (var pendingAction in deleted)
				{
					if (_pendingActionsByDestination.TryGetValue(pendingAction.Destination, out var existingPendingAction))
					{
						if (PendingActionHasImpact(existingPendingAction))
						{
							// Only raise event if it actually had impact
							impactedEndpoints.UnionWith(existingPendingAction.GetEndpoints());
						}

						_pendingActionsByDestination.Remove(existingPendingAction.Destination);
						_pendingConnectionActionsMapping.Remove(existingPendingAction);
					}
				}

				foreach (var pendingAction in updated)
				{
					bool hasImpact = PendingActionHasImpact(pendingAction);

					if (_pendingActionsByDestination.TryGetValue(pendingAction.Destination, out var existingPendingConnectionAction))
					{
						if (existingPendingConnectionAction.Action == pendingAction.Action &&
							existingPendingConnectionAction.PendingSource == pendingAction.PendingSource)
						{
							// No change
							continue;
						}

						if (hasImpact)
						{
							impactedEndpoints.UnionWith(existingPendingConnectionAction.GetEndpoints());
						}
					}

					_pendingActionsByDestination[pendingAction.Destination] = pendingAction;
					_pendingConnectionActionsMapping.AddOrUpdate(pendingAction);

					if (hasImpact)
					{
						impactedEndpoints.UnionWith(pendingAction.GetEndpoints());
					}
				}
			}

			RaiseEndpointsImpacted(impactedEndpoints);
		}

		/// <summary>
		/// Determines whether a pending connection action actually impacts the effective connection state.
		/// </summary>
		private bool PendingActionHasImpact(PendingConnectionAction pendingAction)
		{
			if (!_connectionsByDestination.TryGetValue(pendingAction.Destination, out var existingConnection))
			{
				// No existing connection: only connect actions have impact
				return pendingAction.Action == PendingConnectionActionType.Connect;
			}

			return pendingAction.Action switch
			{
				// Connect: impact if not connected yet or connected to a different source
				PendingConnectionActionType.Connect =>
					!existingConnection.IsConnected || existingConnection.ConnectedSource != pendingAction.PendingSource,

				// Disconnect: impact if currently connected
				PendingConnectionActionType.Disconnect =>
					existingConnection.IsConnected,

				_ => true
			};
		}

		private void RaiseEndpointsImpacted(ICollection<ApiObjectReference<Endpoint>> impactedEndpoints)
		{
			// Ensure we are not holding the lock when raising events
			// This could lead to deadlocks if event handlers try to call back into this class
			Debug.Assert(!Monitor.IsEntered(_lock), "Lock must not be held when raising events to prevent deadlocks from event handlers calling back into this class");

			if (impactedEndpoints.Count == 0)
			{
				return;
			}

			EndpointsImpacted?.Invoke(this, impactedEndpoints);
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
			MediationElement mediationElement;

			lock (_lock)
			{
				if (_elements.ContainsKey(elementId))
				{
					// Element already exists, nothing to do
					return;
				}

				var dms = Api.GetDms();

				var dmsElement = Retry.Do(
					() => dms.GetElement(elementId),
					TimeSpan.FromMilliseconds(250),
					10);

				if (dmsElement.Protocol.Name != MediationElement.ProtocolName)
				{
					// not a mediation element, ignore
					return;
				}

				mediationElement = new MediationElement(Api, dmsElement);
				_elements[mediationElement.DmsElementId] = mediationElement;

				SubscribeElement(mediationElement);
			}

			LoadDataFromMediationElement(mediationElement);
		}

		private void HandleStoppedMediationElement(DmsElementId elementId)
		{
			ICollection<PendingConnectionAction> pendingActions;
			ICollection<Connection> connections;

			lock (_lock)
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
				pendingActions = _pendingActionsByDestination.Values
					.Where(x => x.MediationElement.DmsElementId == elementId)
					.ToList();

				connections = _connectionsByDestination.Values
					.Where(x => x.MediationElement.DmsElementId == elementId)
					.ToList();

				_elements.Remove(elementId);
			}

			UpdatePendingConnections([], pendingActions);
			UpdateConnections([], connections);
		}
	}
}
