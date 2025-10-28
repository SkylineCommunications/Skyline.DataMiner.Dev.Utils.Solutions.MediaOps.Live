namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.API.TransportTypes;
	using Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Profiles;

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

			InitializeConnectivityManagement(installDomModules, createEndpoints, createVsgs, createConnections, createElements);
			InitializeOrchestration(installDomModules);
		}

		public SimulatedDms Dms => _dms;

		public MediaOpsLiveApi Api { get; }

		public void CreateTestConnection(Endpoint source, Endpoint destination)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var mediationElement = Api.MediationElements.GetElementForEndpoint(destination);

			var simulatedElement = Dms
				.Agents[mediationElement.DmaId]
				.Elements[mediationElement.ElementId];

			var connectionsTable = simulatedElement.Tables[MediationElement.ConnectionsTableId];

			var rowKey = Convert.ToString(destination.ID);
			var row = new object[]
			{
				rowKey,
				destination.Name,
				1, // IsConnected=true
				source?.ID.ToString() ?? string.Empty,
				source?.Name ?? string.Empty,
			};

			connectionsTable.SetRow(rowKey, row);

			// Also clear any pending action
			ClearTestPendingConnectionAction(destination);
		}

		public void TestDisconnectDestination(Endpoint destination)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var mediationElement = Api.MediationElements.GetElementForEndpoint(destination);

			var simulatedElement = Dms
				.Agents[mediationElement.DmaId]
				.Elements[mediationElement.ElementId];

			var connectionsTable = simulatedElement.Tables[MediationElement.ConnectionsTableId];

			var rowKey = Convert.ToString(destination.ID);
			var row = new object[]
			{
				rowKey,
				destination.Name,
				0, // IsConnected=false
				string.Empty, // Source ID
				string.Empty, // Source Name
			};

			connectionsTable.SetRow(rowKey, row);

			// Also clear any pending action
			ClearTestPendingConnectionAction(destination);
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

			var mediationElement = Api.MediationElements.GetElementForEndpoint(destination);

			var simulatedElement = Dms
				.Agents[mediationElement.DmaId]
				.Elements[mediationElement.ElementId];

			var pendingActionsTable = simulatedElement.Tables[MediationElement.PendingConnectionActionsTableId];

			var rowKey = Convert.ToString(destination.ID);
			var row = new object[]
			{
				rowKey,
				destination.Name,
				(int)action,
				DateTime.Now.ToOADate(),
				pendingSource?.ID.ToString() ?? string.Empty,
				pendingSource?.Name ?? string.Empty,
			};

			pendingActionsTable.SetRow(rowKey, row);
		}

		public void ClearTestPendingConnectionAction(Endpoint destination)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var mediationElement = Api.MediationElements.GetElementForEndpoint(destination);

			var simulatedElement = Dms
				.Agents[mediationElement.DmaId]
				.Elements[mediationElement.ElementId];

			var pendingActionsTable = simulatedElement.Tables[MediationElement.PendingConnectionActionsTableId];

			var rowKey = Convert.ToString(destination.ID);
			pendingActionsTable.DeleteRow(rowKey);
		}

		private void InitializeConnectivityManagement(bool installDomModules, bool createEndpoints, bool createVsgs, bool createConnections, bool createElements)
		{
			var dmaId1 = 123;
			var dmaId2 = 124;

			var mediationElement1 = CreateMediationElement(dmaId1, 1000, "MediaOps Mediation 1");
			var mediationElement2 = CreateMediationElement(dmaId2, 1000, "MediaOps Mediation 2");

			if (installDomModules)
			{
				var slcConnectivityManagementDomModule = new SlcConnectivityManagementDomModule();
				DomModuleInstaller.Install(_connection.HandleMessages, slcConnectivityManagementDomModule, x => { });
			}

			var category = new Category { Name = "Category 1" };
			Api.Categories.Create(category);

			var transportTypeTSoIP = new TsoipTransportType();
			Api.TransportTypes.Create(transportTypeTSoIP);

			var videoLevel = new Level { Number = 1, Name = "Video", TransportType = transportTypeTSoIP };
			var audioLevel = new Level { Number = 2, Name = "Audio", TransportType = transportTypeTSoIP };
			var dataLevel = new Level { Number = 3, Name = "Data", TransportType = transportTypeTSoIP };
			Api.Levels.CreateOrUpdate([videoLevel, audioLevel, dataLevel]);

			const int numberOfElements = 2;

			if (createElements)
			{
				for (int eid = 1; eid <= numberOfElements; eid++)
				{
					CreateMediatedElement(dmaId1, eid, mediationElement1);
				}
			}

			const int vsgPerElement = 5;
			int vsgCounter = 0;

			if (createEndpoints)
			{
				for (int eid = 1; eid <= numberOfElements; eid++)
				{
					var elementId = new DmsElementId(123, eid);

					for (int i = 1; i <= vsgPerElement; i++)
					{
						vsgCounter++;

						var videoSource = new Endpoint(Tools.GuidFromString($"Video Source {vsgCounter}"))
						{
							Role = EndpointRole.Source,
							Name = $"Video Source {vsgCounter}",
							TransportType = transportTypeTSoIP,
							Element = elementId,
							Identifier = $"Video-{vsgCounter}",
							TransportMetadata =
							{
								new TransportMetadata(TsoipTransportType.FieldNames.SourceIp, "10.0.0.1"),
								new TransportMetadata(TsoipTransportType.FieldNames.MulticastIp, $"239.1.{vsgCounter}.1"),
								new TransportMetadata(TsoipTransportType.FieldNames.MulticastPort, "5000"),
							},
						};
						var audioSource = new Endpoint(Tools.GuidFromString($"Audio Source {vsgCounter}"))
						{
							Role = EndpointRole.Source,
							Name = $"Audio Source {vsgCounter}",
							TransportType = transportTypeTSoIP,
							Element = elementId,
							Identifier = $"Audio-{vsgCounter}",
							TransportMetadata =
							{
								new TransportMetadata(TsoipTransportType.FieldNames.SourceIp, "10.0.0.1"),
								new TransportMetadata(TsoipTransportType.FieldNames.MulticastIp, $"239.1.{vsgCounter}.2"),
								new TransportMetadata(TsoipTransportType.FieldNames.MulticastPort, "5000"),
							},
						};
						var videoDestination = new Endpoint(Tools.GuidFromString($"Video Destination {vsgCounter}"))
						{
							Role = EndpointRole.Destination,
							Name = $"Video Destination {vsgCounter}",
							TransportType = transportTypeTSoIP,
							Element = elementId,
							Identifier = $"Video-{vsgCounter}",
						};
						var audioDestination = new Endpoint(Tools.GuidFromString($"Audio Destination {vsgCounter}"))
						{
							Role = EndpointRole.Destination,
							Name = $"Audio Destination {vsgCounter}",
							TransportType = transportTypeTSoIP,
							Element = elementId,
							Identifier = $"Audio-{vsgCounter}",
						};
						Api.Endpoints.CreateOrUpdate([videoSource, audioSource, videoDestination, audioDestination]);

						if (createVsgs)
						{
							var source1 = new VirtualSignalGroup(Tools.GuidFromString($"Source {vsgCounter}"))
							{
								Role = EndpointRole.Source,
								Name = $"Source {vsgCounter}",
								Description = $"Source {vsgCounter}",
								Categories =
								[
									category,
								],
								Levels =
								[
									new LevelEndpoint(videoLevel, videoSource),
									new LevelEndpoint(audioLevel, audioSource),
								],
							};
							var destination1 = new VirtualSignalGroup(Tools.GuidFromString($"Destination {vsgCounter}"))
							{
								Role = EndpointRole.Destination,
								Name = $"Destination {vsgCounter}",
								Description = $"Destination {vsgCounter}",
								Categories =
								[
									category,
								],
								Levels =
								[
									new LevelEndpoint(videoLevel, videoDestination),
									new LevelEndpoint(audioLevel, audioDestination),
								],
							};
							Api.VirtualSignalGroups.CreateOrUpdate([source1, destination1]);
						}

						if (createConnections)
						{
							CreateTestConnection(videoSource, videoDestination);
							CreateTestConnection(audioSource, audioDestination);
						}
					}
				}
			}
		}

		private SimulatedElement CreateMediationElement(int dmaId, int elementId, string name)
		{
			var element = Dms.GetOrCreateAgent(dmaId)
				.CreateElement(elementId, name, Constants.MediationProtocolName);

			element.CreateTable(MediationElement.ElementsTableId);
			element.CreateTable(MediationElement.ConnectionHandlerScriptsTableId);
			element.CreateTable(MediationElement.ConnectionsTableId);
			element.CreateTable(MediationElement.PendingConnectionActionsTableId);

			var connectionHandlerScriptsTable = element.Tables[MediationElement.ConnectionHandlerScriptsTableId];
			var simulatorConnectionHandlerScript = "Simulator_ConnectionHandler";
			connectionHandlerScriptsTable.SetRow(simulatorConnectionHandlerScript, [simulatorConnectionHandlerScript]);

			return element;
		}

		private void CreateMediatedElement(int dmaId, int elementId, SimulatedElement mediationElement)
		{
			var element = Dms.GetOrCreateAgent(dmaId)
				.CreateElement(elementId, $"MediaOps Simulator {elementId}", "Skyline MediaOps Simulator");

			var mediatedElementsTable = mediationElement.Tables[MediationElement.ElementsTableId];

			var mediatedElementKey = $"{dmaId}/{elementId}";

			var mediatedElementRow = new object[7]
			{
				mediatedElementKey,
				element.Name,
				"Simulator_ConnectionHandler",
				null,
				null,
				null,
				1, // Enabled
			};

			mediatedElementsTable.SetRow(mediatedElementKey, mediatedElementRow);
		}

		private void InitializeOrchestration(bool installDomModules)
		{
			Dms.Agents[123].CreateElement(1001, "Orchestration Dummy Instance 1", "Protocol", "Production");
			Dms.Agents[124].CreateElement(1001, "Orchestration Dummy Instance 2", "Protocol", "Production");

			Dms.AddProfileParameter("IndividualProfileParam_Int", new Guid("986528dc-78af-4b09-b1c1-11dac21744b1"), Parameter.ParameterType.Number);
			Dms.AddProfileParameter("IndividualProfileParam_String", new Guid("b0e37ff1-fe56-4bd7-b108-9e8c992eb6d9"), Parameter.ParameterType.Text);
			Dms.AddProfileParameter("DefinitionProfileParam_Int", new Guid("70b3e8fc-7a6d-4c8d-bbe7-ab806625081e"), Parameter.ParameterType.Number);
			Dms.AddProfileParameter("DefinitionProfileParam_String", new Guid("864d57be-4c26-4754-8da2-0cc0ba50bf6f"), Parameter.ParameterType.Text);

			Dms.AddProfileDefinition(
				"Definition 1",
				new Guid("94fa7d96-8cb3-4bdd-a968-dd1192683165"),
				new List<Guid>
				{
					new Guid("70b3e8fc-7a6d-4c8d-bbe7-ab806625081e"),
					new Guid("864d57be-4c26-4754-8da2-0cc0ba50bf6f"),
				});

			Dms.AddProfileInstance(
				"Instance 1",
				new Guid("279eea1b-2702-4710-be01-ad1d80dd4b9d"),
				new Guid("94fa7d96-8cb3-4bdd-a968-dd1192683165"),
				new Dictionary<Guid, object>()
				{
					{ new Guid("70b3e8fc-7a6d-4c8d-bbe7-ab806625081e"), 500},
					{ new Guid("864d57be-4c26-4754-8da2-0cc0ba50bf6f"), "Hello"},
				});

			Dms.AddScript("Script_Success", new List<string>(), new List<string>());
			Dms.AddScript("Script_Fail", new List<string>(), new List<string>());
			Dms.AddScript(
				"OrchestrationScript",
				new List<string> { "InputParam" },
				new List<string> { "InputDummy" },
				new ScriptInfo
				{
					ProfileParameters =
					{
						{ "IndividualProfileParam_Int", new Guid("986528dc-78af-4b09-b1c1-11dac21744b1") },
						{ "IndividualProfileParam_String", new Guid("b0e37ff1-fe56-4bd7-b108-9e8c992eb6d9") },
						{ "DefinitionProfileParam_Int", new Guid("70b3e8fc-7a6d-4c8d-bbe7-ab806625081e") },
						{ "DefinitionProfileParam_String", new Guid("864d57be-4c26-4754-8da2-0cc0ba50bf6f") },
					},
					ProfileDefinitions =
					{
						new Guid("94fa7d96-8cb3-4bdd-a968-dd1192683165"),
					},
				});

			if (installDomModules)
			{
				var slcOrchestrationDomModule = new SlcOrchestrationDomModule();
				DomModuleInstaller.Install(_connection.HandleMessages, slcOrchestrationDomModule, x => { });
			}

			OrchestrationJobConfiguration job = Api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration("dd2cd5f2-ee7d-42b8-9b96-1e562d472b63");
			Guid jobInfoReference = job.JobInfo.ID;
			job.OrchestrationEvents.AddRange(WithNodes_CreateEventConfigurationInstances(10, 10, jobInfoReference));

			Api.Orchestration.SaveOrchestrationJobConfiguration(job);
		}

		private IEnumerable<OrchestrationEventConfiguration> WithNodes_CreateEventConfigurationInstances(int count, int nodes, Guid jobReferenceId)
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
				});
			}

			List<OrchestrationEventConfiguration> orchestrationEventConfigurations = new List<OrchestrationEventConfiguration>();

			for (int i = 1; i <= count; i++)
			{
				orchestrationEventConfigurations.Add(new OrchestrationEventConfiguration
				{
					Name = $"Test Event {i}",
					EventTime = DateTime.UtcNow + TimeSpan.FromHours(1),
					EventType = EventType.Other,
					EventState = EventState.Draft,
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
