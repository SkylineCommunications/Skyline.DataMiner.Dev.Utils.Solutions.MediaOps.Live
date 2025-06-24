namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	internal class ConnectionHandlerEngine : IConnectionHandlerEngine
	{
		private readonly object _lock = new object();

		public ConnectionHandlerEngine(IEngine engine)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));

			Api = new MediaOpsLiveApi(engine.GetUserConnection());
		}

		public IEngine Engine { get; }

		public MediaOpsLiveApi Api { get; }

		protected SlcConnectivityManagementHelper Helper => Api.SlcConnectivityManagementHelper;

		public void RegisterConnection(ConnectionInfo connectionInfo)
		{
			if (connectionInfo == null)
			{
				throw new ArgumentNullException(nameof(connectionInfo));
			}

			RegisterConnections([connectionInfo]);
		}

		public void RegisterConnections(ICollection<ConnectionInfo> connectionInfos)
		{
			if (connectionInfos == null)
			{
				throw new ArgumentNullException(nameof(connectionInfos));
			}

			UpdateDomConnections(connectionInfos);
			NotifyPendingConnectionActions(connectionInfos);
		}

		private void UpdateDomConnections(ICollection<ConnectionInfo> connectionInfos)
		{
			lock (_lock)
			{
				var destinationEndpointIds = connectionInfos.Select(x => x.DestinationEndpoint.ID).ToList();
				var connectionsByDestination = Helper.GetConnectionsForDestinations(destinationEndpointIds);

				var updatedConnections = new List<ConnectionInstance>();

				foreach (var connectionInfo in connectionInfos)
				{
					var hasChanges = false;

					if (!connectionsByDestination.TryGetValue(connectionInfo.DestinationEndpoint.ID, out var connection))
					{
						connection = CreateNewConnection(connectionInfo.DestinationEndpoint.ID);
						connectionsByDestination.Add(connectionInfo.DestinationEndpoint.ID, connection);
						hasChanges = true;
					}

					hasChanges |= ApplyConnectionUpdate(connection, connectionInfo.SourceEndpoint?.ID);

					if (hasChanges)
					{
						updatedConnections.Add(connection);
					}
				}

				if (updatedConnections.Count > 0)
				{
					Engine.GenerateInformation($"Updating {updatedConnections.Count} connections...");
					Helper.DomHelper.DomInstances.CreateOrUpdateInBatches(updatedConnections.Select(x => x.ToInstance())).ThrowOnFailure();
				}
			}
		}

		private static ConnectionInstance CreateNewConnection(Guid destinationEndpointId)
		{
			return new ConnectionInstance
			{
				ConnectionInfo = new ConnectionInfoSection
				{
					Destination = destinationEndpointId,
					IsConnected = false,
				},
			};
		}

		private static bool ApplyConnectionUpdate(ConnectionInstance connection, Guid? sourceEndpointId)
		{
			var wasConnected = connection.ConnectionInfo.IsConnected;
			var previousSource = connection.ConnectionInfo.ConnectedSource;
			var previousPendingSource = connection.ConnectionInfo.PendingConnectedSource;

			connection.ConnectionInfo.IsConnected = sourceEndpointId != null;
			connection.ConnectionInfo.ConnectedSource = sourceEndpointId;
			connection.ConnectionInfo.PendingConnectedSource = null;

			return wasConnected != connection.ConnectionInfo.IsConnected ||
				   previousSource != connection.ConnectionInfo.ConnectedSource ||
				   previousPendingSource != null;
		}

		private void NotifyPendingConnectionActions(ICollection<ConnectionInfo> connectionInfos)
		{
			var now = DateTimeOffset.Now;
			var dms = Engine.GetDms();

			var mediationElementMap = MediationElement.GetMediationElements(dms, connectionInfos.Select(x => x.DestinationEndpoint));

			foreach (var group in connectionInfos
				.Where(x => mediationElementMap.ContainsKey(x.DestinationEndpoint))
				.GroupBy(x => mediationElementMap[x.DestinationEndpoint]))
			{
				var mediationElement = group.Key;

				var requests = new List<InterApp.Messages.ClearPendingConnectionActionRequest>();

				foreach (var connection in group)
				{
					var request = new InterApp.Messages.ClearPendingConnectionActionRequest
					{
						StartTime = now,
						Destination = new InterApp.Messages.Endpoint
						{
							ID = connection.DestinationEndpoint.ID,
							Name = connection.DestinationEndpoint.Name,
						},
					};

					if (connection.SourceEndpoint != null)
					{
						request.ConnectedSource = new InterApp.Messages.Endpoint
						{
							ID = connection.SourceEndpoint.ID,
							Name = connection.SourceEndpoint.Name,
						};
					}

					requests.Add(request);
				}

				var commands = InterAppCallFactory.CreateNew();

				var message = new InterApp.Messages.ClearPendingConnectionActionMessage { Requests = requests };
				commands.Messages.Add(message);

				commands.Send(
					Engine.GetUserConnection(),
					mediationElement.DmaId,
					mediationElement.ElementId,
					9000000,
					[typeof(InterApp.Messages.NotifyPendingConnectionActionMessage)]);
			}
		}
	}
}
