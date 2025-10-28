namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Logging;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Main API class for MediaOps Live functionality, providing access to connectivity management and orchestration features.
	/// </summary>
	public class MediaOpsLiveApi
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MediaOpsLiveApi"/> class.
		/// </summary>
		/// <param name="connection">The DataMiner connection.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
		public MediaOpsLiveApi(IConnection connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));

			SlcConnectivityManagementHelper = new SlcConnectivityManagementHelper(connection);
			SlcOrchestrationHelper = new SlcOrchestrationHelper(connection);

			MediationElements = new MediationElements(this);

			Endpoints = new EndpointRepository(SlcConnectivityManagementHelper, connection);
			VirtualSignalGroups = new VirtualSignalGroupRepository(SlcConnectivityManagementHelper, connection);
			Levels = new LevelRepository(SlcConnectivityManagementHelper, connection);
			Categories = new CategoryRepository(SlcConnectivityManagementHelper, connection);
			TransportTypes = new TransportTypeRepository(SlcConnectivityManagementHelper, connection);

			Orchestration = new OrchestrationEventRepository(SlcOrchestrationHelper, this);
		}

		/// <summary>
		/// Gets the DataMiner connection.
		/// </summary>
		protected internal IConnection Connection { get; }

		/// <summary>
		/// Gets the logger instance.
		/// </summary>
		protected internal ILogger Logger { get; private set; }

		internal SlcConnectivityManagementHelper SlcConnectivityManagementHelper { get; }

		internal SlcOrchestrationHelper SlcOrchestrationHelper { get; }

		/// <summary>
		/// Gets the mediation elements manager.
		/// </summary>
		public MediationElements MediationElements { get; }

		/// <summary>
		/// Gets the endpoint repository.
		/// </summary>
		public EndpointRepository Endpoints { get; }

		/// <summary>
		/// Gets the virtual signal group repository.
		/// </summary>
		public VirtualSignalGroupRepository VirtualSignalGroups { get; }

		/// <summary>
		/// Gets the level repository.
		/// </summary>
		public LevelRepository Levels { get; }

		/// <summary>
		/// Gets the category repository.
		/// </summary>
		public CategoryRepository Categories { get; }

		/// <summary>
		/// Gets the transport type repository.
		/// </summary>
		public TransportTypeRepository TransportTypes { get; }

		/// <summary>
		/// Gets the orchestration event repository.
		/// </summary>
		public OrchestrationEventRepository Orchestration { get; }

		/// <summary>
		/// Sets the logger to use for logging operations.
		/// </summary>
		/// <param name="logger">The logger instance.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
		public void SetLogger(ILogger logger)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Gets the DataMiner System interface.
		/// </summary>
		/// <returns>The DataMiner System interface.</returns>
		public IDms GetDms()
		{
			return Connection.GetDms();
		}

		/// <summary>
		/// Gets a connection handler for managing endpoint connections.
		/// </summary>
		/// <returns>A new <see cref="TakeHelper"/> instance.</returns>
		public virtual TakeHelper GetConnectionHandler()
		{
			return new TakeHelper(this);
		}

		/// <summary>
		/// Gets a lite connectivity information provider.
		/// </summary>
		/// <returns>A new <see cref="LiteConnectivityInfoProvider"/> instance.</returns>
		public virtual LiteConnectivityInfoProvider GetLiteConnectivityInfoProvider()
		{
			return new LiteConnectivityInfoProvider(this);
		}

		/// <summary>
		/// Gets a connectivity information provider.
		/// </summary>
		/// <returns>A new <see cref="ConnectivityInfoProvider"/> instance.</returns>
		public virtual ConnectivityInfoProvider GetConnectivityInfoProvider()
		{
			return new ConnectivityInfoProvider(this);
		}

		internal virtual MediaOpsPlanHelper GetMediaOpsPlanHelper()
		{
			return new MediaOpsPlanHelper();
		}

		/// <summary>
		/// Checks whether the MediaOps Live modules are installed on the DataMiner System.
		/// </summary>
		/// <returns><c>true</c> if the modules are installed; otherwise, <c>false</c>.</returns>
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

		/// <summary>
		/// Gets the version of the MediaOps Live API.
		/// </summary>
		/// <returns>The version string.</returns>
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
