namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	/// <summary>
	/// A node of the event the orchestration runs for.
	/// </summary>
	public sealed class OrchestrationEventNode
	{
		internal OrchestrationEventNode(string id, string label, string orchestrationScriptName)
		{
			Id = id;
			Label = label;
			OrchestrationScriptName = orchestrationScriptName;
		}

		/// <summary>
		/// Gets the ID of the node. It is the same for all events of the job.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Gets the label (alias) of the node, or <see langword="null"/> when it has none.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// Gets the name of the orchestration script of the node, or <see langword="null"/> when it has none.
		/// </summary>
		public string OrchestrationScriptName { get; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return string.IsNullOrWhiteSpace(Label) ? $"Node '{Id}'" : $"Node '{Label}' ({Id})";
		}
	}
}
