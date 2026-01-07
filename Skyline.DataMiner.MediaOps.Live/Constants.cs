namespace Skyline.DataMiner.MediaOps.Live
{
	/// <summary>
	/// Contains static fields used in MediaOps Live solution.
	/// </summary>
	public static class Constants
	{
		/// <summary>
		/// Gets the name of the MediaOps.LIVE package.
		/// </summary>
		public static string PackageName => "MediaOps.Live-Package";

		/// <summary>
		/// Gets the name of the MediaOps Live mediation protocol.
		/// </summary>
		public static string MediationProtocolName => "Skyline MediaOps Mediation";

		/// <summary>
		/// The name of the main MediaOps Live Orchestration script.
		/// </summary>
		public const string OrchestrationScriptName = "ORC-AS-EventOrchestration";

		/// <summary>
		/// The name of the MediaOps Live Sliding Window scheduling script.
		/// </summary>
		public const string OrchestrationSlidingWindowSchedulerScriptName = "ORC-AS-SlidingWindowScheduler";

		/// <summary>
		/// Default naming for the orchestration tasks.
		/// </summary>
		public const string OrchestrationTaskNaming = "MediaOps Live Orchestration Event";

		/// <summary>
		/// Default naming for the orchestration tasks.
		/// </summary>
		public const string OrchestrationSlidingWindowSchedulerTaskNaming = "MediaOps Live - Schedule upcoming events";

		/// <summary>
		/// The time range towards the future for which events will be created by the sliding window scheduler.
		/// </summary>
		public const int SchedulerSlidingWindowRangeHours_Future = 12;

		/// <summary>
		/// The time range in which events will be kept (not deleted) by the sliding window scheduler.
		/// </summary>
		public const int SchedulerSlidingWindowRangeHours_Past = 1;

		/// <summary>
		/// The max amount of orchestration events that will be allowed to be created by the sliding window scheduler.
		/// </summary>
		public const int SchedulerMaxTotalOrchestrationEvents = 3000;

		/// <summary>
		/// The sliding window scheduler will be executed every x hours.
		/// </summary>
		public const int SlidingWindowSchedulerExecutionFrequencyInMinutes = 360;
	}
}
