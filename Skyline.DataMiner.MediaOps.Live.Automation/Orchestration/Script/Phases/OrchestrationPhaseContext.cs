namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using Skyline.DataMiner.Automation;

	/// <summary>
	/// Provides the context in which a phase or its status method runs.
	/// </summary>
	public sealed class OrchestrationPhaseContext
	{
		internal const string NoNodeOfEvent = "The job context is only available when the script runs for a node of an event.";

		internal OrchestrationPhaseContext(IEngine engine, OrchestrationJobContext job = null, OrchestrationNodeContext node = null)
		{
			Engine = engine;
			Job = job ?? new OrchestrationJobContext(OrchestrationJobValues.Unavailable(NoNodeOfEvent));
			Node = node ?? new OrchestrationNodeContext(null, null);
		}

		/// <summary>
		/// Gets the link with the SLAutomation process.
		/// </summary>
		public IEngine Engine { get; }

		/// <summary>
		/// Gets the job the orchestration runs for, with the job context values of the current node.
		/// </summary>
		public OrchestrationJobContext Job { get; }

		/// <summary>
		/// Gets the node the orchestration runs for.
		/// </summary>
		public OrchestrationNodeContext Node { get; }
	}
}
