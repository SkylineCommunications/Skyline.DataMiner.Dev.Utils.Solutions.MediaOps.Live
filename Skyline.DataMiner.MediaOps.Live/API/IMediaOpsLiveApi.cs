namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Logging;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;
	using Skyline.DataMiner.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.Net;

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