namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	/// <summary>
	/// Bundles orchestration input items that belong together.
	/// </summary>
	public class OrchestrationInputGroup : OrchestrationInputItem
	{
		/// <inheritdoc/>
		public override string Kind => OrchestrationInputKind.Group;

		/// <summary>
		/// Gets the items that belong to this group.
		/// </summary>
		[JsonProperty("children")]
		public List<OrchestrationInputItem> Children { get; } = new List<OrchestrationInputItem>();

		/// <summary>
		/// Gets the fields that directly belong to this group.
		/// </summary>
		[JsonIgnore]
		public IEnumerable<OrchestrationInputField> Fields => Children.OfType<OrchestrationInputField>();

		/// <summary>
		/// Gets the groups that directly belong to this group.
		/// </summary>
		[JsonIgnore]
		public IEnumerable<OrchestrationInputGroup> Groups => Children.OfType<OrchestrationInputGroup>();
	}
}
