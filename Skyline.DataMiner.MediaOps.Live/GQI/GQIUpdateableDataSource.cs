namespace Skyline.DataMiner.MediaOps.Live.GQI
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Analytics.GenericInterface;

	public abstract class GQIUpdateableDataSource : GQIDataSourceBase, IGQIDataSource, IGQIUpdateable
	{
		private readonly object _lock = new object();

		private readonly Dictionary<string, RowInfo> _rows = new Dictionary<string, RowInfo>();

		private IGQIUpdater _updater;

		GQIColumn[] IGQIDataSource.GetColumns()
		{
			return GetColumns();
		}

		GQIPage IGQIDataSource.GetNextPage(GetNextPageInputArgs args)
		{
			var page = GetNextPage(args);

			lock (_lock)
			{
				var newRows = new List<GQIRow>(page.Rows.Length);

				// replace rows that were already updated in the meantime
				foreach (var row in page.Rows)
				{
					var rowInfo = GetOrCreateRowInfo(row.Key);

					if (rowInfo.LastUpdateType == UpdateType.None)
					{
						rowInfo.Row = row;
					}

					if (rowInfo.IsSentToClient ||
						rowInfo.LastUpdateType == UpdateType.Remove)
					{
						// If the row was already sent to the client or removed, skip it
						continue;
					}

					rowInfo.IsSentToClient = true;
					newRows.Add(rowInfo.Row);
				}

				return new GQIPage(newRows.ToArray())
				{
					HasNextPage = page.HasNextPage,
				};
			}
		}

		void IGQIUpdateable.OnStartUpdates(IGQIUpdater updater)
		{
			_updater = updater;
			OnStartUpdates(updater);
		}

		void IGQIUpdateable.OnStopUpdates()
		{
			OnStopUpdates();
			_updater = null;
		}

		public abstract void OnStartUpdates(IGQIUpdater updater);

		public abstract void OnStopUpdates();

		public void AddRow(GQIRow row)
		{
			if (row == null)
			{
				throw new ArgumentNullException(nameof(row));
			}

			lock (_lock)
			{
				var rowInfo = GetOrCreateRowInfo(row.Key);
				rowInfo.LastUpdateType = UpdateType.Add;
				rowInfo.Row = row;

				EnsureGqiUpdaterIsAvailable();
				_updater.AddRow(row);

				rowInfo.IsSentToClient = true;
			}
		}

		public void UpdateRow(GQIRow row)
		{
			if (row == null)
			{
				throw new ArgumentNullException(nameof(row));
			}

			lock (_lock)
			{
				var rowInfo = GetOrCreateRowInfo(row.Key);
				rowInfo.LastUpdateType = UpdateType.Update;
				rowInfo.Row = row;

				if (rowInfo.IsSentToClient)
				{
					EnsureGqiUpdaterIsAvailable();
					_updater.UpdateRow(row);
				}
			}
		}

		public void AddOrUpdateRow(GQIRow row)
		{
			if (row == null)
			{
				throw new ArgumentNullException(nameof(row));
			}

			lock (_lock)
			{
				var rowInfo = GetOrCreateRowInfo(row.Key);
				rowInfo.Row = row;

				EnsureGqiUpdaterIsAvailable();

				if (!rowInfo.IsSentToClient)
				{
					rowInfo.LastUpdateType = UpdateType.Add;

					_updater.AddRow(row);
					rowInfo.IsSentToClient = true;
				}
				else
				{
					rowInfo.LastUpdateType = UpdateType.Update;

					_updater.UpdateRow(row);
				}
			}
		}

		public void RemoveRow(string rowKey)
		{
			if (rowKey == null)
			{
				throw new ArgumentNullException(nameof(rowKey));
			}

			lock (_lock)
			{
				var rowInfo = GetOrCreateRowInfo(rowKey);
				rowInfo.LastUpdateType = UpdateType.Remove;
				rowInfo.Row = null;

				if (rowInfo.IsSentToClient)
				{
					EnsureGqiUpdaterIsAvailable();
					_updater.RemoveRow(rowKey);
				}
			}
		}

		private void EnsureGqiUpdaterIsAvailable()
		{
			if (_updater == null)
			{
				throw new InvalidOperationException($"{nameof(OnStartUpdates)} wasn't called yet (updater is not available)");
			}
		}

		private RowInfo GetOrCreateRowInfo(string key)
		{
			if (!_rows.TryGetValue(key, out var rowInfo))
			{
				rowInfo = new RowInfo(key);
				_rows.Add(key, rowInfo);
			}

			return rowInfo;
		}

		private enum UpdateType
		{
			None,
			Add,
			Update,
			Remove,
		}

		private class RowInfo
		{
			public string Key { get; }

			public GQIRow Row { get; set; }

			public bool IsSentToClient { get; set; }

			public UpdateType LastUpdateType { get; set; }

			public RowInfo(string key)
			{
				Key = key ?? throw new ArgumentNullException(nameof(key));
			}
		}
	}
}
