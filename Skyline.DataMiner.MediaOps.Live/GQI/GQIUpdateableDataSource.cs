namespace Skyline.DataMiner.MediaOps.Live.GQI
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Analytics.GenericInterface;

	public abstract class GQIUpdateableDataSource : GQIDataSourceBase, IGQIDataSource, IGQIUpdateable
	{
		private readonly object _lock = new object();

		private readonly HashSet<string> _returnedRows = new HashSet<string>();
		private readonly Dictionary<string, PendingRowUpdate> _pendingRowUpdates = new Dictionary<string, PendingRowUpdate>();

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
					if (_returnedRows.Contains(row.Key))
					{
						// Row already sent to the client, skip it
						continue;
					}

					var rowToReturn = row;

					if (_pendingRowUpdates.TryGetValue(row.Key, out var pending))
					{
						_pendingRowUpdates.Remove(row.Key);

						if (pending.Type == PendingUpdateType.Remove)
						{
							continue; // Deleted
						}

						rowToReturn = pending.Row;
					}

					newRows.Add(rowToReturn);
					_returnedRows.Add(row.Key);
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
				if (!_returnedRows.Contains(row.Key))
				{
					EnsureGqiUpdaterIsAvailable();
					_updater.AddRow(row);

					_returnedRows.Add(row.Key);
				}

				_pendingRowUpdates.Remove(row.Key);
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
				else
				{
					_pendingRowUpdates[row.Key] = new PendingRowUpdate(PendingUpdateType.Update, row);
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
				if (_returnedRows.Contains(row.Key))
				{
					UpdateRow(row);
				}
				else
				{
					AddRow(row);
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
				if (_returnedRows.Contains(rowKey))
				{
					EnsureGqiUpdaterIsAvailable();
					_updater.RemoveRow(rowKey);

					_returnedRows.Remove(rowKey);
				}
				else
				{
					_pendingRowUpdates[rowKey] = new PendingRowUpdate(PendingUpdateType.Remove);
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

		private enum PendingUpdateType
		{
			Update,
			Remove,
		}

		private readonly struct PendingRowUpdate
		{
			public PendingUpdateType Type { get; }

			public GQIRow Row { get; }

			public PendingRowUpdate(PendingUpdateType type, GQIRow row = null)
			{
				Type = type;
				Row = row;
			}
		}
	}
}
