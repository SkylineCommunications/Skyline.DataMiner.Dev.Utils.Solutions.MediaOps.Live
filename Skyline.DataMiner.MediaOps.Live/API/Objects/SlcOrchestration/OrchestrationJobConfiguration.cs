namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// This object groups the orchestration event configurations belonging to the same job.
	/// </summary>
	public class OrchestrationJobConfiguration : OrchestrationJob
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationJobConfiguration"/> class, with an empty list of events.
		/// </summary>
		/// <param name="jobId">The reference ID of the job.</param>
		public OrchestrationJobConfiguration(Guid jobId) : this (jobId, new List<OrchestrationEventConfiguration>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationJobConfiguration"/> class, with a given list of events.
		/// </summary>
		/// <param name="jobId">The reference ID of the job.</param>
		/// <param name="orchestrationEventConfigurations">The list of events to assign to the job.</param>
		public OrchestrationJobConfiguration(Guid jobId, IEnumerable<OrchestrationEventConfiguration> orchestrationEventConfigurations) : base (jobId, orchestrationEventConfigurations)
		{
			var events = orchestrationEventConfigurations.ToList();
			OrchestrationEvents = events;
		}

		internal new IEnumerable<Guid> RemovedIds => InitialEventIds.Except(OrchestrationEvents.Select(e => e.ID));

		public new IList<OrchestrationEventConfiguration> OrchestrationEvents { get; internal set; }

		private void ValidateConfigurationsBeforeSaving(IEnumerable<OrchestrationEvent> orchestrationEventConfigurations)
		{
			// IEnumerable<OrchestrationEvent> configurations = orchestrationEventConfigurations.ToList();
			// To be implemented
		}

		internal new void ValidateEventsBeforeSaving()
		{
			IEnumerable<OrchestrationEvent> events = OrchestrationEvents.ToList();
			ValidateEventInfo(events.ToList());
			ValidateConfigurationsBeforeSaving(OrchestrationEvents);
		}
	}
}
