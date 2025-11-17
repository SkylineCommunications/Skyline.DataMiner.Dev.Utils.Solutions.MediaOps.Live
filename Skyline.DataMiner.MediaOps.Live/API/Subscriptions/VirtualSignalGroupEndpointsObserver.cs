namespace Skyline.DataMiner.MediaOps.Live.API.Subscriptions
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class VirtualSignalGroupEndpointsObserver : IDisposable
	{
		private readonly object _lock = new();

		private RepositorySubscription<Endpoint> _subscriptionEndpoints;
		private RepositorySubscription<VirtualSignalGroup> _subscriptionVirtualSignalGroups;

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualSignalGroupEndpointsObserver"/> class.
		/// This observer can be used to monitor changes in virtual signal groups and endpoints.
		/// It uses the provided API to subscribe to changes and updates the provided cache accordingly.
		/// It raises events when virtual signal groups or endpoints are created, updated, or deleted.
		/// </summary>
		/// <param name="api">The API object to use for subscriptions.</param>
		/// <param name="cache">The cache to update when changes occur.</param>
		public VirtualSignalGroupEndpointsObserver(MediaOpsLiveApi api, VirtualSignalGroupEndpointsCache cache)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			Cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

			/// <summary>
		/// Initializes a new instance of the <see cref="VirtualSignalGroupEndpointsObserver"/> class.
		/// This observer can be used to monitor changes in virtual signal groups and endpoints.
		/// It uses the provided API to subscribe to changes and updates the provided cache accordingly.
		/// It raises events when virtual signal groups or endpoints are created, updated, or deleted.
		/// </summary>
		/// <param name="api">The API object to use for subscriptions.</param>
		public VirtualSignalGroupEndpointsObserver(MediaOpsLiveApi api) : this(api, new VirtualSignalGroupEndpointsCache())
		{
		}

		public event EventHandler<ApiObjectsChangedEvent<Endpoint>> EndpointsChanged;

		public event EventHandler<ApiObjectsChangedEvent<VirtualSignalGroup>> VirtualSignalGroupsChanged;

		internal MediaOpsLiveApi Api { get; }

		public VirtualSignalGroupEndpointsCache Cache { get; }

		public bool IsSubscribed { get; private set; }

		public void Subscribe()
		{
			lock (_lock)
			{
				if (IsSubscribed)
				{
					return;
				}

				_subscriptionEndpoints = Api.Endpoints.Subscribe();
				_subscriptionEndpoints.Changed += Endpoints_Changed;

				_subscriptionVirtualSignalGroups = Api.VirtualSignalGroups.Subscribe();
				_subscriptionVirtualSignalGroups.Changed += VirtualSignalGroups_Changed;

				IsSubscribed = true;
			}
		}

		public void Unsubscribe()
		{
			lock (_lock)
			{
				if (!IsSubscribed)
				{
					return;
				}

				_subscriptionEndpoints.Changed -= Endpoints_Changed;
				_subscriptionEndpoints.Dispose();

				_subscriptionVirtualSignalGroups.Changed -= VirtualSignalGroups_Changed;
				_subscriptionVirtualSignalGroups.Dispose();

				IsSubscribed = false;
			}
		}

		public void LoadInitialData()
		{
			lock (_lock)
			{
				Cache.LoadInitialData(Api);
			}
		}

		private void Endpoints_Changed(object sender, ApiObjectsChangedEvent<Endpoint> e)
		{
			lock (_lock)
			{
				Cache.UpdateEndpoints(e.Created.Concat(e.Updated), e.Deleted);
			}

			EndpointsChanged?.Invoke(this, e);
		}

		private void VirtualSignalGroups_Changed(object sender, ApiObjectsChangedEvent<VirtualSignalGroup> e)
		{
			lock (_lock)
			{
				Cache.UpdateVirtualSignalGroups(e.Created.Concat(e.Updated), e.Deleted);
			}

			VirtualSignalGroupsChanged?.Invoke(this, e);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			Unsubscribe();
		}
	}
}
