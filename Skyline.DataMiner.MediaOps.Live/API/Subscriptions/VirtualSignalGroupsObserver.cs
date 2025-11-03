namespace Skyline.DataMiner.MediaOps.Live.API.Subscriptions
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class VirtualSignalGroupsObserver : IDisposable
	{
		private readonly object _lock = new();

		private RepositorySubscription<VirtualSignalGroup> _subscriptionVirtualSignalGroups;

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualSignalGroupsObserver"/> class.
		/// This observer can be used to monitor changes in virtual signal groups.
		/// It uses the provided API to subscribe to changes and updates the provided cache accordingly.
		/// It raises events when virtual signal groups are created, updated, or deleted.
		/// </summary>
		/// <param name="api">The API object to use for subscriptions.</param>
		/// <param name="cache">The cache to update when changes occur.</param>
		public VirtualSignalGroupsObserver(MediaOpsLiveApi api, VirtualSignalGroupsCache cache)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			Cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualSignalGroupsObserver"/> class.
		/// This observer can be used to monitor changes in virtual signal groups.
		/// It uses the provided API to subscribe to changes and updates the provided cache accordingly.
		/// It raises events when virtual signal groups are created, updated, or deleted.
		/// </summary>
		/// <param name="api">The API object to use for subscriptions.</param>
		public VirtualSignalGroupsObserver(MediaOpsLiveApi api) : this(api, new VirtualSignalGroupsCache())
		{
		}

		public event EventHandler<ApiObjectsChangedEvent<VirtualSignalGroup>> VirtualSignalGroupsChanged;

		internal MediaOpsLiveApi Api { get; }

		public VirtualSignalGroupsCache Cache { get; }

		public bool IsSubscribed { get; private set; }

		public void Subscribe()
		{
			lock (_lock)
			{
				if (IsSubscribed)
				{
					return;
				}

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
