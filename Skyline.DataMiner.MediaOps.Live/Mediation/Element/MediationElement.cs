namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;

	public sealed class MediationElement
	{
		public static string ProtocolName => Constants.MediationProtocolName;

		internal static readonly int ElementsTableId = 1000;
		internal static readonly int ConnectionHandlerScriptsTableId = 2000;
		internal static readonly int PendingConnectionActionsTableId = 3000;
		internal static readonly int ConnectionsTableId = 5000;

		internal MediationElement(MediaOpsLiveApi api, IDmsElement dmsElement)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			DmsElement = dmsElement ?? throw new ArgumentNullException(nameof(dmsElement));
		}

		internal MediaOpsLiveApi Api { get; }

		public IDmsElement DmsElement { get; }

		public DmsElementId DmsElementId => DmsElement.DmsElementId;

		public int DmaId => DmsElement.AgentId;

		public int ElementId => DmsElement.Id;

		public string Name => DmsElement.Name;

		public MediationElementSubscription CreateSubscription()
		{
			return new MediationElementSubscription(Api, this);
		}

		public IEnumerable<PendingConnectionAction> GetPendingConnectionActions()
		{
			if (DmsElement.State != ElementState.Active)
			{
				return [];
			}

			var tableData = DmsElement.GetTable(PendingConnectionActionsTableId).GetData();
			return tableData.Values.Select(x => new PendingConnectionAction(this, x));
		}

		public IEnumerable<Connection> GetConnections()
		{
			if (DmsElement.State != ElementState.Active)
			{
				return [];
			}

			var tableData = DmsElement.GetTable(ConnectionsTableId).GetData();
			return tableData.Values.Select(x => new Connection(this, x));
		}

		public bool TryGetConnection(Guid destinationEndpointId, out Connection connection)
		{
			if (DmsElement.State != ElementState.Active)
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
					connection = new Connection(this, row);
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
			if (DmsElement.State != ElementState.Active)
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
					pendingConnectionAction = new PendingConnectionAction(this, row);
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

		public IEnumerable<MediatedElementInfo> GetMediatedElements()
		{
			return DmsElement.GetTable(ElementsTableId)
				.GetData().Values
				.Select(row =>
				{
					var key = Convert.ToString(row[0]);
					var name = Convert.ToString(row[1]);

					DmsElementId.TryParse(key, out var id);

					return new MediatedElementInfo(id, name)
					{
						ConnectionHandlerScript = Convert.ToString(row[2]),
						IsEnabled = Convert.ToInt32(row[6]) == 1,
					};
				});
		}

		public IEnumerable<string> GetConnectionHandlerScriptNames()
		{
			return DmsElement.GetTable(ConnectionHandlerScriptsTableId)
				.GetData().Values
				.Select(row => Convert.ToString(row[0]));
		}

		public string GetConnectionHandlerScriptName(IDmsElement destinationElement)
		{
			if (destinationElement is null)
			{
				throw new ArgumentNullException(nameof(destinationElement));
			}

			var scriptColumn = DmsElement.GetTable(ElementsTableId).GetColumn<string>(1003);

			var script = scriptColumn.GetValue(destinationElement.DmsElementId.Value, KeyType.PrimaryKey);

			if (String.IsNullOrEmpty(script))
			{
				throw new InvalidOperationException($"No connection handler script found for element '{destinationElement.Name}' in mediation element '{Name}'.");
			}

			return script;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj is MediationElement other)
			{
				return DmsElementId == other.DmsElementId;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return DmsElementId.GetHashCode();
		}
	}
}
