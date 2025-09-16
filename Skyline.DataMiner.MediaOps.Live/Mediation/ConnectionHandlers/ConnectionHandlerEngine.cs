namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Logging;

	internal class ConnectionHandlerEngine : IConnectionHandlerEngine
	{
		internal ConnectionHandlerEngine(IEngine engine, ILogger logger)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));

			Api = new MediaOpsLiveApi(Automation.Engine.SLNetRaw);
			Api.SetEngine(engine);
			Api.SetLogger(logger);
		}

		public IEngine Engine { get; }

		public ILogger Logger { get; }

		public MediaOpsLiveApi Api { get; }

		public void Log(string message, Logging.LogType logLevel = Logging.LogType.Information)
		{
			Logger?.Log(message, logLevel);
		}

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

			Log($"Registering {connections.Count} connection updates.");

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

				// Build list with active connections to register
				var connectionChanges = new List<InterApp.Messages.ConnectionChange>();

				foreach (var connection in group)
				{
					var connectionChange = new InterApp.Messages.ConnectionChange
					{
						Time = now,
						Destination = new InterApp.Messages.EndpointInfo(connection.DestinationEndpoint),
						IsConnected = connection.IsConnected,
					};

					if (connection.SourceEndpoint != null)
					{
						connectionChange.ConnectedSource = new InterApp.Messages.EndpointInfo(connection.SourceEndpoint);
					}

					connectionChanges.Add(connectionChange);
				}

				// Log
				Log($"Notifying {connectionChanges.Count} connection changes to mediation element '{mediationElement.Name}' [{mediationElement.DmsElementId}]:\n" +
					JsonConvert.SerializeObject(connectionChanges, Formatting.Indented));

				// Send message
				var commands = InterAppCallFactory.CreateNew();

				var message = new InterApp.Messages.NotifyConnectionChangesMessage { Changes = connectionChanges };
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
