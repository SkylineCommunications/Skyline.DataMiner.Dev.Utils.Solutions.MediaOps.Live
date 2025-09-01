namespace Skyline.DataMiner.MediaOps.Live.API.Caching
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Subscriptions;

	public class LevelsCache : IDisposable
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<Level>, Level> _levels = new();

		private RepositorySubscription<Level> _subscriptionLevels;

		public LevelsCache(MediaOpsLiveApi api, bool subscribe = false)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));

			Initialize(subscribe);
		}

		public MediaOpsLiveApi Api { get; }

		public IReadOnlyDictionary<ApiObjectReference<Level>, Level> Levels => _levels;

		public bool IsSubscribed { get; private set; }

		public bool TryGetLevel(ApiObjectReference<Level> id, out Level level)
		{
			return _levels.TryGetValue(id, out level);
		}

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

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			Unsubscribe();
		}

		private void Initialize(bool subscribe)
		{
			lock (_lock)
			{
				if (subscribe)
				{
					Subscribe();
				}

				LoadInitialData();
			}
		}

		private void LoadInitialData()
		{
			var levels = Api.Levels.ReadAll();
			UpdateLevels(levels);
		}

		private void Levels_Changed(object sender, ApiObjectsChangedEvent<Level> e)
		{
			lock (_lock)
			{
				Debug.WriteLine($"Levels changed: {e}");

				UpdateLevels(e.Created.Concat(e.Updated), e.Deleted);
			}
		}

		private void UpdateLevels(IEnumerable<Level> updated, IEnumerable<Level> deleted = null)
		{
			lock (_lock)
			{
				if (updated != null)
				{
					foreach (var item in updated)
					{
						_levels[item.ID] = item;
					}
				}

				if (deleted != null)
				{
					foreach (var item in deleted)
					{
						_levels.Remove(item);
					}
				}
			}
		}
	}
}
