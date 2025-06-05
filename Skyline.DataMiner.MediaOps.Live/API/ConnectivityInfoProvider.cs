namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;
	using System.Collections.Concurrent;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;



	public class ConnectivityInfoProvider : IDisposable
	{
		private readonly object _lock = new();

		private readonly ConcurrentDictionary<ApiObjectReference<Endpoint>, Endpoint> _endpoints = new();
		private readonly ConcurrentDictionary<ApiObjectReference<VirtualSignalGroup>, VirtualSignalGroup> _virtualSignalGroups = new();
		private readonly ConcurrentDictionary<ApiObjectReference<Connection>, Connection> _connections = new();

		private readonly VirtualSignalGroupEndpointsMapping _virtualSignalGroupEndpointsMapping = new();
		private readonly ConnectionEndpointsMapping _connectionEndpointsMapping = new();

		private RepositorySubscription<Endpoint> _subscriptionEndpoints;
		private RepositorySubscription<VirtualSignalGroup> _subscriptionVirtualSignalGroups;
		private RepositorySubscription<Connection> _subscriptionConnections;

		public ConnectivityInfoProvider(MediaOpsLiveApi api)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
		}

		public MediaOpsLiveApi Api { get; }

		public void Subscribe()
		{
			_subscriptionEndpoints = Api.Endpoints.Subscribe();
			_subscriptionVirtualSignalGroups = Api.VirtualSignalGroups.Subscribe();
			_subscriptionConnections = Api.Connections.Subscribe();
		}

		public void Dispose()
		{
			_subscriptionEndpoints?.Dispose();
			_subscriptionVirtualSignalGroups?.Dispose();
			_subscriptionConnections?.Dispose();
		}
	}
}
