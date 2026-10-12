namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using Newtonsoft.Json;

	/// <summary>
	/// Base class for every item that can appear in an orchestration input definition.
	/// </summary>
	[JsonConverter(typeof(OrchestrationInputItemConverter))]
	public abstract class OrchestrationInputItem
	{
		/// <summary>
		/// Gets the discriminator that identifies the concrete type of this item.
		/// </summary>
		[JsonProperty("kind", Order = -2)]
		public abstract string Kind { get; }

		/// <summary>
		/// Gets or sets the name of this item. It is shown to the operator, it identifies the item within its parent
		/// and it must not contain the path separator.
		/// </summary>
		[JsonProperty("name", Order = -1)]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the full path of this item within the input definition.
		/// </summary>
		[JsonProperty("path")]
		public string Path { get; set; }

		/// <summary>
		/// Gets or sets an optional description that further explains this item to the operator.
		/// </summary>
		[JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
		public string Description { get; set; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return Path ?? Name;
		}
	}
}
