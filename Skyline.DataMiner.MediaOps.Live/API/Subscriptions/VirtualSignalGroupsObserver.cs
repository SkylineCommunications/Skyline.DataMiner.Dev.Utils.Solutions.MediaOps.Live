namespace Skyline.DataMiner.MediaOps.Live.API.Subscriptions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class VirtualSignalGroupsObserver : IDisposable
	{
		private readonly object _lock = new();

		private RepositorySubscription<VirtualSignalGroup> _subscriptionVirtualSignalGroups;
		private RepositorySubscription<VirtualSignalGroupState> _subscriptionVirtualSignalGroupStates;

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

				_subscriptionVirtualSignalGroupStates = Api.VirtualSignalGroupStates.Subscribe();
				_subscriptionVirtualSignalGroupStates.Changed += VirtualSignalGroupStates_Changed;

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

				_subscriptionVirtualSignalGroupStates.Changed -= VirtualSignalGroupStates_Changed;
				_subscriptionVirtualSignalGroupStates.Dispose();

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

		private void VirtualSignalGroupStates_Changed(object sender, ApiObjectsChangedEvent<VirtualSignalGroupState> e)
		{
			// These state objects don't impact VSG configuration directly,
			// but we still want to report the associated VSGs as "updated".
			ICollection<VirtualSignalGroup> updatedVsgs;

			lock (_lock)
			{
				Cache.UpdateVirtualSignalGroupStates(e.Created.Concat(e.Updated), e.Deleted);

				var vsgRefs = e.Created.Concat(e.Updated).Concat(e.Deleted)
					.Select(item => item.VirtualSignalGroupReference);

				updatedVsgs = ResolveVirtualSignalGroups(vsgRefs);
			}

			// Raise event outside lock to avoid potential deadlocks
			if (updatedVsgs.Count > 0)
			{
				var args = new ApiObjectsChangedEvent<VirtualSignalGroup>(
					created: [],
					updated: updatedVsgs,
					deleted: []);

				VirtualSignalGroupsChanged?.Invoke(this, args);
			}
		}

		/// <summary>
		/// Resolves a collection of <see cref="VirtualSignalGroup"/> references to their corresponding objects.
		/// </summary>
		/// <param name="references">
		/// The references to resolve.
		/// </param>
		/// <remarks>
		/// This method first attempts to resolve each reference from the local cache.
		/// If any references are missing from the cache, it loads the missing <see cref="VirtualSignalGroup"/> objects from the API,
		/// updates the cache with the newly loaded objects, and includes them in the result.
		/// </remarks>
		/// <returns>
		/// A collection of <see cref="VirtualSignalGroup"/> objects corresponding to the provided references.
		/// </returns>
		private ICollection<VirtualSignalGroup> ResolveVirtualSignalGroups(IEnumerable<ApiObjectReference<VirtualSignalGroup>> references)
		{
			var result = new HashSet<VirtualSignalGroup>();
			var missing = new HashSet<ApiObjectReference<VirtualSignalGroup>>();

			// First attempt: resolve from cache
			foreach (var reference in references)
			{
				if (Cache.TryGetVirtualSignalGroup(reference, out var vsg))
				{
					result.Add(vsg);
				}
				else
				{
					missing.Add(reference);
				}
			}

			// Second attempt: load missing VSGs from API
			if (missing.Count > 0)
			{
				var loaded = Api.VirtualSignalGroups.Read(missing).Values;

				Cache.UpdateVirtualSignalGroups(loaded, []);
				result.UnionWith(loaded);
			}

			return result;
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
