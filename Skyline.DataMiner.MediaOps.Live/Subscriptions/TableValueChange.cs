namespace Skyline.DataMiner.MediaOps.Live.Subscriptions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	internal sealed class TableValueChange
	{
		public TableValueChange(
			IDmsElement element,
			int tableId,
			IDictionary<string, object[]> updatedRows,
			ICollection<string> deletedRows)
		{
			Element = element ?? throw new ArgumentNullException(nameof(element));
			TableId = tableId;

			UpdatedRows = updatedRows ?? throw new ArgumentNullException(nameof(updatedRows));
			DeletedRows = deletedRows ?? throw new ArgumentNullException(nameof(deletedRows));
		}

		public IDmsElement Element { get; }

		public int TableId { get; }

		public IDictionary<string, object[]> UpdatedRows { get; }

		public ICollection<string> DeletedRows { get; }

		public override string ToString()
		{
			return $"{nameof(TableValueChange)} ({UpdatedRows.Count} updated, {DeletedRows.Count} removed)";
		}
	}
}
