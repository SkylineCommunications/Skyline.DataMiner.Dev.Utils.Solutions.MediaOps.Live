namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	///     This object groups the orchestration event configurations belonging to the same job.
	/// </summary>
	public class OrchestrationJobConfiguration
	{
		/// <summary>
		///     Holds the list of event IDs at the start of this objects creation.
		/// </summary>
		private readonly List<Guid> _initialEventIds;

		/// <summary>
		///     Initializes a new instance of the <see cref="OrchestrationJobConfiguration" /> class, with an empty list of events.
		/// </summary>
		/// <param name="jobId">The reference ID of the job.</param>
		internal OrchestrationJobConfiguration(string jobId) : this(jobId, new List<OrchestrationEventConfiguration>())
		{
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="OrchestrationJobConfiguration" /> class, with a given list of events.
		/// </summary>
		/// <param name="jobId">The reference ID of the job.</param>
		/// <param name="orchestrationEventConfigurations">The list of events to assign to the job.</param>
		internal OrchestrationJobConfiguration(string jobId, IEnumerable<OrchestrationEventConfiguration> orchestrationEventConfigurations)
		{
			JobId = jobId;
			List<OrchestrationEventConfiguration> events = orchestrationEventConfigurations.ToList();
			OrchestrationEvents = events;
			_initialEventIds = events.Select(e => e.ID).ToList();
		}

		/// <summary>
		///     Gets the job reference ID.
		/// </summary>
		public string JobId { get; }

		internal IEnumerable<Guid> RemovedIds => _initialEventIds.Except(OrchestrationEvents.Select(e => e.ID));

		public List<OrchestrationEventConfiguration> OrchestrationEvents { get; }

		private static void ValidateConfigurationsBeforeSaving(IEnumerable<OrchestrationEvent> orchestrationEventConfigurations)
		{
			// IEnumerable<OrchestrationEvent> configurations = orchestrationEventConfigurations.ToList();
			// To be implemented
		}

		internal void ValidateEventsBeforeSaving()
		{
			AssignJobReferencesBeforeSaving(JobId, OrchestrationEvents.ToList());
			IEnumerable<OrchestrationEvent> events = OrchestrationEvents.ToList();
			OrchestrationJob.ValidateEventInfo(events.ToList());
			ValidateConfigurationsBeforeSaving(OrchestrationEvents);
		}

		internal static void AssignJobReferencesBeforeSaving(string jobId, IList<OrchestrationEventConfiguration> orchestrationEvents)
		{
			foreach (OrchestrationEventConfiguration orchestrationEvent in orchestrationEvents)
			{
				if (String.IsNullOrEmpty(orchestrationEvent.JobReference))
				{
					orchestrationEvent.JobReference = jobId;
					continue;
				}

				if (orchestrationEvent.JobReference != jobId) throw new InvalidOperationException("One of the job events is already part of another job");
			}
		}
	}
}