namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;

	public sealed class MediationElement
	{
		public static readonly int ConnectionHandlerScriptsTableId = 1000;
		public static readonly int PendingConnectionActionsTableId = 3000;
		public static readonly int ConnectionsTableId = 5000;

		private readonly MediaOpsLiveApi _api;

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

		public ConnectionSubscription CreateConnectionSubscription()
		{
			return new ConnectionSubscription(_api, this);
		}

		public PendingConnectionActionSubscription CreatePendingActionSubscription()
		{
			return new PendingConnectionActionSubscription(_api, this);
		}

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

		public bool TryGetConnection(Guid destinationEndpointId, out Connection connection)
		{
			if (DmsElement.State != Core.DataMinerSystem.Common.ElementState.Active)
			{
				connection = null;
				return false;
			}

			var rowKey = Convert.ToString(destinationEndpointId);

			try
			{
				var table = DmsElement.GetTable(ConnectionsTableId);
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

			var rowKey = Convert.ToString(destinationEndpointId);

			try
			{
				var table = DmsElement.GetTable(PendingConnectionActionsTableId);
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
	}
}
