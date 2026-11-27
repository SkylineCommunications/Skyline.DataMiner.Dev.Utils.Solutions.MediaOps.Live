namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;

	/// <summary>
	/// The node the orchestration runs for.
	/// </summary>
	public sealed class OrchestrationNodeContext
	{
		private readonly OrchestrationNodeValues _values;

		internal OrchestrationNodeContext(string id, string label, OrchestrationNodeValues values)
		{
			Id = id;
			Label = label;
			_values = values ?? throw new ArgumentNullException(nameof(values));
		}

		/// <summary>
		/// Gets the ID of the node, or <see langword="null"/> when the script does not run for a node of an event.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Gets the label of the node, or <see langword="null"/> when it has none.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// Gets the read-only values of this node, kept for the lifetime of the job.
		/// </summary>
		public IOrchestrationValues Values => _values.Values;

		/// <summary>
		/// Gets the writable values of this node, or <see langword="null"/> when the script does not run for a node of an event.
		/// </summary>
		public IOrchestrationWritableValues WritableValues => _values.WritableValues;

		/// <summary>
		/// Gets a read-only view on the values of another node of the job.
		/// </summary>
		/// <param name="node">The ID or label of a node of the current event, or the ID of a node of another event of the job.</param>
		/// <returns>The values of that node.</returns>
		public IOrchestrationValues ForNode(string node) => _values.ForNode(node);
	}
}
