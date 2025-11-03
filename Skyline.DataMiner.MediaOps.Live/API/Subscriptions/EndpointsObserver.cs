namespace Skyline.DataMiner.MediaOps.Live.API.Subscriptions
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class EndpointsObserver : IDisposable
	{
		private readonly object _lock = new();

		private RepositorySubscription<Endpoint> _subscriptionEndpoints;

		/// <summary>
		/// Initializes a new instance of the <see cref="EndpointsObserver"/> class.
		/// This observer can be used to monitor changes in endpoints.
		/// It uses the provided API to subscribe to changes and updates the provided cache accordingly.
		/// It raises events when endpoints are created, updated, or deleted.
		/// </summary>
		/// <param name="api">The API object to use for subscriptions.</param>
		/// <param name="cache">The cache to update when changes occur.</param>
		public EndpointsObserver(MediaOpsLiveApi api, EndpointsCache cache)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			Cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EndpointsObserver"/> class.
		/// This observer can be used to monitor changes in endpoints.
		/// It uses the provided API to subscribe to changes and updates the provided cache accordingly.
		/// It raises events when endpoints are created, updated, or deleted.
		/// </summary>
		/// <param name="api">The API object to use for subscriptions.</param>
		public EndpointsObserver(MediaOpsLiveApi api) : this(api, new EndpointsCache())
		{
		}

		public event EventHandler<ApiObjectsChangedEvent<Endpoint>> EndpointsChanged;

		internal MediaOpsLiveApi Api { get; }

		public EndpointsCache Cache { get; }

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
