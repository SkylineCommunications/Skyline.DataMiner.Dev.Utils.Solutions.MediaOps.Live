namespace Skyline.DataMiner.MediaOps.Live.Automation.Plan
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.Automation.API;
	using Skyline.DataMiner.MediaOps.Live.Plan;
	using Skyline.DataMiner.Utils.MediaOps.Common.IOData.Scheduling.Scripts.JobHandler;
	using Skyline.DataMiner.Utils.MediaOps.Common.IOData.Scheduling.Scripts.JobHandler.Enums;

	using OrchestrationEvent = Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration.OrchestrationEvent;

	internal class EngineMediaOpsPlanHelper : MediaOpsPlanHelper
	{
		internal EngineMediaOpsPlanHelper(EngineMediaOpsLiveApi api)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			Engine = api.Engine;
		}

		internal EngineMediaOpsLiveApi Api { get; }

		internal IEngine Engine { get; }

		internal override void UpdateJobState(OrchestrationEvent orchestrationEvent)
		{
			if (orchestrationEvent is null)
			{
				throw new ArgumentNullException(nameof(orchestrationEvent));
			}

			try
			{
				OrchestrationJobInfo info = orchestrationEvent.GetJobInfo(Api);

				if (info == null)
				{
					return;
				}

				SetJobOrchestrationStateAction setStateAction = new SetJobOrchestrationStateAction
				{
					DomJobId = Guid.Parse(info.JobReference),
					Event = GetEventTypeAsPlanJobEvent(orchestrationEvent),
					EventState = orchestrationEvent.EventState == EventState.Failed || !String.IsNullOrEmpty(orchestrationEvent.FailureInfo)
						? OrchestrationEventState.Failed
						: OrchestrationEventState.Succeeded,
					Message = orchestrationEvent.FailureInfo,
				};

				setStateAction.SendToJobHandler(Engine);
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
