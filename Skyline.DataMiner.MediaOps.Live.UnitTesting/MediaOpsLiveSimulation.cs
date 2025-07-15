namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.Net;

	using Level = Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement.Level;

	public class MediaOpsLiveSimulation
	{
		private readonly SimulatedDms _dms;
		private readonly IConnection _connection;

		public MediaOpsLiveSimulation(bool installDomModules = true, bool createEndpoints = true, bool createVsgs = true, bool createConnections = false, bool createElements = true)
		{
			_dms = new SimulatedDms();
			_connection = Dms.CreateConnection();

			Api = new MediaOpsLiveApi(_connection);

			Initialize(installDomModules, createEndpoints, createVsgs, createConnections, createElements);
		}

		public SimulatedDms Dms => _dms;

		public MediaOpsLiveApi Api { get; }

		public void CreateTestConnection(Endpoint source, Endpoint destination)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			ClearTestPendingConnectionAction(destination);

			var mediationElement = Api.MediationElements.GetMediationElement(destination);

			var connectionsTable = Dms
				.Agents[mediationElement.DmaId]
				.Elements[mediationElement.ElementId]
				.Tables[MediationElement.ConnectionsTableId];

			var rowKey = Convert.ToString(destination.ID);
			var row = new object[]
			{
				rowKey,
				destination.Name,
				source != null ? 1 : 0, // IsConnected
				source?.ID.ToString() ?? String.Empty,
				source?.Name ?? String.Empty,
			};

			connectionsTable.SetRow(rowKey, row);
		}

		public void CreateTestPendingConnectionAction(
			Endpoint pendingSource,
			Endpoint destination,
			PendingConnectionActionType action = PendingConnectionActionType.Connect)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var mediationElement = Api.MediationElements.GetMediationElement(destination);

			var pendingActionsTable = Dms
				.Agents[mediationElement.DmaId]
				.Elements[mediationElement.ElementId]
				.Tables[MediationElement.PendingConnectionActionsTableId];

			var rowKey = Convert.ToString(destination.ID);
			var row = new object[]
			{
				rowKey,
				destination.Name,
				(int)action,
				DateTime.Now.ToOADate(),
				pendingSource?.ID.ToString() ?? String.Empty,
				pendingSource?.Name ?? String.Empty,
			};

			pendingActionsTable.SetRow(rowKey, row);
		}

		public void ClearTestPendingConnectionAction(Endpoint destination)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var mediationElement = Api.MediationElements.GetMediationElement(destination);

			var pendingActionsTable = Dms
				.Agents[mediationElement.DmaId]
				.Elements[mediationElement.ElementId]
				.Tables[MediationElement.PendingConnectionActionsTableId];

			var rowKey = Convert.ToString(destination.ID);
			pendingActionsTable.DeleteRow(rowKey);
		}

		private void Initialize(bool installDomModules, bool createEndpoints, bool createVsgs, bool createConnections, bool createElements)
		{
			CreateMediationElement(123, 1000, "MediaOps Mediation 1");
			CreateMediationElement(124, 1000, "MediaOps Mediation 1");

			if (installDomModules)
			{
				var slcConnectivityManagementDomModule = new SlcConnectivityManagementDomModule();
				DomModuleInstaller.Install(_connection.HandleMessages, slcConnectivityManagementDomModule, x => { });

				var slcOrchestrationDomModule = new SlcOrchestrationDomModule();
				DomModuleInstaller.Install(_connection.HandleMessages, slcOrchestrationDomModule, x => { });
			}

			var category = new Category { Name = "Category 1" };
			Api.Categories.Create(category);

			var transportTypeIP = new TransportType { Name = "IP" };
			Api.TransportTypes.Create(transportTypeIP);

			var videoLevel = new Level { Number = 1, Name = "Video", TransportType = transportTypeIP };
			var audioLevel = new Level { Number = 2, Name = "Audio", TransportType = transportTypeIP };
			var dataLevel = new Level { Number = 3, Name = "Data", TransportType = transportTypeIP };
			Api.Levels.CreateOrUpdate([videoLevel, audioLevel, dataLevel]);

			for (int i = 1; i <= 10; i++)
			{
				if (createElements)
				{
					Dms.GetOrCreateAgent(123)
						.CreateElement(i, $"MediaOps Simulator {i}", "Skyline MediaOps Simulator");
				}

				if (createEndpoints)
				{
					var videoSource1 = new Endpoint(Tools.GuidFromString($"Video Source {i}"))
					{
						Role = Role.Source,
						Name = $"Video Source {i}",
						TransportType = transportTypeIP,
						Element = $"123/{i}",
						Identifier = $"Key-{i}",
					};
					var audioSource1 = new Endpoint(Tools.GuidFromString($"Audio Source {i}"))
					{
						Role = Role.Source,
						Name = $"Audio Source {i}",
						TransportType = transportTypeIP,
						Element = $"123/{i}",
						Identifier = $"Key-{i}",
					};
					var videoDestination1 = new Endpoint(Tools.GuidFromString($"Video Destination {i}"))
					{
						Role = Role.Destination,
						Name = $"Video Destination {i}",
						TransportType = transportTypeIP,
						Element = $"123/{i}",
						Identifier = $"Key-{i}",
					};
					var audioDestination1 = new Endpoint(Tools.GuidFromString($"Audio Destination {i}"))
					{
						Role = Role.Destination,
						Name = $"Audio Destination {i}",
						TransportType = transportTypeIP,
						Element = $"123/{i}",
						Identifier = $"Key-{i}",
					};
					Api.Endpoints.CreateOrUpdate([videoSource1, audioSource1, videoDestination1, audioDestination1]);

					if (createVsgs)
					{
						var source1 = new VirtualSignalGroup(Tools.GuidFromString($"Source {i}"))
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
						var destination1 = new VirtualSignalGroup(Tools.GuidFromString($"Destination {i}"))
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
						Api.VirtualSignalGroups.CreateOrUpdate([source1, destination1]);
					}

					if (createConnections)
					{
						CreateTestConnection(videoSource1, videoDestination1);
						CreateTestConnection(audioSource1, audioDestination1);
					}
				}
			}

			OrchestrationJobConfiguration job = Api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration("dd2cd5f2-ee7d-42b8-9b96-1e562d472b63");
			job.OrchestrationEvents.AddRange(WithNodes_CreateEventConfigurationInstances(10, 10));

			Api.Orchestration.SaveEventConfigurations(job.OrchestrationEvents);
		}

		private void CreateMediationElement(int dmaId, int elementId, string name)
		{
			var element = Dms.GetOrCreateAgent(dmaId)
				.CreateElement(elementId, name, "Skyline MediaOps Mediation");

			element.CreateStandaloneParameter(50);
			element.CreateStandaloneParameter(51);

			element.CreateTable(MediationElement.ConnectionHandlerScriptsTableId);
			element.CreateTable(MediationElement.PendingConnectionActionsTableId);
			element.CreateTable(MediationElement.ConnectionsTableId);
		}

		private IEnumerable<OrchestrationEventConfiguration> WithNodes_CreateEventConfigurationInstances(int count, int nodes)
		{
			List<API.Objects.Orchestration.Connection> connections = new List<API.Objects.Orchestration.Connection>();
			List<NodeConfiguration> nodeConfigs = new List<NodeConfiguration>();
			List<LevelMapping> levelMapping = new List<LevelMapping>
			{
				new(
					new API.Objects.Orchestration.Level("Destination",1),
					new API.Objects.Orchestration.Level("Source", 1)),
			};

			List<OrchestrationScriptArgument> scriptArguments = new List<OrchestrationScriptArgument>
			{
				new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Element, "Name", "Value"),
				new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, "Name", "Value"),
				new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, "Name", "Value"),
			};

			for (int i = 1; i <= nodes; i++)
			{
				connections.Add(new API.Objects.Orchestration.Connection
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
