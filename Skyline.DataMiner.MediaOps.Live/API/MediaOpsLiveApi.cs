namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class MediaOpsLiveApi
	{
		public MediaOpsLiveApi(ICommunication communication)
		{
			if (communication is null)
			{
				throw new ArgumentNullException(nameof(communication));
			}

			Dms = DmsFactory.CreateDms(communication);
			MessageHandler = communication.SendMessages;
			SlcConnectivityManagementHelper = new SlcConnectivityManagementHelper(communication);
			SlcOrchestrationHelper = new SlcOrchestrationHelper(communication);

			Endpoints = new EndpointRepository(SlcConnectivityManagementHelper);
			VirtualSignalGroups = new VirtualSignalGroupRepository(SlcConnectivityManagementHelper);
			Levels = new LevelRepository(SlcConnectivityManagementHelper);
			Categories = new CategoryRepository(SlcConnectivityManagementHelper);
			TransportTypes = new TransportTypeRepository(SlcConnectivityManagementHelper);
			Connections = new ConnectionRepository(SlcConnectivityManagementHelper);

			Orchestration = new OrchestrationEventRepository(SlcOrchestrationHelper, this);
		}

		public MediaOpsLiveApi(IConnection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			Dms = connection.GetDms();
			MessageHandler = connection.HandleMessages;
			SlcConnectivityManagementHelper = new SlcConnectivityManagementHelper(connection);
			SlcOrchestrationHelper = new SlcOrchestrationHelper(connection);

			Endpoints = new EndpointRepository(SlcConnectivityManagementHelper);
			VirtualSignalGroups = new VirtualSignalGroupRepository(SlcConnectivityManagementHelper);
			Levels = new LevelRepository(SlcConnectivityManagementHelper);
			Categories = new CategoryRepository(SlcConnectivityManagementHelper);
			TransportTypes = new TransportTypeRepository(SlcConnectivityManagementHelper);
			Connections = new ConnectionRepository(SlcConnectivityManagementHelper);

			Orchestration = new OrchestrationEventRepository(SlcOrchestrationHelper, this);
		}

		public MediaOpsLiveApi(IEngine engine) : this(engine?.GetUserConnection())
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		internal Func<DMSMessage[], DMSMessage[]> MessageHandler { get; }

		internal SlcConnectivityManagementHelper SlcConnectivityManagementHelper { get; }

		internal SlcOrchestrationHelper SlcOrchestrationHelper { get; }

		internal IEngine Engine { get; }

		internal IDms Dms { get; }

		public EndpointRepository Endpoints { get; }

		public VirtualSignalGroupRepository VirtualSignalGroups { get; }

		public LevelRepository Levels { get; }

		public CategoryRepository Categories { get; }

		public TransportTypeRepository TransportTypes { get; }

		public ConnectionRepository Connections { get; }

		public OrchestrationEventRepository Orchestration { get; }

		public bool IsInstalled()
		{
			var moduleSettingsHelper = new ModuleSettingsHelper(MessageHandler);

			var filter = ModuleSettingsExposers.ModuleId.Equal(SlcConnectivityManagementIds.ModuleId).OR(ModuleSettingsExposers.ModuleId.Equal(SlcOrchestrationIds.ModuleId));
			var count = moduleSettingsHelper.ModuleSettings.Count(filter);

			if (count == 0)
			{
				return false;
			}

			var connectivityManagementDefinitions = SlcConnectivityManagementHelper.DomHelper.DomDefinitions.ReadAll()
				.Select(x => x.ID)
				.ToList();

			var orchestrationDefinitions = SlcOrchestrationHelper.DomHelper.DomDefinitions.ReadAll()
				.Select(x => x.ID)
				.ToList();

			return connectivityManagementDefinitions.Contains(SlcConnectivityManagementIds.Definitions.VirtualSignalGroup) &&
				   connectivityManagementDefinitions.Contains(SlcConnectivityManagementIds.Definitions.Endpoint) &&
				   orchestrationDefinitions.Contains(SlcOrchestrationIds.Definitions.OrchestrationEvent);
		}
	}
}
