namespace Skyline.DataMiner.MediaOps.Live.Mediation.Data
{
	using System.Collections.Generic;
	using System.Text;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	public class ParameterUpdate
	{
		public int AgentId { get; set; }

		public int ElementId { get; set; }

		public DmsElementId DmsElementId => new(AgentId, ElementId);

		public int ParameterId { get; set; }

		public object OldValue { get; set; }

		public object NewValue { get; set; }

		public bool IsForcePush { get; }

		public IDictionary<string, object[]> UpdatedRows { get; set; }

		public IDictionary<string, object[]> DeletedRows { get; set; }

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.Append($"{nameof(ParameterUpdate)} {AgentId}/{ElementId}/{ParameterId}");

			int updatedCount = UpdatedRows?.Count ?? 0;
			int deletedCount = DeletedRows?.Count ?? 0;

			if (updatedCount > 0 || deletedCount > 0)
			{
				sb.Append($" ({updatedCount} updated, {deletedCount} removed)");
			}

			return sb.ToString();
		}
	}
}
