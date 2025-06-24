namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	using Connection = Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement.Connection;
	using Level = Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement.Level;

	public class MediaOpsLiveApiMock : MediaOpsLiveApi
	{
		public MediaOpsLiveApiMock(bool installDomModules = true, bool createEndpoints = true, bool createVsgs = true, bool createConnections = false)
			: this(new DomConnectionMock(), installDomModules, createEndpoints, createVsgs, createConnections)
		{
		}

		public MediaOpsLiveApiMock(Net.IConnection connection, bool installDomModules = true, bool createEndpoints = true, bool createVsgs = true, bool createConnections = false)
			: base(connection)
		{
			if (installDomModules)
			{
				var slcConnectivityManagementDomModule = new SlcConnectivityManagementDomModule();
				DomModuleInstaller.Install(connection.HandleMessages, slcConnectivityManagementDomModule, x => { });

				var slcOrchestrationDomModule = new SlcOrchestrationDomModule();
				DomModuleInstaller.Install(connection.HandleMessages, slcOrchestrationDomModule, x => { });
			}

			var category = new Category { Name = "Category 1" };
			Categories.Create(category);

			var transportTypeIP = new TransportType { Name = "IP" };
			TransportTypes.Create(transportTypeIP);

			var videoLevel = new Level { Number = 1, Name = "Video", TransportType = transportTypeIP };
			var audioLevel = new Level { Number = 2, Name = "Audio", TransportType = transportTypeIP };
			var dataLevel = new Level { Number = 3, Name = "Data", TransportType = transportTypeIP };
			Levels.CreateOrUpdate([videoLevel, audioLevel, dataLevel]);

			if (!createEndpoints)
			{
				return;
			}

			for (int i = 1; i <= 10; i++)
			{
				var videoSource1 = new Endpoint
				{
					Role = Role.Source,
					Name = $"Video Source {i}",
					TransportType = transportTypeIP,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var audioSource1 = new Endpoint
				{
					Role = Role.Source,
					Name = $"Audio Source {i}",
					TransportType = transportTypeIP,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var videoDestination1 = new Endpoint
				{
					Role = Role.Destination,
					Name = $"Video Destination {i}",
					TransportType = transportTypeIP,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var audioDestination1 = new Endpoint
				{
					Role = Role.Destination,
					Name = $"Audio Destination {i}",
					TransportType = transportTypeIP,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				Endpoints.CreateOrUpdate([videoSource1, audioSource1, videoDestination1, audioDestination1]);

				if (createVsgs)
				{
					var source1 = new VirtualSignalGroup
					{
						Role = Role.Source,
						Name = $"Source {i}",
						Description = $"Source {i}",
						Categories =
						[
							category,
						],
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
						Categories =
						[
							category,
						],
						Levels =
						[
							new LevelEndpoint(videoLevel, videoDestination1),
							new LevelEndpoint(audioLevel, audioDestination1),
						],
					};
					VirtualSignalGroups.CreateOrUpdate([source1, destination1]);
				}

				if (createConnections)
				{
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
			}

			OrchestrationJobConfiguration? job = Orchestration.GetOrCreateNewOrchestrationJobConfiguration("dd2cd5f2-ee7d-42b8-9b96-1e562d472b63");
			job.OrchestrationEvents.AddRange(WithNodes_CreateEventConfigurationInstances(10, 10));

			Orchestration.SaveEventConfigurations(job.OrchestrationEvents);
		}

		public void CreateTestConnection(Endpoint? source, Endpoint destination)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var connection = Connections.GetByDestination(destination)
				?? new Connection { Destination = destination };

			connection.ConnectedSource = source;
			connection.IsConnected = source != null;

			if (connection.PendingConnectedSource == source)
			{
				connection.PendingConnectedSource = null;
			}

			Connections.CreateOrUpdate(connection);
		}

		public void CreateTestPendingConnection(Endpoint? pendingSource, Endpoint destination)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var connection = Connections.GetByDestination(destination)
				?? new Connection { Destination = destination, IsConnected = false };

			if (connection.ConnectedSource == pendingSource)
			{
				// already connected to the pending source
				return;
			}

			connection.PendingConnectedSource = pendingSource;

			Connections.CreateOrUpdate(connection);
		}

		private IEnumerable<OrchestrationEventConfiguration> WithNodes_CreateEventConfigurationInstances(int count, int nodes)
		{
			List<Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration.Connection> connections = new List<Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration.Connection>();
			List<NodeConfiguration> nodeConfigs = new List<NodeConfiguration>();
			List<LevelMapping> levelMapping = new List<LevelMapping>
			{
				new(
					new Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration.Level("Destination",1),
					new Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration.Level("Source", 1)),
			};

			List<OrchestrationScriptArgument> scriptArguments = new List<OrchestrationScriptArgument>
			{
				new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Element, "Name", "Value"),
				new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, "Name", "Value"),
				new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, "Name", "Value"),
			};

			for (int i = 1; i <= nodes; i++)
			{
				connections.Add(new Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration.Connection
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
					EventTime = DateTime.UtcNow,
					EventType = SlcOrchestrationIds.Enums.EventType.Other,
					EventState = SlcOrchestrationIds.Enums.EventState.Confirmed,
					GlobalOrchestrationScript = "Test Script",
					GlobalOrchestrationScriptArguments = scriptArguments,
					JobReference = "dd2cd5f2-ee7d-42b8-9b96-1e562d472b63",
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
