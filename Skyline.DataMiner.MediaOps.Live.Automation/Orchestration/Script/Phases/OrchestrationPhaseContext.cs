namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using Skyline.DataMiner.Automation;

	/// <summary>
	/// Provides the context in which a phase or its status method runs.
	/// </summary>
	public sealed class OrchestrationPhaseContext
	{
		internal const string NoNodeOfEvent = "The job context values are only available when the script runs for a node of an event.";

		internal OrchestrationPhaseContext(IEngine engine, OrchestrationJobContext job = null, OrchestrationNodeContext node = null, OrchestrationEventContext orchestrationEvent = null)
		{
			Engine = engine;
			Job = job ?? new OrchestrationJobContext(null, OrchestrationJobValues.Unavailable(NoNodeOfEvent));
			Node = node ?? new OrchestrationNodeContext(null, null);
			Event = orchestrationEvent;
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

		/// <summary>
		/// Gets the definition of the event the orchestration runs for: its nodes and connections.
		/// <see langword="null"/> when the script does not run for an event.
		/// </summary>
		public OrchestrationEventContext Event { get; }
	}
}
