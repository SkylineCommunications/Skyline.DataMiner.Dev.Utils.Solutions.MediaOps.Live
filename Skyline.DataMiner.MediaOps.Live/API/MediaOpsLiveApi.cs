namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Logging;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class MediaOpsLiveApi
	{
		public MediaOpsLiveApi(IConnection connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));

			SlcConnectivityManagementHelper = new SlcConnectivityManagementHelper(connection);
			SlcOrchestrationHelper = new SlcOrchestrationHelper(connection);

			ConnectionsHelper = new TakeHelper(this);
			MediationElements = new MediationElements(this);

			Endpoints = new EndpointRepository(SlcConnectivityManagementHelper, connection);
			VirtualSignalGroups = new VirtualSignalGroupRepository(SlcConnectivityManagementHelper, connection);
			Levels = new LevelRepository(SlcConnectivityManagementHelper, connection);
			Categories = new CategoryRepository(SlcConnectivityManagementHelper, connection);
			TransportTypes = new TransportTypeRepository(SlcConnectivityManagementHelper, connection);

			Orchestration = new OrchestrationEventRepository(SlcOrchestrationHelper, this);
		}

		protected internal IConnection Connection { get; }

		protected internal ILogger Logger { get; private set; }

		internal SlcConnectivityManagementHelper SlcConnectivityManagementHelper { get; }

		internal SlcOrchestrationHelper SlcOrchestrationHelper { get; }

		internal TakeHelper ConnectionsHelper { get; }

		internal MediationElements MediationElements { get; }

		public EndpointRepository Endpoints { get; }

		public VirtualSignalGroupRepository VirtualSignalGroups { get; }

		public LevelRepository Levels { get; }

		public CategoryRepository Categories { get; }

		public TransportTypeRepository TransportTypes { get; }

		public OrchestrationEventRepository Orchestration { get; }

		public void SetLogger(ILogger logger)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public IDms GetDms()
		{
			return Connection.GetDms();
		}

		public bool IsInstalled()
		{
			var moduleSettingsHelper = new ModuleSettingsHelper(Connection.HandleMessages);

			var filter = new ORFilterElement<ModuleSettings>(
				ModuleSettingsExposers.ModuleId.Equal(SlcConnectivityManagementIds.ModuleId),
				ModuleSettingsExposers.ModuleId.Equal(SlcOrchestrationIds.ModuleId));

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
