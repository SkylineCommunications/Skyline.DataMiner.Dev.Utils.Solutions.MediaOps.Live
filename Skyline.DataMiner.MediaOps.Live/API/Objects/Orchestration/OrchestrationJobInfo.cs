namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	using System;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Information about the job where this event is part of.
	/// </summary>
	public class OrchestrationJobInfo : ApiObject<OrchestrationJobInfo>
	{
		private readonly OrchestrationJobInfoInstance _domInstance;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationJobInfo"/> class.
		/// </summary>
		public OrchestrationJobInfo() : this(domInstance: new OrchestrationJobInfoInstance())
		{
		}

		internal OrchestrationJobInfo(OrchestrationJobInfoInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal OrchestrationJobInfo(DomInstance domInstance) : this(domInstance: new OrchestrationJobInfoInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcOrchestrationIds.Definitions.OrchestrationJobInfo;

		/// <summary>
		/// Gets or sets the identifier of the job.
		/// </summary>
		public string JobReference
		{
			get
			{
				return _domInstance.JobInfo.JobReference;
			}

			set
			{
				_domInstance.JobInfo.JobReference = value;
			}
		}

		/// <summary>
		/// Gets the ID of the service that is used for monitoring the events of this job.
		/// </summary>
		public DmsServiceId MonitoringService
		{
			get
			{
				string taskId = _domInstance.JobInfo.MonitoringService;
				if (String.IsNullOrEmpty(taskId) || !taskId.Contains("/"))
				{
					return default;
				}

				string[] splitTaskId = taskId.Split('/');

				return new DmsServiceId(Convert.ToInt32(splitTaskId[0]), Convert.ToInt32(splitTaskId[1]));
			}

			internal set
			{
				_domInstance.JobInfo.MonitoringService = value == default ? null : String.Join("/", value.AgentId, value.ServiceId);
			}
		}
	}

	public static class OrchestrationJobInfoExposers
	{
		public static readonly Exposer<OrchestrationJobInfo, string> JobReference = new Exposer<OrchestrationJobInfo, string>(x => x.JobReference, nameof(OrchestrationJobInfo.JobReference));
	}
}