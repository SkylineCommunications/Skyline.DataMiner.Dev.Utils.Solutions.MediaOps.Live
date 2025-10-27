namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Net;

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
			JobInfo = new OrchestrationJobInfo
			{
				JobReference = jobId,
			};

			List<OrchestrationEventConfiguration> events = orchestrationEventConfigurations.ToList();
			OrchestrationEvents = events;
			_initialEventIds = events.Select(e => e.ID).ToList();
		}

		/// <summary>
		/// Gets the job reference ID.
		/// </summary>
		public string JobId => JobInfo.JobReference;

		internal OrchestrationJobInfo JobInfo { get; set; }

		internal IEnumerable<Guid> RemovedIds => _initialEventIds.Except(OrchestrationEvents.Select(e => e.ID));

		/// <summary>
		/// Gets the list of orchestration events relating to this job.
		/// </summary>
		public List<OrchestrationEventConfiguration> OrchestrationEvents { get; }

		private static void ValidateConfigurationsBeforeSaving(IEnumerable<OrchestrationEvent> orchestrationEventConfigurations)
		{
			// IEnumerable<OrchestrationEvent> configurations = orchestrationEventConfigurations.ToList();
			// To be implemented
		}

		internal void ValidateEventsBeforeSaving(IConnection connection)
		{
			AssignJobReferencesBeforeSaving(JobInfo.ID, OrchestrationEvents.ToList());
			IEnumerable<OrchestrationEvent> events = OrchestrationEvents.ToList();
			OrchestrationJob.ValidateEventInfo(events.ToList());
			ValidateConfigurationsBeforeSaving(OrchestrationEvents);
			ValidateOrchestrationScriptInformation(connection, OrchestrationEvents);
		}

		internal virtual void ValidateOrchestrationScriptInformation(IConnection connection, List<OrchestrationEventConfiguration> orchestrationEvents)
		{
			foreach (OrchestrationEventConfiguration orchestrationEvent in orchestrationEvents)
			{
				if (orchestrationEvent.EventState != EventState.Confirmed)
				{
					continue;
				}

				OrchestrationJob.ValidateOrchestrationScriptInput(
					connection,
					orchestrationEvent.GlobalOrchestrationScript,
					orchestrationEvent.GlobalOrchestrationScriptArguments.ToList(),
					orchestrationEvent.Profile.Values.ToList());

				foreach (NodeConfiguration configurationNodeConfiguration in orchestrationEvent.Configuration.NodeConfigurations)
				{
					OrchestrationJob.ValidateOrchestrationScriptInput(
						connection,
						configurationNodeConfiguration.OrchestrationScriptName,
						configurationNodeConfiguration.OrchestrationScriptArguments.ToList(),
						configurationNodeConfiguration.Profile.Values.ToList());
				}
			}
		}

		internal static void AssignJobReferencesBeforeSaving(Guid jobInfoReference, IList<OrchestrationEventConfiguration> orchestrationEvents)
		{
			foreach (OrchestrationEventConfiguration orchestrationEvent in orchestrationEvents)
			{
				if (!orchestrationEvent.JobInfoReference.HasValue || orchestrationEvent.JobInfoReference.Value.ID == Guid.Empty)
				{
					orchestrationEvent.JobInfoReference = new ApiObjectReference<OrchestrationJobInfo>(jobInfoReference);
					continue;
				}

				if (orchestrationEvent.JobInfoReference.Value.ID != jobInfoReference)
				{
					throw new InvalidOperationException($"One of the job events is already part of another job (reference: {orchestrationEvent.JobInfoReference.Value.ID}");
				}
			}
		}
	}
}