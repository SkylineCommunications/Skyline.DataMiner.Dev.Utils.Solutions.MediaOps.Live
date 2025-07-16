namespace Skyline.DataMiner.MediaOps.Live.Subscriptions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Cache of the current values of the rows in a table.
	/// </summary>
	internal sealed class TableCache
	{
		private sealed class NoChange
		{
			public static readonly object Value = new NoChange();
		}

		private readonly object _lock = new object();
		private readonly IDmsElement _element;
		private readonly int _tableId;

		public TableCache(IDmsElement element, int tableId)
		{
			_element = element ?? throw new ArgumentNullException(nameof(element));
			_tableId = tableId;
		}

		public IDictionary<string, object[]> Rows { get; } = new Dictionary<string, object[]>();

		internal TableValueChange ApplyUpdate(ParameterChangeEventMessage message)
		{
			if (message == null)
			{
				throw new ArgumentNullException(nameof(message));
			}

			lock (_lock)
			{
				IDictionary<string, object[]> updatedRows;
				ICollection<string> deletedRows;

				if (message is ParameterTableUpdateEventMessage parameterTableUpdateEventMessage)
				{
					updatedRows = ApplyUpdatedRows(parameterTableUpdateEventMessage);
					deletedRows = ApplyDeletedRows(parameterTableUpdateEventMessage);
				}
				else
				{
					updatedRows = ApplyUpdatedRows(message);
					deletedRows = Array.Empty<string>();
				}

				return new TableValueChange(_element, _tableId, updatedRows, deletedRows);
			}
		}

		private IDictionary<string, object[]> ApplyUpdatedRows(ParameterTableUpdateEventMessage update)
		{
			var dict = BuildDictionary(update);

			return ApplyUpdatedRows(dict);
		}

		private IDictionary<string, object[]> ApplyUpdatedRows(ParameterChangeEventMessage update)
		{
			var dict = BuildDictionary(update);

			return ApplyUpdatedRows(dict);
		}

		private IDictionary<string, object[]> ApplyUpdatedRows(IDictionary<string, object[]> update)
		{
			var updatedRows = new Dictionary<string, object[]>();

			foreach (var r in update)
			{
				if (ApplyRowUpdate(r.Key, r.Value))
				{
					updatedRows.Add(r.Key, Rows[r.Key]);
				}
			}

			return updatedRows;
		}

		private ICollection<string> ApplyDeletedRows(ParameterTableUpdateEventMessage update)
		{
			if (update.DeletedRows == null)
			{
				return Array.Empty<string>();
			}

			var deletedRows = new List<string>();

			foreach (var r in update.DeletedRows)
			{
				if (Rows.Remove(r))
				{
					deletedRows.Add(r);
				}
			}

			return deletedRows;
		}

		private bool ApplyRowUpdate(string key, object[] newValues)
		{
			bool hasChanges = false;

			if (!Rows.TryGetValue(key, out var cachedRow))
			{
				cachedRow = new object[newValues.Length];
				Rows.Add(key, newValues);

				hasChanges = true;
			}

			if (cachedRow.Length < newValues.Length)
			{
				// should not be possible, but you never know...
				Array.Resize(ref cachedRow, newValues.Length);
				hasChanges = true;
			}

			for (int i = 0; i < newValues.Length; i++)
			{
				// Empty value means that the value is not changed.
				if (Equals(newValues[i], NoChange.Value))
					continue;

				if (Equals(newValues[i], cachedRow[i]))
					continue;

				cachedRow[i] = newValues[i];
				hasChanges = true;
			}

			return hasChanges;
		}

		private IDictionary<string, object[]> BuildDictionary(ParameterTableUpdateEventMessage message)
		{
			var result = new Dictionary<string, object[]>();

			if (message.NewValue == null || message.NewValue.ArrayValue == null)
			{
				return result;
			}

			foreach (var updatedRow in message.UpdatedRows)
			{
				var array = updatedRow.ArrayValue
					.Select(pv => pv.IsEmpty ? NoChange.Value : pv.CellValue.InteropValue)
					.ToArray();

				if (array.Length == 0)
				{
					continue;
				}

				if (message.IndexColumnID >= array.Length)
				{
					throw new InvalidOperationException("Couldn't find key of row");
				}

				var key = Convert.ToString(array[message.IndexColumnID]);
				result[key] = array;
			}

			return result;
		}

		private IDictionary<string, object[]> BuildDictionary(ParameterChangeEventMessage message, int keyColumnIndex = 0)
		{
			var result = new Dictionary<string, object[]>();

			if (message.NewValue == null || message.NewValue.ArrayValue == null)
			{
				return result;
			}

			var columns = message.NewValue.ArrayValue;

			if (keyColumnIndex >= columns.Length)
			{
				throw new ArgumentException("Invalid key column index.", nameof(keyColumnIndex));
			}

			// Dictionary used as a mapping from index to key.
			var keyColumn = columns[keyColumnIndex].ArrayValue;
			var keyMap = new string[keyColumn.Length];

			for (int rowIndex = 0; rowIndex < keyColumn.Length; rowIndex++)
			{
				var keyCell = keyColumn[rowIndex];
				var primaryKey = keyCell.CellValue.StringValue;

				result[primaryKey] = new object[columns.Length];
				keyMap[rowIndex] = primaryKey;
			}

			// Fill the result dictionary with values from the columns.
			for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
			{
				var column = columns[columnIndex];

				for (int rowIndex = 0; rowIndex < column.ArrayValue.Length; rowIndex++)
				{
					var cell = column.ArrayValue[rowIndex];
					var cellValue = cell.IsEmpty ? NoChange.Value : cell.CellValue.InteropValue;

					result[keyMap[rowIndex]][columnIndex] = cellValue;
				}
			}

			return result;
		}
	}
}
