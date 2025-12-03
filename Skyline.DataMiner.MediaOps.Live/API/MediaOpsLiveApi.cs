namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.MediaOps.Live.Logging;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.Plan;
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

			MediationElements = new MediationElements(this);

			Endpoints = new EndpointRepository(this);
			VirtualSignalGroups = new VirtualSignalGroupRepository(this);
			Levels = new LevelRepository(this);
			TransportTypes = new TransportTypeRepository(this);

			Orchestration = new OrchestrationHelper(SlcOrchestrationHelper, this);
		}

		protected internal IConnection Connection { get; }

		protected internal ILogger Logger { get; private set; }

		internal SlcConnectivityManagementHelper SlcConnectivityManagementHelper { get; }

		internal SlcOrchestrationHelper SlcOrchestrationHelper { get; }

		public MediationElements MediationElements { get; }

		public EndpointRepository Endpoints { get; }

		public VirtualSignalGroupRepository VirtualSignalGroups { get; }

		public LevelRepository Levels { get; }

		public TransportTypeRepository TransportTypes { get; }

		public OrchestrationHelper Orchestration { get; }

		public void SetLogger(ILogger logger)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public IDms GetDms()
		{
			return Connection.GetDms();
		}

		public virtual TakeHelper GetConnectionHandler()
		{
			return new TakeHelper(this);
		}

		public virtual LiteConnectivityInfoProvider GetLiteConnectivityInfoProvider()
		{
			return new LiteConnectivityInfoProvider(this);
		}

		public virtual ConnectivityInfoProvider GetConnectivityInfoProvider()
		{
			return new ConnectivityInfoProvider(this);
		}

		internal virtual MediaOpsPlanHelper GetMediaOpsPlanHelper()
		{
			return new MediaOpsPlanHelper();
		}

		/// <summary>
		/// Installs the required DOM modules for the MediaOps.LIVE API.
		/// </summary>
		/// <param name="logAction">Optional action to log progress or messages during installation. If null, logging is suppressed.</param>
		public void InstallDomModules(Action<string> logAction = null)
		{
			// When no logging action is provided, use a no-op.
			logAction ??= x => { };

			DomModuleInstaller.Install(Connection.HandleMessages, new SlcConnectivityManagementDomModule(), logAction);
			DomModuleInstaller.Install(Connection.HandleMessages, new SlcOrchestrationDomModule(), logAction);
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

		public string GetVersion()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			if (!String.IsNullOrWhiteSpace(ThisAssembly.Git.Tag))
			{
				return ThisAssembly.Git.Tag;
			}

			return $"{ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch}{ThisAssembly.Git.SemVer.DashLabel}";
#pragma warning restore CS0618 // Type or member is obsolete
		}
	}
}
