namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class MediaOpsLiveApi
	{
		public MediaOpsLiveApi(Func<DMSMessage[], DMSMessage[]> messageHandler)
		{
			MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
			SlcConnectivityManagementHelper = new SlcConnectivityManagementHelper(messageHandler);
			SlcOrchestrationHelper = new SlcOrchestrationHelper(messageHandler);

			Endpoints = new EndpointRepository(SlcConnectivityManagementHelper);
			VirtualSignalGroups = new VirtualSignalGroupRepository(SlcConnectivityManagementHelper);
			Levels = new LevelRepository(SlcConnectivityManagementHelper);
			Categories = new CategoryRepository(SlcConnectivityManagementHelper);
			TransportTypes = new TransportTypeRepository(SlcConnectivityManagementHelper);
			Connections = new ConnectionRepository(SlcConnectivityManagementHelper);

			Orchestration = new OrchestrationEventRepository(SlcOrchestrationHelper);
		}

		public MediaOpsLiveApi(IEngine engine) : this(engine.SendSLNetMessages)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}
		}

		internal Func<DMSMessage[], DMSMessage[]> MessageHandler { get; }

		internal SlcConnectivityManagementHelper SlcConnectivityManagementHelper { get; }

		internal SlcOrchestrationHelper SlcOrchestrationHelper { get; }

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
