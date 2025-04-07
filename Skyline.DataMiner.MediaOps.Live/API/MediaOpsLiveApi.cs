namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
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

			Endpoints = new EndpointRepository(SlcConnectivityManagementHelper);
			VirtualSignalGroups = new VirtualSignalGroupRepository(SlcConnectivityManagementHelper);
			Levels = new LevelRepository(SlcConnectivityManagementHelper);
			Categories = new CategoryRepository(SlcConnectivityManagementHelper);
			TransportTypes = new TransportTypeRepository(SlcConnectivityManagementHelper);
			Connections = new ConnectionRepository(SlcConnectivityManagementHelper);
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

		public EndpointRepository Endpoints { get; }

		public VirtualSignalGroupRepository VirtualSignalGroups { get; }

		public LevelRepository Levels { get; }

		public CategoryRepository Categories { get; }

		public TransportTypeRepository TransportTypes { get; }

		public ConnectionRepository Connections { get; }

		public bool IsInstalled()
		{
			var moduleSettingsHelper = new ModuleSettingsHelper(MessageHandler);

			var filter = ModuleSettingsExposers.ModuleId.Equal(SlcConnectivityManagementIds.ModuleId);
			var count = moduleSettingsHelper.ModuleSettings.Count(filter);

			if (count == 0)
			{
				return false;
			}

			var definitions = SlcConnectivityManagementHelper.DomHelper.DomDefinitions.ReadAll()
				.Select(x => x.ID)
				.ToList();

			return definitions.Contains(SlcConnectivityManagementIds.Definitions.VirtualSignalGroup) &&
				   definitions.Contains(SlcConnectivityManagementIds.Definitions.Endpoint);
		}
	}
}
