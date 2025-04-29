namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using DOM.Model.SlcOrchestration;

	using Repositories.SlcOrchestration;

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

		private IList<OrchestrationEvent> _orchestrationEvents;
		private readonly IEnumerable<Guid> _initialEventIds;

		public OrchestrationJob(Guid jobId) : this (jobId, new List<OrchestrationEvent>())
		{
		}

		public OrchestrationJob(Guid jobId, IEnumerable<OrchestrationEvent> orchestrationEvents)
		{
			JobId = jobId;

			IEnumerable<OrchestrationEvent> events = orchestrationEvents.ToList();
			_orchestrationEvents = events.ToList();
			_initialEventIds = events.Select(e => e.ID);
		}

		public Guid JobId { get; }

		internal IEnumerable<Guid> RemovedIds => _initialEventIds.Except(_orchestrationEvents.Select(e => e.ID));

		public IList<OrchestrationEvent> OrchestrationEvents
		{
			get
			{
				return _orchestrationEvents;
			}

			internal set
			{
				_orchestrationEvents = value;
			}
		}

		internal void ValidateEventsBeforeSaving()
		{
			AssignJobReferencesBeforeSaving();
			ValidateEventTypesBeforeSaving();
			ValidateEventOrderBeforeSaving();
		}

		private void AssignJobReferencesBeforeSaving()
		{
			foreach (OrchestrationEvent orchestrationEvent in _orchestrationEvents)
			{
				if (orchestrationEvent.JobReference == Guid.Empty)
				{
					orchestrationEvent.JobReference = JobId;
					continue;
				}

				if (orchestrationEvent.JobReference != JobId)
				{
					throw new InvalidOperationException("One of the job events is already part of another job");
				}
			}
		}

		private void ValidateEventTypesBeforeSaving()
		{
			if (_orchestrationEvents.All(e => e.EventType == SlcOrchestrationIds.Enums.EventType.Other))
			{
				return;
			}

			OrchestrationEvent startEvent;
			OrchestrationEvent stopEvent;
			try
			{
				startEvent = _orchestrationEvents.SingleOrDefault(e => StartTypes.Contains(e.EventType));
				stopEvent = _orchestrationEvents.SingleOrDefault(e => StopTypes.Contains(e.EventType));
			}
			catch (InvalidOperationException e)
			{
				throw new InvalidOperationException("Job can have only a single starting event (Start, PrerollStart) and a single ending event (Stop, PostrollStop).");
			}

			if (startEvent == null || stopEvent == null)
			{
				throw new InvalidOperationException("Job must have a starting event (Start, PrerollStart) and an ending event (Stop, PostrollStop).");
			}
		}

		private void ValidateEventOrderBeforeSaving()
		{
			var eventWithoutOtherType = _orchestrationEvents.Where(e => e.EventType != SlcOrchestrationIds.Enums.EventType.Other);

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
	}
}
