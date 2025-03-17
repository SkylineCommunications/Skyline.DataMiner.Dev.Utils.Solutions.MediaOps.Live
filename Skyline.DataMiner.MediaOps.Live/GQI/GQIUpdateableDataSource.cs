namespace Skyline.DataMiner.MediaOps.Live.GQI
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Analytics.GenericInterface;

	public abstract class GQIUpdateableDataSource : IGQIDataSource, IGQIUpdateable
	{
		private readonly object _lock = new object();

		private readonly HashSet<string> _returnedRows = new HashSet<string>();
		private readonly Dictionary<string, GQIRow> _rows = new Dictionary<string, GQIRow>();

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
				// replace rows that were already updated in the meantime
				for (int i = 0; i < page.Rows.Length; i++)
				{
					var row = page.Rows[i];

					if (_rows.TryGetValue(row.Key, out var updatedRow))
					{
						page.Rows[i] = updatedRow;
					}
					else
					{
						_rows.Add(row.Key, row);
					}

					_returnedRows.Add(row.Key);
				}
			}

			return page;
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

		public abstract GQIColumn[] GetColumns();

		public abstract GQIPage GetNextPage(GetNextPageInputArgs args);

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
				if (!_returnedRows.Contains(row.Key))
				{
					EnsureGqiUpdaterIsAvailable();
					_updater.AddRow(row);

					_returnedRows.Add(row.Key);
				}

				_rows[row.Key] = row;
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
				if (_returnedRows.Contains(row.Key))
				{
					EnsureGqiUpdaterIsAvailable();
					_updater.UpdateRow(row);
				}

				_rows[row.Key] = row;
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
				EnsureGqiUpdaterIsAvailable();

				if (_returnedRows.Contains(row.Key))
				{
					_updater.UpdateRow(row);
				}
				else
				{
					_updater.AddRow(row);
					_returnedRows.Add(row.Key);
				}

				_rows[row.Key] = row;
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
				if (_returnedRows.Contains(rowKey))
				{
					EnsureGqiUpdaterIsAvailable();
					_updater.RemoveRow(rowKey);

					_returnedRows.Remove(rowKey);
				}

				_rows.Remove(rowKey);
			}
		}

		private void EnsureGqiUpdaterIsAvailable()
		{
			if (_updater == null)
			{
				throw new InvalidOperationException($"{nameof(OnStartUpdates)} wasn't called yet (updater is not available)");
			}
		}
	}
}
