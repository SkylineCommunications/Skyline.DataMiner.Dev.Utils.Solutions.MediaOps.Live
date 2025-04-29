namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	/// <summary>
	/// This object groups the orchestration events belonging to the same job.
	/// </summary>
	public class OrchestrationJob
	{
		private static readonly List<SlcOrchestrationIds.Enums.EventType> StartTypes = new List<SlcOrchestrationIds.Enums.EventType>
		{
			SlcOrchestrationIds.Enums.EventType.Start,
			SlcOrchestrationIds.Enums.EventType.Prerollstart,
		};

		private static readonly List<SlcOrchestrationIds.Enums.EventType> StopTypes = new List<SlcOrchestrationIds.Enums.EventType>
		{
			SlcOrchestrationIds.Enums.EventType.Stop,
			SlcOrchestrationIds.Enums.EventType.Postrollstop,
		};

		private static readonly List<SlcOrchestrationIds.Enums.EventType> ExpectedOrderOfTypes = new List<SlcOrchestrationIds.Enums.EventType>
		{
			SlcOrchestrationIds.Enums.EventType.Start,
			SlcOrchestrationIds.Enums.EventType.Prerollstart,
			SlcOrchestrationIds.Enums.EventType.Prerollstop,
			SlcOrchestrationIds.Enums.EventType.Postrollstart,
			SlcOrchestrationIds.Enums.EventType.Postrollstop,
			SlcOrchestrationIds.Enums.EventType.Stop,
		};

		/// <summary>
		/// Holds the list of event IDs at the start of this objects creation.
		/// </summary>
		private readonly IEnumerable<Guid> _initialEventIds;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationJob"/> class, with an empty list of events.
		/// </summary>
		/// <param name="jobId">The reference ID of the job.</param>
		public OrchestrationJob(Guid jobId) : this(jobId, new List<OrchestrationEvent>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationJob"/> class, with a given list of events.
		/// </summary>
		/// <param name="jobId">The reference ID of the job.</param>
		/// <param name="orchestrationEvents">The list of events to assign to the job.</param>
		public OrchestrationJob(Guid jobId, IEnumerable<OrchestrationEvent> orchestrationEvents)
		{
			JobId = jobId;

			IEnumerable<OrchestrationEvent> events = orchestrationEvents.ToList();
			OrchestrationEvents = events.ToList();
			_initialEventIds = events.Select(e => e.ID);
		}

		/// <summary>
		/// Gets the job reference ID.
		/// </summary>
		public Guid JobId { get; }

		public IEnumerable<Guid> RemovedIds => _initialEventIds.Except(OrchestrationEvents.Select(e => e.ID));

		/// <summary>
		/// Gets the list of currently assigned events to the job. To save edits on DataMiner, use the CreateOrUpdateOrchestrationJob method from the main API.
		/// </summary>
		public IList<OrchestrationEvent> OrchestrationEvents { get; internal set; }

		private static void ValidateEventTypesBeforeSaving(IList<OrchestrationEvent> orchestrationEvents)
		{
			if (orchestrationEvents.All(e => e.EventType == SlcOrchestrationIds.Enums.EventType.Other))
			{
				return;
			}

			OrchestrationEvent startEvent;
			OrchestrationEvent stopEvent;
			try
			{
				startEvent = orchestrationEvents.SingleOrDefault(e => StartTypes.Contains(e.EventType));
				stopEvent = orchestrationEvents.SingleOrDefault(e => StopTypes.Contains(e.EventType));
			}
			catch (InvalidOperationException)
			{
				throw new InvalidOperationException("Job can have only a single starting event (Start, PrerollStart) and a single ending event (Stop, PostrollStop).");
			}

			if (startEvent == null || stopEvent == null)
			{
				throw new InvalidOperationException("Job must have a starting event (Start, PrerollStart) and an ending event (Stop, PostrollStop).");
			}
		}

		private static void ValidateEventOrderBeforeSaving(IList<OrchestrationEvent> orchestrationEvents)
		{
			var eventWithoutOtherType = orchestrationEvents.Where(e => e.EventType != SlcOrchestrationIds.Enums.EventType.Other);

			var orderedByExpectedTypeOrder = eventWithoutOtherType.OrderBy(e => ExpectedOrderOfTypes.IndexOf(e.EventType)).ToList();

			for (int i = 0; i < orderedByExpectedTypeOrder.Count - 1; i++)
			{
				var earlierEvent = orderedByExpectedTypeOrder[i];
				var laterEvent = orderedByExpectedTypeOrder[i + 1];

				if (earlierEvent.EventTime > laterEvent.EventTime)
				{
					throw new InvalidOperationException($"Event of type {laterEvent.EventType.ToString()} can not be scheduled before an event of type {earlierEvent.EventType.ToString()} ");
				}
			}
		}

		internal void ValidateEventsBeforeSaving()
		{
			AssignJobReferencesBeforeSaving(JobId, OrchestrationEvents);
			ValidateEventInfo(OrchestrationEvents);
		}

		internal static void ValidateEventInfo(IList<OrchestrationEvent> orchestrationEvents)
		{
			ValidateEventTypesBeforeSaving(orchestrationEvents);
			ValidateEventOrderBeforeSaving(orchestrationEvents);
		}

		internal static void AssignJobReferencesBeforeSaving(Guid jobId, IList<OrchestrationEvent> orchestrationEvents)
		{
			foreach (OrchestrationEvent orchestrationEvent in orchestrationEvents)
			{
				if (orchestrationEvent.JobReference == Guid.Empty)
				{
					orchestrationEvent.JobReference = jobId;
					continue;
				}

				if (orchestrationEvent.JobReference != jobId)
				{
					throw new InvalidOperationException("One of the job events is already part of another job");
				}
			}
		}
	}
}
