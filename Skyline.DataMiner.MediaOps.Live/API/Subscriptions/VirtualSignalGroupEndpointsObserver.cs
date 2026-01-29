namespace Skyline.DataMiner.MediaOps.Live.API.Subscriptions
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class VirtualSignalGroupEndpointsObserver : IDisposable
	{
		private readonly object _lock = new();

		private EndpointsObserver _endpointsObserver;
		private VirtualSignalGroupsObserver _virtualSignalGroupsObserver;

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualSignalGroupEndpointsObserver"/> class.
		/// This observer can be used to monitor changes in virtual signal groups and endpoints.
		/// It uses the provided API to subscribe to changes and updates the provided cache accordingly.
		/// It raises events when virtual signal groups or endpoints are created, updated, or deleted.
		/// </summary>
		/// <param name="api">The API object to use for subscriptions.</param>
		/// <param name="cache">The cache to update when changes occur.</param>
		public VirtualSignalGroupEndpointsObserver(IMediaOpsLiveApi api, VirtualSignalGroupEndpointsCache cache)
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
		public VirtualSignalGroupEndpointsObserver(IMediaOpsLiveApi api) : this(api, new VirtualSignalGroupEndpointsCache())
		{
		}

		public event EventHandler<ApiObjectsChangedEvent<Endpoint>> EndpointsChanged;

		public event EventHandler<ApiObjectsChangedEvent<VirtualSignalGroup>> VirtualSignalGroupsChanged;

		internal IMediaOpsLiveApi Api { get; }

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

				// Initialize and subscribe to endpoints observer
				_endpointsObserver = new EndpointsObserver(Api, Cache.Endpoints);
				_endpointsObserver.EndpointsChanged += Endpoints_Changed;
				_endpointsObserver.Subscribe();

				// Initialize and subscribe to virtual signal groups observer
				_virtualSignalGroupsObserver = new VirtualSignalGroupsObserver(Api, Cache.VirtualSignalGroups);
				_virtualSignalGroupsObserver.VirtualSignalGroupsChanged += VirtualSignalGroups_Changed;
				_virtualSignalGroupsObserver.Subscribe();

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

				if (_endpointsObserver != null)
				{
					_endpointsObserver.EndpointsChanged -= Endpoints_Changed;
					_endpointsObserver.Unsubscribe();
					_endpointsObserver.Dispose();
					_endpointsObserver = null;
				}

				if (_virtualSignalGroupsObserver != null)
				{
					_virtualSignalGroupsObserver.VirtualSignalGroupsChanged -= VirtualSignalGroups_Changed;
					_virtualSignalGroupsObserver.Unsubscribe();
					_virtualSignalGroupsObserver.Dispose();
					_virtualSignalGroupsObserver = null;
				}

				IsSubscribed = false;
			}
		}

		public void LoadInitialData()
		{
			lock (_lock)
			{
				// The cache.LoadInitialData will load both endpoints and virtual signal groups
				Cache.LoadInitialData(Api);
			}
		}

		private void Endpoints_Changed(object sender, ApiObjectsChangedEvent<Endpoint> e)
		{
			// Forward the event from the child observer
			EndpointsChanged?.Invoke(this, e);
		}

		private void VirtualSignalGroups_Changed(object sender, ApiObjectsChangedEvent<VirtualSignalGroup> e)
		{
			// Forward the event from the child observer
			VirtualSignalGroupsChanged?.Invoke(this, e);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Unsubscribe();
			}
		}
	}
}
