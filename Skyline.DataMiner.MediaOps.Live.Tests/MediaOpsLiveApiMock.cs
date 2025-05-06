namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using API.Objects.SlcConnectivityManagement;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	using Connection = API.Objects.SlcConnectivityManagement.Connection;
	using Level = API.Objects.SlcConnectivityManagement.Level;

	public class MediaOpsLiveApiMock : MediaOpsLiveApi
	{
		public MediaOpsLiveApiMock() : base(Engine.SLNetRaw)
		{
			CreateMessageHandler(out var messageHandler);
			MessageHandler = messageHandler;

			var transportType = new TransportType { Name = "IP" };
			TransportTypes.Create(transportType);

			var videoLevel = new Level { Number = 1, Name = "Video", TransportType = transportType };
			var audioLevel = new Level { Number = 2, Name = "Audio", TransportType = transportType };
			var dataLevel = new Level { Number = 3, Name = "Data", TransportType = transportType };
			Levels.CreateOrUpdate([videoLevel, audioLevel, dataLevel]);

			for (int i = 1; i <= 10; i++)
			{
				var videoSource1 = new Endpoint
				{
					Role = Role.Source,
					Name = $"Video Source {i}",
					TransportType = transportType,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var audioSource1 = new Endpoint
				{
					Role = Role.Source,
					Name = $"Audio Source {i}",
					TransportType = transportType,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var videoDestination1 = new Endpoint
				{
					Role = Role.Destination,
					Name = $"Video Destination {i}",
					TransportType = transportType,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var audioDestination1 = new Endpoint
				{
					Role = Role.Destination,
					Name = $"Audio Destination {i}",
					TransportType = transportType,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				Endpoints.CreateOrUpdate([videoSource1, audioSource1, videoDestination1, audioDestination1]);

				var source1 = new VirtualSignalGroup
				{
					Role = Role.Source,
					Name = $"Source {i}",
					Description = $"Source {i}",
					Levels =
					[
						new LevelEndpoint(videoLevel, videoSource1),
						new LevelEndpoint(audioLevel, audioSource1),
					],
				};
				var destination1 = new VirtualSignalGroup
				{
					Role = Role.Destination,
					Name = $"Destination {i}",
					Description = $"Destination {i}",
					Levels =
					[
						new LevelEndpoint(videoLevel, videoDestination1),
						new LevelEndpoint(audioLevel, audioDestination1),
					],
				};
				VirtualSignalGroups.CreateOrUpdate([source1, destination1]);

				var connection1 = new Connection
				{
					Destination = videoDestination1,
					ConnectedSource = videoSource1,
					IsConnected = true,
				};
				var connection2 = new Connection
				{
					Destination = audioDestination1,
					ConnectedSource = audioSource1,
					IsConnected = true,
				};
				Connections.CreateOrUpdate([connection1, connection2]);
			}

			var job = new OrchestrationJobConfiguration("dd2cd5f2-ee7d-42b8-9b96-1e562d472b63", WithNodes_CreateEventConfigurationInstances(10, 10));
			Orchestration.CreateOrUpdateOrchestrationJobConfiguration(job);
		}

		public DomSLNetMessageHandler MessageHandler { get; }

		private static DomSLNetMessageHandler CreateMessageHandler(out DomSLNetMessageHandler handler)
		{
			handler = new DomSLNetMessageHandler();
			return handler;
		}

		private IEnumerable<OrchestrationEventConfiguration> WithNodes_CreateEventConfigurationInstances(int count, int nodes)
		{
			List<Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration.Connection> connections = new List<Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration.Connection>();
			List<NodeConfiguration> nodeConfigs = new List<NodeConfiguration>();
			List<LevelMapping> levelMapping = new List<LevelMapping>
			{
				new LevelMapping
				{
					Destination = new Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration.Level
					{
						Name = "Destination",
						Number = 1,
					},
					Source = new Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration.Level
					{
						Name = "Source",
						Number = 1,
					},
				},
			};

			List<OrchestrationScriptArgument> scriptArguments = new List<OrchestrationScriptArgument>
			{
				new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Element, "Name", "Value"),
				new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, "Name", "Value"),
				new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, "Name", "Value"),
			};

			for (int i = 1; i <= nodes; i++)
			{
				connections.Add(new Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration.Connection
				{
					DestinationNodeId = "1",
					DestinationVsg = Guid.NewGuid(),
					SourceVsg = Guid.NewGuid(),
					SourceNodeId = "1",
					LevelMappings = levelMapping,
				});

				nodeConfigs.Add(new NodeConfiguration
				{
					NodeId = "1",
					NodeLabel = "Node Label",
					OrchestrationScriptName = "OrchestrationScript",
					OrchestrationScriptArguments = scriptArguments,
				});
			}

			List<OrchestrationEventConfiguration> orchestrationEventConfigurations = new List<OrchestrationEventConfiguration>();

			for (int i = 1; i <= count; i++)
			{
				orchestrationEventConfigurations.Add(new OrchestrationEventConfiguration
				{
					Name = $"Test Event {i}",
					EventTime = DateTime.Now,
					EventType = SlcOrchestrationIds.Enums.EventType.Other,
					EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
					GlobalOrchestrationScript = "Test Script",
					GlobalOrchestrationScriptArguments = scriptArguments,
					Configuration =
					{
						Connections = connections,
						NodeConfigurations = nodeConfigs,
					},
				});
			}

			return orchestrationEventConfigurations;
		}
	}
}
