namespace Skyline.DataMiner.MediaOps.Live.API.Subscriptions
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class TransportTypesObserver : IDisposable
	{
		private readonly object _lock = new();

		private RepositorySubscription<TransportType> _subscriptionTransportTypes;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransportTypesObserver"/> class.
		/// This observer can be used to monitor changes in transport types.
		/// It uses the provided API to subscribe to changes and updates the provided cache accordingly.
		/// It raises events when transport types are created, updated, or deleted.
		/// </summary>
		/// <param name="api">The API object to use for subscriptions.</param>
		/// <param name="cache">The cache to update when changes occur.</param>
		public TransportTypesObserver(MediaOpsLiveApi api, TransportTypesCache cache)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			Cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TransportTypesObserver"/> class.
		/// This observer can be used to monitor changes in transport types.
		/// It uses the provided API to subscribe to changes and updates the provided cache accordingly.
		/// It raises events when transport types are created, updated, or deleted.
		/// </summary>
		/// <param name="api">The API object to use for subscriptions.</param>
		public TransportTypesObserver(MediaOpsLiveApi api) : this(api, new TransportTypesCache())
		{
		}

		public event EventHandler<ApiObjectsChangedEvent<TransportType>> TransportTypesChanged;

		internal MediaOpsLiveApi Api { get; }

		public TransportTypesCache Cache { get; }

		public bool IsSubscribed { get; private set; }

		public void Subscribe()
		{
			lock (_lock)
			{
				if (IsSubscribed)
				{
					return;
				}

				_subscriptionTransportTypes = Api.TransportTypes.Subscribe();
				_subscriptionTransportTypes.Changed += TransportTypes_Changed;

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

				_subscriptionTransportTypes.Changed -= TransportTypes_Changed;
				_subscriptionTransportTypes.Dispose();

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

		private void TransportTypes_Changed(object sender, ApiObjectsChangedEvent<TransportType> e)
		{
			lock (_lock)
			{
				Cache.UpdateTransportTypes(e.Created.Concat(e.Updated), e.Deleted);
			}

			TransportTypesChanged?.Invoke(this, e);
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
