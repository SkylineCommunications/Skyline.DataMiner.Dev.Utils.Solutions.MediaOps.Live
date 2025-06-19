namespace Skyline.DataMiner.MediaOps.Live
{
	public static class Constants
	{
		public static string MediationProtocolName => "Skyline MediaOps Mediation";

		/// <summary>
		/// The name of the main MediaOps Live Orchestration script.
		/// </summary>
		public static string OrchestrationScriptName = "ORC-AS-EventOrchestration";

		/// <summary>
		/// The name of the MediaOps Live Sliding Window scheduling script.
		/// </summary>
		public static string OrchestrationSlidingWindowSchedulerScriptName = "ORC-AS-SlidingWindowScheduler";

		/// <summary>
		/// Default naming for the orchestration tasks.
		/// </summary>
		public static string OrchestrationTaskNaming = "MediaOps Live Orchestration Event";

		/// <summary>
		/// Default naming for the orchestration tasks.
		/// </summary>
		public static string OrchestrationSlidingWindowSchedulerTaskNaming = "MediaOps Live - Schedule upcoming events";

		/// <summary>
		/// The time range towards the future for which events will be created by the sliding window scheduler.
		/// </summary>
		public static int SchedulerSlidingWindowRangeHours_Future = 12;

		/// <summary>
		/// The time range in which events will be kept (not deleted) by the sliding window scheduler.
		/// </summary>
		public static int SchedulerSlidingWindowRangeHours_Past = 1;

		/// <summary>
		/// The max amount of orchestration events that will be allowed to be created by the sliding window scheduler.
		/// </summary>
		public static int SchedulerMaxTotalOrchestrationEvents = 3000;

		/// <summary>
		/// The sliding window scheduler will be executed every x hours.
		/// </summary>
		public static int SlidingWindowSchedulerExecutionFrequencyInHours = 6;
	}
}
