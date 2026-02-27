namespace Skyline.DataMiner.Solutions.MediaOps.Live.Plan
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

	public interface IMediaOpsPlanHelper
	{
		void UpdateJobState(OrchestrationEvent orchestrationEvent);
	}
}
