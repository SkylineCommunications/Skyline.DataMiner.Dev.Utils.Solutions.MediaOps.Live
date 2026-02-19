namespace Skyline.DataMiner.Solutions.MediaOps.Live.API
{
	using System;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Logging;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;

	public interface IMediaOpsLiveApi
	{
		IConnection Connection { get; }

		EndpointRepository Endpoints { get; }

		LevelRepository Levels { get; }

		MediationElements MediationElements { get; }

		OrchestrationHelper Orchestration { get; }

		TransportTypeRepository TransportTypes { get; }

		VirtualSignalGroupRepository VirtualSignalGroups { get; }

		VirtualSignalGroupStateRepository VirtualSignalGroupStates { get; }

		MediaOpsLiveCache GetCache();

		TakeHelper GetConnectionHandler();

		ConnectivityInfoProvider GetConnectivityInfoProvider();

		LiteConnectivityInfoProvider GetLiteConnectivityInfoProvider();

		void InstallDomModules();

		bool IsInstalled();

		bool IsInstalled(out string version);

		void SetLogger(ILogger logger);
	}
}