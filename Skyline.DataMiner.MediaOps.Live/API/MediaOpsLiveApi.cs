namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	public class MediaOpsLiveApi
	{
		public MediaOpsLiveApi(IEngine engine) : this(new SlcConnectivityManagementHelper(engine))
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}
		}

		public MediaOpsLiveApi(DomHelper helper) : this(new SlcConnectivityManagementHelper(helper))
		{
			if (helper == null)
			{
				throw new ArgumentNullException(nameof(helper));
			}
		}

		public MediaOpsLiveApi(SlcConnectivityManagementHelper helper)
		{
			Helper = helper ?? throw new ArgumentNullException(nameof(helper));

			Endpoints = new EndpointRepository(helper);
			VirtualSignalGroups = new VirtualSignalGroupRepository(helper);
			Levels = new LevelRepository(helper);
			Categories = new CategoryRepository(helper);
			TransportTypes = new TransportTypeRepository(helper);
			Connections = new ConnectionRepository(helper);
		}

		internal SlcConnectivityManagementHelper Helper { get; }

		public EndpointRepository Endpoints { get; }

		public VirtualSignalGroupRepository VirtualSignalGroups { get; }

		public LevelRepository Levels { get; }

		public CategoryRepository Categories { get; }

		public TransportTypeRepository TransportTypes { get; }

		public ConnectionRepository Connections { get; }
	}
}
