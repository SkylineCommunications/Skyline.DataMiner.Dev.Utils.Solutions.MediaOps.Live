namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;

	internal class ConnectionHandlerEngine : IConnectionHandlerEngine
	{
		public ConnectionHandlerEngine(IEngine engine)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));

			Api = new MediaOpsLiveApi(engine.GetUserConnection());
		}

		public IEngine Engine { get; }

		public MediaOpsLiveApi Api { get; }

		protected SlcConnectivityManagementHelper Helper => Api.SlcConnectivityManagementHelper;

		public void RegisterConnection(ConnectionUpdate connection)
		{
			if (connection == null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			RegisterConnections([connection]);
		}

		public void RegisterConnections(ICollection<ConnectionUpdate> connections)
		{
			if (connections == null)
			{
				throw new ArgumentNullException(nameof(connections));
			}

			Engine.GenerateInformation($"Registering {connections.Count} connections...");

			NotifyConnectionChanges(connections);
		}

		private void NotifyConnectionChanges(ICollection<ConnectionUpdate> connections)
		{
			var now = DateTimeOffset.Now;

			var mediationElementMap = Api.MediationElements.GetMediationElements(
				connections.Select(x => x.DestinationEndpoint));

			foreach (var group in connections
				.Where(x => mediationElementMap.ContainsKey(x.DestinationEndpoint))
				.GroupBy(x => mediationElementMap[x.DestinationEndpoint]))
			{
				var mediationElement = group.Key;

				var requests = new List<InterApp.Messages.ConnectionChange>();

				foreach (var connection in group)
				{
					var request = new InterApp.Messages.ConnectionChange
					{
						Time = now,
						Destination = new InterApp.Messages.EndpointInfo(connection.DestinationEndpoint),
						IsConnected = connection.IsConnected,
					};

					if (connection.SourceEndpoint != null)
					{
						request.ConnectedSource = new InterApp.Messages.EndpointInfo(connection.SourceEndpoint);
					}

					requests.Add(request);
				}

				var commands = InterAppCallFactory.CreateNew();

				var message = new InterApp.Messages.NotifyConnectionChangesMessage { Changes = requests };
				commands.Messages.Add(message);

				commands.Send(
					Api.Connection,
					mediationElement.DmaId,
					mediationElement.ElementId,
					9000000,
					[typeof(InterApp.Messages.NotifyPendingConnectionActionMessage)]);
			}
		}
	}
}
