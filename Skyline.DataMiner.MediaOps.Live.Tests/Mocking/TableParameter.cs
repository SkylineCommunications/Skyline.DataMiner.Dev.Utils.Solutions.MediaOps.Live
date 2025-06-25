namespace Skyline.DataMiner.MediaOps.Live.Tests.Mocking
{
	using System;
	using System.Collections.Concurrent;

	using Skyline.DataMiner.Net.Messages;

	internal class TableParameter
	{
		private readonly ConcurrentDictionary<string, object[]> _rows = new();

		public TableParameter(int id)
		{
			Id = id;

			_rows.TryAdd("Row1", new object[] { "Value1", "Value2" });
		}

		public int Id { get; }

		public IReadOnlyDictionary<string, object[]> Rows => _rows;

		internal ParameterValue ToParameterValue()
		{
			var rowList = _rows.Values.ToList();
			var columnCount = rowList.Count > 0 ? rowList.Max(x => x.Length) : 1;
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
