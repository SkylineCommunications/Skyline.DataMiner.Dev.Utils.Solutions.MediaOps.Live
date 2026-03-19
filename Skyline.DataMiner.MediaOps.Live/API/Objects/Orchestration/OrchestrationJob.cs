namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;

	/// <summary>
	/// This object groups the orchestration events belonging to the same job.
	/// </summary>
	public class OrchestrationJob
	{
		internal static readonly IReadOnlyList<EventType> StartTypes =
		[
			EventType.Start,
			EventType.PrerollStart,
		];

		internal static readonly IReadOnlyList<EventType> StopTypes =
		[
			EventType.Stop,
			EventType.PostrollStop,
		];

		internal static readonly IReadOnlyList<EventType> ExpectedOrderOfTypes =
		[
			EventType.Start,
			EventType.PrerollStart,
			EventType.PrerollStop,
			EventType.PostrollStart,
			EventType.PostrollStop,
			EventType.Stop,
		];

		public static readonly EventTypeOrderComparer EventTypeOrderComparer = new(ExpectedOrderOfTypes);

		/// <summary>
		/// Holds the list of event IDs at the start of this objects creation.
		/// </summary>
		private readonly List<Guid> _initialEventIds;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationJob"/> class, with an empty list of events.
		/// </summary>
		/// <param name="jobId">The reference ID of the job.</param>
		internal OrchestrationJob(string jobId) : this(jobId, new List<OrchestrationEvent>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationJob"/> class, with a given list of events.
		/// </summary>
		/// <param name="jobId">The reference ID of the job.</param>
		/// <param name="orchestrationEvents">The list of events to assign to the job.</param>
		internal OrchestrationJob(string jobId, IEnumerable<OrchestrationEvent> orchestrationEvents)
		{
			JobInfo = new OrchestrationJobInfo
			{
				JobReference = jobId,
			};

			IEnumerable<OrchestrationEvent> events = orchestrationEvents.ToList();
			OrchestrationEvents = events.ToList();
			_initialEventIds = events.Select(e => e.ID).ToList();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationJob"/> class, with a given list of events.
		/// </summary>
		/// <param name="jobInfo">The job info of the job.</param>
		/// <param name="orchestrationEvents">The list of events to assign to the job.</param>
		internal OrchestrationJob(OrchestrationJobInfo jobInfo, IEnumerable<OrchestrationEvent> orchestrationEvents)
		{
			JobInfo = jobInfo;

			IEnumerable<OrchestrationEvent> events = orchestrationEvents.ToList();
			OrchestrationEvents = events.ToList();
			_initialEventIds = events.Select(e => e.ID).ToList();
		}

		/// <summary>
		/// Gets the job reference ID.
		/// </summary>
		public string JobId => JobInfo.JobReference;

		/// <summary>
		/// Gets the job info object, which contains information about the job, such as the job ID and the shared properties for all job events.
		/// </summary>
		public OrchestrationJobInfo JobInfo { get; }

		internal IEnumerable<Guid> RemovedIds => _initialEventIds.Except(OrchestrationEvents.Select(e => e.ID));

		/// <summary>
		/// Gets the list of currently assigned events to the job. To save edits on DataMiner, use the CreateOrUpdateOrchestrationJob method from the main API.
		/// </summary>
		public List<OrchestrationEvent> OrchestrationEvents { get; }

		private static void ValidateEventTimesBeforeSaving(IList<OrchestrationEvent> orchestrationEvents)
		{
			var now = DateTimeOffset.UtcNow;

			foreach (var e in orchestrationEvents)
			{
				if (e.EventState != EventState.Confirmed)
				{
					continue;
				}

				var timeUntilStart = e.EventTime - now;

				if (timeUntilStart < TimeSpan.Zero)
				{
					throw new InvalidOperationException("Job cannot contain an event with 'Confirmed' state in the past.");
				}

				if (timeUntilStart < TimeSpan.FromSeconds(5))
				{
					throw new InvalidOperationException("Cannot save/update an event with 'Confirmed' state that starts in less than 5 seconds.");
				}
			}
		}

		private static void ValidateEventTypesBeforeSaving(IList<OrchestrationEvent> orchestrationEvents)
		{
			var startCount = orchestrationEvents.Count(x => StartTypes.Contains(x.EventType));
			var stopCount = orchestrationEvents.Count(x => StopTypes.Contains(x.EventType));

			// If all events are "Other", validation is not required
			if (startCount == 0 && stopCount == 0)
			{
				return;
			}

			if (startCount == 0)
			{
				throw new InvalidOperationException("Job must have a starting event (Start, PrerollStart).");
			}

			if (startCount > 1)
			{
				throw new InvalidOperationException("Job can have only a single starting event (Start, PrerollStart).");
			}

			if (stopCount == 0)
			{
				throw new InvalidOperationException("Job must have an ending event (Stop, PostrollStop).");
			}

			if (stopCount > 1)
			{
				throw new InvalidOperationException("Job can have only a single ending event (Stop, PostrollStop).");
			}
		}

		private static void ValidateEventOrderBeforeSaving(IList<OrchestrationEvent> orchestrationEvents)
		{
			var orderedByExpectedTypeOrder = orchestrationEvents
				.Where(e => e.EventType != EventType.Other)
				.OrderBy(e => e.EventType, EventTypeOrderComparer)
				.ToList();

			for (int i = 0; i < orderedByExpectedTypeOrder.Count - 1; i++)
			{
				var earlierEvent = orderedByExpectedTypeOrder[i];
				var laterEvent = orderedByExpectedTypeOrder[i + 1];

				var earlierEventTime = earlierEvent.ActualStartTime ?? earlierEvent.EventTime;
				var laterEventTime = laterEvent.ActualStartTime ?? laterEvent.EventTime;

				if (earlierEventTime > laterEventTime)
				{
					throw new InvalidOperationException($"Event of type {laterEvent.EventType} can not be scheduled before an event of type {earlierEvent.EventType}");
				}
			}
		}

		internal void ValidateEventsBeforeSaving(MediaOpsLiveApi api)
		{
			AssignJobReferencesBeforeSaving(JobInfo.ID, OrchestrationEvents);
			ValidateEventInfo(OrchestrationEvents);
			ValidateOrchestrationScriptInformation(api, OrchestrationEvents);
		}

		internal void ValidateOrchestrationScriptInformation(MediaOpsLiveApi api, List<OrchestrationEvent> orchestrationEvents)
		{
			foreach (OrchestrationEvent orchestrationEvent in orchestrationEvents)
			{
				if (orchestrationEvent.EventState == EventState.Confirmed)
				{
					ValidateOrchestrationScriptInput(
						api,
						orchestrationEvent.GlobalOrchestrationScript,
						orchestrationEvent.GlobalOrchestrationScriptArguments.ToList(),
						orchestrationEvent.Profile.Values.ToList());
				}
			}
		}

		internal static void ValidateOrchestrationScriptInput(MediaOpsLiveApi api, string scriptName, List<OrchestrationScriptArgument> arguments, List<OrchestrationProfileValue> profileValues)
		{
			if (String.IsNullOrEmpty(scriptName))
			{
				return;
			}

			var scriptInfo = api.Orchestration.Scripts.GetOrchestrationScriptInputInfo(scriptName);

			foreach (var scriptInputParam in scriptInfo.Parameters)
			{
				if (arguments.Any(arg => arg.Name == scriptInputParam.Name && arg.Type == OrchestrationScriptArgumentType.Parameter))
				{
					continue;
				}

				if (profileValues.Any(value => value.Name == scriptInputParam.Name))
				{
					continue;
				}

				throw new InvalidOperationException($"Script input parameter missing for confirmed event. Script: {scriptName}. Parameter: {scriptInputParam.Name}");
			}

			foreach (var scriptInputElement in scriptInfo.Elements)
			{
				if (arguments.Any(arg => arg.Name == scriptInputElement.Name && arg.Type == OrchestrationScriptArgumentType.Element))
				{
					continue;
				}

				if (profileValues.Any(value => value.Name == scriptInputElement.Name))
				{
					continue;
				}

				throw new InvalidOperationException($"Script input dummy missing for confirmed event. Script: {scriptName}. Dummy: {scriptInputElement.Name}");
			}
		}

		internal static void ValidateEventInfo(IList<OrchestrationEvent> orchestrationEvents)
		{
			ValidateEventTimesBeforeSaving(orchestrationEvents);
			ValidateEventTypesBeforeSaving(orchestrationEvents);
			ValidateEventOrderBeforeSaving(orchestrationEvents);
		}

		internal static void AssignJobReferencesBeforeSaving(Guid jobInfoReference, IList<OrchestrationEvent> orchestrationEvents)
		{
			foreach (OrchestrationEvent orchestrationEvent in orchestrationEvents)
			{
				if (!orchestrationEvent.JobInfoReference.HasValue || orchestrationEvent.JobInfoReference.Value.ID == Guid.Empty)
				{
					orchestrationEvent.JobInfoReference = new ApiObjectReference<OrchestrationJobInfo>(jobInfoReference);
					continue;
				}

				if (orchestrationEvent.JobInfoReference.Value.ID != jobInfoReference)
				{
					throw new InvalidOperationException("One of the job events is already part of another job");
				}
			}
		}
	}
}
