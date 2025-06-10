namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
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

			Endpoints = new EndpointRepository(SlcConnectivityManagementHelper, connection);
			VirtualSignalGroups = new VirtualSignalGroupRepository(SlcConnectivityManagementHelper, connection);
			Levels = new LevelRepository(SlcConnectivityManagementHelper, connection);
			Categories = new CategoryRepository(SlcConnectivityManagementHelper, connection);
			TransportTypes = new TransportTypeRepository(SlcConnectivityManagementHelper, connection);
			Connections = new ConnectionRepository(SlcConnectivityManagementHelper, connection);
		}

		public MediaOpsLiveApi(IEngine engine) : this(engine?.GetUserConnection())
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}
		}

		protected IConnection Connection { get; }

		internal SlcConnectivityManagementHelper SlcConnectivityManagementHelper { get; }

		public EndpointRepository Endpoints { get; }

		public VirtualSignalGroupRepository VirtualSignalGroups { get; }

		public LevelRepository Levels { get; }

		public CategoryRepository Categories { get; }

		public TransportTypeRepository TransportTypes { get; }

		public ConnectionRepository Connections { get; }

		public bool IsInstalled()
		{
			var moduleSettingsHelper = new ModuleSettingsHelper(Connection.HandleMessages);

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
