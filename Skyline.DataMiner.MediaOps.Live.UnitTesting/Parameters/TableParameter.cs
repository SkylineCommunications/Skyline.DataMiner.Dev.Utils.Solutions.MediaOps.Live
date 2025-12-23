namespace Skyline.DataMiner.MediaOps.Live.UnitTesting.Parameters
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.UnitTesting.Simulation;
	using Skyline.DataMiner.Net.Messages;

	public class TableParameter : ParameterBase
	{
		private readonly ConcurrentDictionary<string, object[]> _rows = new();

		public TableParameter(SimulatedElement element, int id) : base(element, id)
		{
		}

		public int TableId => Id;

		public IReadOnlyDictionary<string, object[]> Rows => _rows;

		public void SetRow(string key, object[] row)
		{
			if (String.IsNullOrEmpty(key))
			{
				throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
			}

			if (row is null)
			{
				throw new ArgumentNullException(nameof(row));
			}

			_rows[key] = row;

			// send subscription event
			var e = new ParameterTableUpdateEventMessage(Element.DmaId, Element.ElementId, TableId)
			{
				NewValue = ToParameterValue(row),
			};

			Element.Dma.NotifySubscriptions(e);
		}

		public void DeleteRow(string key)
		{
			if (String.IsNullOrEmpty(key))
			{
				throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
			}

			if (_rows.TryRemove(key, out _))
			{
				// send subscription event
				var e = new ParameterTableUpdateEventMessage(Element.DmaId, Element.ElementId, TableId)
				{
					DeletedRows = [key],
				};
				Element.Dma.NotifySubscriptions(e);
			}
		}

		internal ParameterValue ToParameterValue()
		{
			var rowList = _rows.Values.ToList();

			var columnCount = rowList.Count > 0 ? rowList.Max(x => x.Length) : 0;

			if (columnCount == 0)
			{
				// Create at least one column that represents the keys.
				columnCount = 1;
			}

			var columns = new object[columnCount];

			for (int c = 0; c < columnCount; c++)
			{
				var columnValues = new object[rowList.Count];

				for (int r = 0; r < rowList.Count; r++)
				{
					var row = rowList[r];

					var cellData = new object[7];
					cellData[0] = c < row.Length ? row[c] : null;

					columnValues[r] = cellData;
				}

				columns[c] = columnValues;
			}

			return ParameterValue.Compose(columns);
		}

		private ParameterValue ToParameterValue(object[] row)
		{
			var columnCount = row.Length;

			if (columnCount == 0)
			{
				// Create at least one column that represents the keys.
				columnCount = 1;
			}

			var cells = new object[columnCount];

			for (int c = 0; c < columnCount; c++)
			{
				var cellData = new object[7];
				cellData[0] = c < row.Length ? row[c] : null;

				cells[c] = cellData;
			}

			return ParameterValue.Compose(new[] { cells });
		}
	}
}
