namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Subscriptions
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class LevelsObserver : IDisposable
	{
		private readonly object _lock = new();

		private RepositorySubscription<Level> _subscriptionLevels;

		/// <summary>
		/// Initializes a new instance of the <see cref="LevelsObserver"/> class.
		/// This observer can be used to monitor changes in levels.
		/// It uses the provided API to subscribe to changes and updates the provided cache accordingly.
		/// It raises events when levels are created, updated, or deleted.
		/// </summary>
		/// <param name="api">The API object to use for subscriptions.</param>
		/// <param name="cache">The cache to update when changes occur.</param>
		public LevelsObserver(IMediaOpsLiveApi api, LevelsCache cache)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			Cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LevelsObserver"/> class.
		/// This observer can be used to monitor changes in levels.
		/// It uses the provided API to subscribe to changes and updates the provided cache accordingly.
		/// It raises events when levels are created, updated, or deleted.
		/// </summary>
		/// <param name="api">The API object to use for subscriptions.</param>
		public LevelsObserver(IMediaOpsLiveApi api) : this(api, new LevelsCache())
		{
		}

		public event EventHandler<ApiObjectsChangedEvent<Level>> LevelsChanged;

		internal IMediaOpsLiveApi Api { get; }

		public LevelsCache Cache { get; }

		public bool IsSubscribed { get; private set; }

		public void Subscribe()
		{
			lock (_lock)
			{
				if (IsSubscribed)
				{
					return;
				}

				_subscriptionLevels = Api.Levels.Subscribe();
				_subscriptionLevels.Changed += Levels_Changed;

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

				_subscriptionLevels.Changed -= Levels_Changed;
				_subscriptionLevels.Dispose();

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

		private void Levels_Changed(object sender, ApiObjectsChangedEvent<Level> e)
		{
			lock (_lock)
			{
				Cache.UpdateLevels(e.Created.Concat(e.Updated), e.Deleted);
			}

			LevelsChanged?.Invoke(this, e);
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
