namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

	/// <summary>
	/// The job the orchestration runs for.
	/// </summary>
	public sealed class OrchestrationJobContext
	{
		internal const string NoEvent = "The job is only available when the script runs for an event.";

		private readonly Lazy<OrchestrationJobInfo> _info;

		internal OrchestrationJobContext(Func<OrchestrationJobInfo> loadInfo)
		{
			_info = new Lazy<OrchestrationJobInfo>(() => (loadInfo ?? throw new InvalidOperationException(NoEvent))()
				?? throw new InvalidOperationException("The job of the event could not be found."));
		}

		/// <summary>
		/// Gets the reference of the job.
		/// </summary>
		public string Reference => _info.Value.JobReference;

		/// <summary>
		/// Gets the name of the job.
		/// </summary>
		public string Name => _info.Value.JobName;

		/// <summary>
		/// Gets the monitoring service of the job, or the default value when the job has none.
		/// The job is read once per execution, when it is first accessed.
		/// </summary>
		public DmsServiceId MonitoringService => _info.Value.MonitoringService;

	}
}
