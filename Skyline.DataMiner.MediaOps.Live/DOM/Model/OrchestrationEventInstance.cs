namespace Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration
{
	public partial class OrchestrationEventInstance
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
