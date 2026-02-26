namespace Skyline.DataMiner.Solutions.MediaOps.Live.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM.Registration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Definitions.SlcConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Definitions.SlcOrchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Logging;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Plan;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;

	public class MediaOpsLiveApi : IMediaOpsLiveApi
	{
		private const string CatalogItemId = "213031b9-af0b-488c-be20-934912b967c0";

		public MediaOpsLiveApi(IConnection connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));

			SlcConnectivityManagementHelper = new SlcConnectivityManagementHelper(connection);
			SlcOrchestrationHelper = new SlcOrchestrationHelper(connection);

			MediationElements = new MediationElements(this);

			Endpoints = new EndpointRepository(this);
			VirtualSignalGroups = new VirtualSignalGroupRepository(this);
			VirtualSignalGroupStates = new VirtualSignalGroupStateRepository(this);
			Levels = new LevelRepository(this);
			TransportTypes = new TransportTypeRepository(this);

			Orchestration = new OrchestrationHelper(SlcOrchestrationHelper, this);
		}

		public IConnection Connection { get; }

		public MediationElements MediationElements { get; }

		public EndpointRepository Endpoints { get; }

		public VirtualSignalGroupRepository VirtualSignalGroups { get; }

		public VirtualSignalGroupStateRepository VirtualSignalGroupStates { get; }

		public LevelRepository Levels { get; }

		public TransportTypeRepository TransportTypes { get; }

		public OrchestrationHelper Orchestration { get; }

		internal SlcConnectivityManagementHelper SlcConnectivityManagementHelper { get; }

		internal SlcOrchestrationHelper SlcOrchestrationHelper { get; }

		internal ILogger Logger { get; private set; }

		public void SetLogger(ILogger logger)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public virtual MediaOpsLiveCache GetCache()
		{
			return MediaOpsLiveCache.GetOrCreate(Connection);
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
			return new MediaOpsPlanHelper(this);
		}

		/// <summary>
		/// Installs the required DOM modules for the MediaOps.LIVE API.
		/// </summary>
		public void InstallDomModules()
		{
			Action<string> logAction = x => Logger?.Information(x);

			DomModuleInstaller.Install(Connection.HandleMessages, new SlcConnectivityManagementDomModule(), logAction);
			DomModuleInstaller.Install(Connection.HandleMessages, new SlcOrchestrationDomModule(), logAction);
		}

		/// <summary>
		/// Determines whether the MediaOps.LIVE application is installed on the DataMiner System.
		/// </summary>
		/// <param name="version">
		/// When this method returns <c>true</c>, contains the version of the installed application;
		/// otherwise, <c>null</c>.
		/// </param>
		/// <returns>
		/// <c>true</c> if the application is installed; otherwise, <c>false</c>.
		/// </returns>
		public bool IsInstalled(out string version)
		{
			var registrar = Connection.GetSdmRegistrar();
			var mediaOpsLiveRegistration = registrar.Solutions.Read(SolutionRegistrationExposers.ID.Equal(CatalogItemId)).FirstOrDefault();
			if (mediaOpsLiveRegistration == null)
			{
				version = null;
				return false;
			}

			version = mediaOpsLiveRegistration.Version;
			return true;
		}

		/// <summary>
		/// Determines whether the MediaOps.LIVE application is installed on the DataMiner System.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the application is installed; otherwise, <c>false</c>.
		/// </returns>
		public bool IsInstalled()
		{
			return IsInstalled(out _);
		}

		public static string GetVersion()
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
