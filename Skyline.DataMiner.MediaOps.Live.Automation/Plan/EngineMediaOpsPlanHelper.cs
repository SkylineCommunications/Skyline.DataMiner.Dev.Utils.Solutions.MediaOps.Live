namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Plan
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Plan;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;

	using OrchestrationEvent = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration.OrchestrationEvent;

	internal class EngineMediaOpsPlanHelper : MediaOpsPlanHelper
	{
		internal EngineMediaOpsPlanHelper(EngineMediaOpsLiveApi api)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));

			PlanApi = api.Engine.GetMediaOpsPlanApi();
		}

		internal EngineMediaOpsLiveApi Api { get; }

		internal IMediaOpsPlanApi PlanApi { get; }

		internal override void UpdateJobState(OrchestrationEvent orchestrationEvent)
		{
			if (orchestrationEvent is null)
			{
				throw new ArgumentNullException(nameof(orchestrationEvent));
			}

			try
			{
				var jobInfo = orchestrationEvent.GetJobInfo(Api)
					?? throw new InvalidOperationException("Orchestration event does not have associated job info.");

				var jobId = Guid.Parse(jobInfo.JobReference);
				var eventState = orchestrationEvent.EventState == EventState.Failed || !String.IsNullOrEmpty(orchestrationEvent.FailureInfo)
						? OrchestrationEventState.Failed
						: OrchestrationEventState.Succeeded;

				var updateDetails = new OrchestrationUpdateDetails()
				{
					Event = GetEventTypeAsPlanJobEvent(orchestrationEvent),
					EventState = eventState,
					Message = orchestrationEvent.FailureInfo,
				};

				PlanApi.Jobs.SetOrchestrationState(jobId, updateDetails);
			}
			catch (Exception)
			{
				// No logic needed. Just needs to catch errors in case the events are not related to a PLAN job, which we do not know.
			}
		}

		private OrchestrationEventType GetEventTypeAsPlanJobEvent(OrchestrationEvent orchestrationEvent)
		{
			switch (orchestrationEvent.EventType)
			{
				case EventType.PostrollStart:
				case EventType.Stop:
					return OrchestrationEventType.PostrollStart;

				case EventType.PrerollStart:
				case EventType.Start:
					return OrchestrationEventType.PrerollStart;

				case EventType.PostrollStop:
					return OrchestrationEventType.PostrollStop;

				case EventType.PrerollStop:
					return OrchestrationEventType.PrerollStop;

				default:
					throw new NotSupportedException("Event type cannot be translated to PLAN job event");
			}
		}
	}
}
