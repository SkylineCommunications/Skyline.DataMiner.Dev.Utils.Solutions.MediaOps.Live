namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Subscriptions;

	public sealed class MediationElement : IDisposable
	{
		public static readonly int ConnectionHandlerScriptsTableId = 1000;
		public static readonly int PendingConnectionActionsTableId = 3000;
		public static readonly int ConnectionsTableId = 5000;

		private readonly object _lock = new();

		private readonly MediaOpsLiveApi _api;

		private bool _isSubscribed;
		private TableSubscription _subscriptionConnections;
		private TableSubscription _subscriptionPendingConnectionActions;

		internal MediationElement(MediaOpsLiveApi api, IDmsElement dmsElement)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
			DmsElement = dmsElement ?? throw new ArgumentNullException(nameof(dmsElement));
		}

		public event EventHandler<ConnectionsChangedEvent> ConnectionsChanged;

		public event EventHandler<PendingConnectionActionsChangedEvent> PendingConnectionActionsChanged;

		public IDmsElement DmsElement { get; }

		public DmsElementId Id => DmsElement.DmsElementId;

		public int DmaId => DmsElement.AgentId;

		public int ElementId => DmsElement.Id;

		public string Name => DmsElement.Name;

		public IEnumerable<PendingConnectionAction> GetPendingConnectionActions()
		{
			if (DmsElement.State != Core.DataMinerSystem.Common.ElementState.Active)
			{
				return [];
			}

			var tableData = DmsElement.GetTable(PendingConnectionActionsTableId).GetData();
			return tableData.Values.Select(x => new PendingConnectionAction(x));
		}

		public IEnumerable<Connection> GetConnections()
		{
			if (DmsElement.State != Core.DataMinerSystem.Common.ElementState.Active)
			{
				return [];
			}

			var tableData = DmsElement.GetTable(ConnectionsTableId).GetData();
			return tableData.Values.Select(x => new Connection(x));
		}

		public void Subscribe(bool skipInitialEvents = true)
		{
			lock (_lock)
			{
				if (_isSubscribed)
					return;

				_subscriptionConnections = new TableSubscription(
					_api.Connection,
					DmsElement,
					MediationElement.ConnectionsTableId,
					skipInitialEvents: skipInitialEvents);
				_subscriptionConnections.OnChanged += HandleChange_Connections;

				_subscriptionPendingConnectionActions = new TableSubscription(
					_api.Connection,
					DmsElement,
					MediationElement.PendingConnectionActionsTableId,
					skipInitialEvents: skipInitialEvents);
				_subscriptionPendingConnectionActions.OnChanged += HandleChange_PendingConnectionActions;

				_isSubscribed = true;
			}
		}

		public void Unsubscribe()
		{
			lock (_lock)
			{
				if (!_isSubscribed)
					return;

				_subscriptionConnections.OnChanged -= HandleChange_Connections;
				_subscriptionConnections.Dispose();
				_subscriptionConnections = null;

				_subscriptionPendingConnectionActions.OnChanged -= HandleChange_Connections;
				_subscriptionPendingConnectionActions.Dispose();
				_subscriptionPendingConnectionActions = null;

				_isSubscribed = false;
			}
		}

		public void Dispose()
		{
			Unsubscribe();
		}

		public bool TryGetConnection(Guid destinationEndpointId, out Connection connection)
		{
			if (DmsElement.State != Core.DataMinerSystem.Common.ElementState.Active)
			{
				connection = null;
				return false;
			}

			try
			{
				var table = DmsElement.GetTable(ConnectionsTableId);
				var rowKey = Convert.ToString(destinationEndpointId);
				var row = table.GetRow(rowKey);

				if (row != null)
				{
					connection = new Connection(row);
					return true;
				}
			}
			catch
			{
				// ignore
			}

			connection = null;
			return false;
		}

		public bool TryGetPendingConnectionAction(Guid destinationEndpointId, out PendingConnectionAction pendingConnectionAction)
		{
			if (DmsElement.State != Core.DataMinerSystem.Common.ElementState.Active)
			{
				pendingConnectionAction = null;
				return false;
			}

			try
			{
				var table = DmsElement.GetTable(PendingConnectionActionsTableId);
				var rowKey = Convert.ToString(destinationEndpointId);
				var row = table.GetRow(rowKey);

				if (row != null)
				{
					pendingConnectionAction = new PendingConnectionAction(row);
					return true;
				}
			}
			catch
			{
				// ignore
			}

			pendingConnectionAction = null;
			return false;
		}

		public string GetConnectionHandlerScriptName(IDmsElement destinationElement)
		{
			if (destinationElement is null)
			{
				throw new ArgumentNullException(nameof(destinationElement));
			}

			var scriptColumn = DmsElement.GetTable(ConnectionHandlerScriptsTableId).GetColumn<string>(1003);

			var script = scriptColumn.GetValue(destinationElement.DmsElementId.Value, KeyType.PrimaryKey);

			if (string.IsNullOrEmpty(script))
			{
				throw new InvalidOperationException($"No connection handler script found for element '{destinationElement.Name}' in mediation element '{Name}'.");
			}

			return script;
		}

		private void HandleChange_Connections(object sender, TableValueChange e)
		{
			var updated = e.UpdatedRows.Values.Select(r => new Connection(r));
			var deleted = e.DeletedRows.Values.Select(r => new Connection(r));

			ConnectionsChanged?.Invoke(this, new ConnectionsChangedEvent(updated, deleted));
		}

		private void HandleChange_PendingConnectionActions(object sender, TableValueChange e)
		{
			var updated = e.UpdatedRows.Values.Select(r => new PendingConnectionAction(r));
			var deleted = e.DeletedRows.Values.Select(r => new PendingConnectionAction(r));

			PendingConnectionActionsChanged?.Invoke(this, new PendingConnectionActionsChangedEvent(updated, deleted));
		}
	}
}
