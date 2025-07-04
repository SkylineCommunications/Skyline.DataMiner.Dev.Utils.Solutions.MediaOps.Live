namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;
	using Skyline.DataMiner.MediaOps.Live.Subscriptions;
	using Skyline.DataMiner.Net.Messages;

	public sealed class MediationElement : IDisposable
	{
		private readonly object _lock = new object();
		private readonly MediaOpsLiveApi _api;

		private bool _isSubscribed;
		private TableSubscription _connectionsSubscription;
		private TableSubscription _pendingConnectionActionsSubscription;

		internal MediationElement(MediaOpsLiveApi api, IDmsElement dmsElement)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
			DmsElement = dmsElement ?? throw new ArgumentNullException(nameof(dmsElement));
		}

		public IDmsElement DmsElement { get; }

		public DmsElementId Id => DmsElement.DmsElementId;

		public int DmaId => DmsElement.AgentId;

		public int ElementId => DmsElement.Id;

		public string Name => DmsElement.Name;

		public event EventHandler<PendingConnectionActionsChangedEvent> PendingConnectionActionsChanged;

		public event EventHandler<ConnectionsChangedEvent> ConnectionsChanged;

		public void Subscribe()
		{
			lock (_lock)
			{
				if (_isSubscribed)
				{
					return;
				}

				_pendingConnectionActionsSubscription = new TableSubscription(_api.Connection, DmsElement, 3000);
				_pendingConnectionActionsSubscription.OnChanged += PendingConnectionActions_OnChanged;

				_connectionsSubscription = new TableSubscription(_api.Connection, DmsElement, 5000);
				_connectionsSubscription.OnChanged += Connections_OnChanged;

				_isSubscribed = true;
			}
		}

		public void Unsubscribe()
		{
			lock (_lock)
			{
				if (!_isSubscribed)
				{
					return;
				}

				if (_pendingConnectionActionsSubscription != null)
				{
					_pendingConnectionActionsSubscription.OnChanged -= PendingConnectionActions_OnChanged;
					_pendingConnectionActionsSubscription.Dispose();
					_pendingConnectionActionsSubscription = null;
				}

				if (_connectionsSubscription != null)
				{
					_connectionsSubscription.OnChanged -= Connections_OnChanged;
					_connectionsSubscription.Dispose();
					_connectionsSubscription = null;
				}

				_isSubscribed = false;
			}
		}

		public IEnumerable<PendingConnectionAction> GetPendingConnectionActions()
		{
			if (DmsElement.State != Core.DataMinerSystem.Common.ElementState.Active)
			{
				return [];
			}

			var table = DmsElement.GetTable(3000).GetData();
			return table.Values.Select(x => new PendingConnectionAction(x));
		}

		public IEnumerable<Connection> GetConnections()
		{
			if (DmsElement.State != Core.DataMinerSystem.Common.ElementState.Active)
			{
				return [];
			}

			var table = DmsElement.GetTable(5000).GetData();
			return table.Values.Select(x => new Connection(x));
		}

		public string GetConnectionHandlerScriptName(IDmsElement destinationElement)
		{
			if (destinationElement is null)
			{
				throw new ArgumentNullException(nameof(destinationElement));
			}

			var scriptColumn = DmsElement.GetTable(1000).GetColumn<string>(1003);

			var script = scriptColumn.GetValue(destinationElement.DmsElementId.Value, KeyType.PrimaryKey);

			if (string.IsNullOrEmpty(script))
			{
				throw new InvalidOperationException($"No connection handler script found for element '{destinationElement.Name}' in mediation element '{Name}'.");
			}

			return script;
		}

		public void Dispose()
		{
			Unsubscribe();
		}

		public static IEnumerable<MediationElement> GetAllMediationElements(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			var dms = api.Connection.GetDms();

			var request = new GetLiteElementInfo
			{
				ProtocolName = Constants.MediationProtocolName,
			};

			var responses = api.Connection.HandleMessage(request);

			foreach (var liteElementInfo in responses.OfType<LiteElementInfoEvent>())
			{
				var elementId = new DmsElementId(liteElementInfo.DataMinerID, liteElementInfo.ElementID);
				var dmsElement = dms.GetElementReference(elementId);

				yield return new MediationElement(api, dmsElement);
			}
		}

		public static IDictionary<EndpointInfo, MediationElement> GetMediationElements(MediaOpsLiveApi api, IEnumerable<EndpointInfo> endpoints)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			var dms = api.Connection.GetDms();

			var endpointToElement = endpoints
				.GroupBy(e => e.Element)
				.SelectMany(group =>
				{
					var element = dms.GetElementReference(new DmsElementId(group.Key));
					return group.Select(endpoint => new { endpoint, element });
				})
				.ToDictionary(x => x.endpoint, x => x.element);

			var allMediationElements = GetAllMediationElements(api)
				.ToDictionary(e => e.DmsElement.Host.Id);

			var result = new Dictionary<EndpointInfo, MediationElement>();

			foreach (var group in endpointToElement.GroupBy(kvp => kvp.Value.Host.Id))
			{
				if (!allMediationElements.TryGetValue(group.Key, out var mediationElement))
				{
					throw new InvalidOperationException($"Couldn't find MediaOps mediation element on hosting agent {group.Key}");
				}

				foreach (var kvp in group)
				{
					result[kvp.Key] = mediationElement;
				}
			}

			return result;
		}

		public static IDictionary<Endpoint, MediationElement> GetMediationElements(MediaOpsLiveApi api, IEnumerable<Endpoint> endpoints)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			var endpointInfoMap = endpoints.ToDictionary(x => new EndpointInfo(x));
			var mediationElementMap = GetMediationElements(api, endpointInfoMap.Keys);

			var result = new Dictionary<Endpoint, MediationElement>();

			foreach (var endpointInfo in endpointInfoMap.Keys)
			{
				result[endpointInfoMap[endpointInfo]] = mediationElementMap[endpointInfo];
			}

			return result;
		}

		public static MediationElement GetMediationElement(MediaOpsLiveApi api, EndpointInfo endpoint)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			var mediationElements = GetMediationElements(api, [endpoint]);

			if (mediationElements.Count != 1)
			{
				throw new InvalidOperationException($"Expected exactly one mediation element for endpoint '{endpoint.Name}', but found {mediationElements.Count}.");
			}

			return mediationElements[endpoint];
		}

		public static MediationElement GetMediationElement(MediaOpsLiveApi api, Endpoint endpoint)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			return GetMediationElement(api, new EndpointInfo(endpoint));
		}

		private void PendingConnectionActions_OnChanged(object sender, TableValueChange e)
		{
			var updatedActions = e.UpdatedRows.Values.Select(row => new PendingConnectionAction(row));
			var deletedActionKeys = e.DeletedRows.Select(key => Guid.Parse(key));

			var eventArgs = new PendingConnectionActionsChangedEvent(updatedActions, deletedActionKeys);
			PendingConnectionActionsChanged?.Invoke(this, eventArgs);
		}

		private void Connections_OnChanged(object sender, TableValueChange e)
		{
			var updatedConnections = e.UpdatedRows.Values.Select(row => new Connection(row));
			var deletedConnectionKeys = e.DeletedRows.Select(key => Guid.Parse(key));

			var eventArgs = new ConnectionsChangedEvent(updatedConnections, deletedConnectionKeys);
			ConnectionsChanged?.Invoke(this, eventArgs);
		}
	}
}
