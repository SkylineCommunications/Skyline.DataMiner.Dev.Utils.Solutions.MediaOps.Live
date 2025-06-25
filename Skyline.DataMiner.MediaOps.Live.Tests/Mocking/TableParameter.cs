namespace Skyline.DataMiner.MediaOps.Live.Tests.Mocking
{
	using System;
	using System.Collections.Concurrent;

	using Skyline.DataMiner.Net.Messages;

	public class TableParameter
	{
		private readonly ConcurrentDictionary<string, object[]> _rows = new();

		public TableParameter(SimulatedElement element, int id)
		{
			Element = element;
			Id = id;
		}

		public SimulatedElement Element { get; }

		public int Id { get; }

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
				var columnValues = new object?[rowList.Count];

				for (int r = 0; r < rowList.Count; r++)
				{
					var row = rowList[r];

					columnValues[r] = c < row.Length ? row[c] : null;
				}

				columns[c] = columnValues;
			}

			return ParameterValue.Compose(columns);
		}
	}
}
