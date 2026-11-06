namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	/// <summary>
	/// The node the orchestration runs for.
	/// </summary>
	public sealed class OrchestrationNodeContext
	{
		internal OrchestrationNodeContext(string id, string label)
		{
			Id = id;
			Label = label;
		}

		/// <summary>
		/// Gets the ID of the node, or <see langword="null"/> when the script does not run for a node of an event.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Gets the label of the node, or <see langword="null"/> when it has none.
		/// </summary>
		public string Label { get; }
	}
}
