namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Logging;

	internal class ConnectionHandlerEngine : IConnectionHandlerEngine
	{
		private readonly Lazy<IDms> _lazyDms;

		internal ConnectionHandlerEngine(IEngine engine, ILogger logger)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));

			Api = new MediaOpsLiveApi(Automation.Engine.SLNetRaw, logger);
			Api.SetEngine(engine);

			_lazyDms = new Lazy<IDms>(engine.GetDms);
		}

		public IEngine Engine { get; }

		public ILogger Logger { get; }

		public MediaOpsLiveApi Api { get; }

		public IDms Dms => _lazyDms.Value;

		public void Log(string message, LogLevel logLevel = LogLevel.Information)
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
