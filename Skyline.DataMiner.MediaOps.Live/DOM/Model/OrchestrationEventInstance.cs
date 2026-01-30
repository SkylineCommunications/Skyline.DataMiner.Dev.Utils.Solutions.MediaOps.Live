namespace Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration
{
	internal partial class OrchestrationEventInstance
	{
		/// <summary>
		/// Apply default setting after initializing the instance.
		/// </summary>
		protected sealed override void AfterLoad()
		{
			if (!OrchestrationEventInfo.EventState.HasValue)
			{
				OrchestrationEventInfo.EventState = SlcOrchestrationIds.Enums.EventState.Draft;
			}

			if (!OrchestrationEventInfo.EventType.HasValue)
			{
				OrchestrationEventInfo.EventType = SlcOrchestrationIds.Enums.EventType.Other;
			}
		}
	}
}
