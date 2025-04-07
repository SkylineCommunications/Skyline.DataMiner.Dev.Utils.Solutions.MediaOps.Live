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
			ConnectivityManagerHelper = new SlcConnectivityManagementHelper(messageHandler);
			OrchestrationHelper = new SlcOrchestrationHelper(messageHandler);

			Endpoints = new EndpointRepository(ConnectivityManagerHelper);
			VirtualSignalGroups = new VirtualSignalGroupRepository(ConnectivityManagerHelper);
			Levels = new LevelRepository(ConnectivityManagerHelper);
			Categories = new CategoryRepository(ConnectivityManagerHelper);
			TransportTypes = new TransportTypeRepository(ConnectivityManagerHelper);
			Connections = new ConnectionRepository(ConnectivityManagerHelper);

			OrchestrationEvents = new OrchestrationEventRepository(OrchestrationHelper);
		}

		public MediaOpsLiveApi(IEngine engine) : this(engine.SendSLNetMessages)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}
		}

		public MediaOpsLiveApi(SlcConnectivityManagementHelper helper)
		{
			ConnectivityManagerHelper = helper ?? throw new ArgumentNullException(nameof(helper));

			Endpoints = new EndpointRepository(helper);
			VirtualSignalGroups = new VirtualSignalGroupRepository(helper);
			Levels = new LevelRepository(helper);
			Categories = new CategoryRepository(helper);
			TransportTypes = new TransportTypeRepository(helper);
			Connections = new ConnectionRepository(helper);
		}

		internal Func<DMSMessage[], DMSMessage[]> MessageHandler { get; }

		internal SlcConnectivityManagementHelper ConnectivityManagerHelper { get; }

		internal SlcOrchestrationHelper OrchestrationHelper { get; }

		public EndpointRepository Endpoints { get; }

		public VirtualSignalGroupRepository VirtualSignalGroups { get; }

		public LevelRepository Levels { get; }

		public CategoryRepository Categories { get; }

		public TransportTypeRepository TransportTypes { get; }

		public ConnectionRepository Connections { get; }

		public OrchestrationEventRepository OrchestrationEvents { get; }

		public bool IsInstalled()
		{
			var moduleSettingsHelper = new ModuleSettingsHelper(MessageHandler);

			var filter = ModuleSettingsExposers.ModuleId.Equal(SlcConnectivityManagementIds.ModuleId);
			var count = moduleSettingsHelper.ModuleSettings.Count(filter);

			if (count == 0)
			{
				return false;
			}

			var connectivityManagementDefinitions = ConnectivityManagerHelper.DomHelper.DomDefinitions.ReadAll()
				.Select(x => x.ID)
				.ToList();

			var orchestrationDefinitions = OrchestrationHelper.DomHelper.DomDefinitions.ReadAll()
				.Select(x => x.ID)
				.ToList();

			return connectivityManagementDefinitions.Contains(SlcConnectivityManagementIds.Definitions.VirtualSignalGroup) &&
				   connectivityManagementDefinitions.Contains(SlcConnectivityManagementIds.Definitions.Endpoint) &&
				   orchestrationDefinitions.Contains(SlcOrchestrationIds.Definitions.OrchestrationEvent);
		}
	}
}
