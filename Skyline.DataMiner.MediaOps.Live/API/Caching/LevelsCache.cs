namespace Skyline.DataMiner.MediaOps.Live.API.Caching
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class LevelsCache
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<Level>, Level> _levels = new();
		private readonly Dictionary<string, Level> _levelsByName = new();

		public LevelsCache()
		{
		}

		public LevelsCache(IEnumerable<Level> levels)
		{
			if (levels != null)
			{
				UpdateLevels(levels, []);
			}
		}

		public IReadOnlyDictionary<ApiObjectReference<Level>, Level> Levels => _levels;

		public IReadOnlyDictionary<string, Level> LevelsByName => _levelsByName;

		public Level GetLevel(ApiObjectReference<Level> id)
		{
			if (!TryGetLevel(id, out var level))
			{
				throw new ArgumentException($"Couldn't find level with ID {id.ID}", nameof(id));
			}

			return level;
		}

		public Level GetLevel(string name)
		{
			if (!TryGetLevel(name, out var level))
			{
				throw new ArgumentException($"Couldn't find level with name '{name}'", nameof(name));
			}

			return level;
		}

		public bool TryGetLevel(ApiObjectReference<Level> id, out Level level)
		{
			return _levels.TryGetValue(id, out level);
		}

		public bool TryGetLevel(string name, out Level level)
		{
			return _levelsByName.TryGetValue(name, out level);
		}

		public void LoadInitialData(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			var levels = api.Levels.ReadAll();

			lock (_lock)
			{
				UpdateLevels(levels, []);
			}
		}

		public void UpdateLevels(IEnumerable<Level> updated, IEnumerable<Level> deleted)
		{
			if (updated is null)
			{
				throw new ArgumentNullException(nameof(updated));
			}

			if (deleted is null)
			{
				throw new ArgumentNullException(nameof(deleted));
			}

			lock (_lock)
			{
				foreach (var item in updated)
				{
					// Remove old name if it exists
					if (_levels.TryGetValue(item.ID, out var existing))
					{
						_levelsByName.Remove(existing.Name);
					}

					_levels[item.ID] = item;
					_levelsByName[item.Name] = item;
				}

				foreach (var item in deleted)
				{
					_levels.Remove(item.ID);
					_levelsByName.Remove(item.Name);
				}
			}
		}
	}
}
