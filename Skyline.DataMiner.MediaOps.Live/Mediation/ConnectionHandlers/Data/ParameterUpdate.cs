namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data
{
	using System.Collections.Generic;
	using System.Text;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	/// <summary>
	/// Contains information about a parameter update on a DataMiner element.
	/// </summary>
	public class ParameterUpdate
	{
		/// <summary>
		/// Gets or sets the agent ID.
		/// </summary>
		public int AgentId { get; set; }

		/// <summary>
		/// Gets or sets the element ID.
		/// </summary>
		public int ElementId { get; set; }

		/// <summary>
		/// Gets the DataMiner element ID.
		/// </summary>
		[JsonIgnore]
		public DmsElementId DmsElementId => new(AgentId, ElementId);

		/// <summary>
		/// Gets or sets the parameter ID.
		/// </summary>
		public int ParameterId { get; set; }

		/// <summary>
		/// Gets a value indicating whether this update should be force pushed.
		/// </summary>
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool IsForcePush { get; }

		/// <summary>
		/// Gets or sets the old value of the parameter.
		/// </summary>
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public object OldValue { get; set; }

		/// <summary>
		/// Gets or sets the new value of the parameter.
		/// </summary>
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public object NewValue { get; set; }

		/// <summary>
		/// Gets or sets the updated rows for table parameters.
		/// </summary>
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public IDictionary<string, object[]> UpdatedRows { get; set; }

		/// <summary>
		/// Gets or sets the deleted rows for table parameters.
		/// </summary>
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public IDictionary<string, object[]> DeletedRows { get; set; }

		/// <summary>
		/// Returns a string representation of the parameter update.
		/// </summary>
		/// <returns>A string containing the update information.</returns>
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
