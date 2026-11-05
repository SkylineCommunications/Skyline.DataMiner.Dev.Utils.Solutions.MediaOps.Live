namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling;

	/// <summary>
	/// The phase statuses of one node for one event.
	/// </summary>
	internal class OrchestrationNodePhases : ApiObject<OrchestrationNodePhases>
	{
		private readonly OrchestrationNodePhasesInstance _domInstance;
		private readonly WrappedList<PhaseStatusSection, OrchestrationPhaseStatus> _wrappedPhases;

		public OrchestrationNodePhases() : this(new OrchestrationNodePhasesInstance())
		{
		}

		internal OrchestrationNodePhases(OrchestrationNodePhasesInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));

			_wrappedPhases = new WrappedList<PhaseStatusSection, OrchestrationPhaseStatus>(
				_domInstance.PhaseStatus,
				x => new OrchestrationPhaseStatus(x),
				x => x.DomSection);
		}

		internal OrchestrationNodePhases(DomInstance domInstance) : this(new OrchestrationNodePhasesInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcOrchestrationIds.Definitions.OrchestrationNodePhases;

		public ApiObjectReference<OrchestrationJobInfo> JobInfoReference
		{
			get
			{
				return new ApiObjectReference<OrchestrationJobInfo>(_domInstance.NodePhasesInfo.JobInformation ?? Guid.Empty);
			}

			set
			{
				_domInstance.NodePhasesInfo.JobInformation = value == ApiObjectReference<OrchestrationJobInfo>.Empty ? null : value.ID;
			}
		}

		public ApiObjectReference<OrchestrationEvent> EventReference
		{
			get
			{
				return new ApiObjectReference<OrchestrationEvent>(_domInstance.NodePhasesInfo.OrchestrationEvent ?? Guid.Empty);
			}

			set
			{
				_domInstance.NodePhasesInfo.OrchestrationEvent = value == ApiObjectReference<OrchestrationEvent>.Empty ? null : value.ID;
			}
		}

		public string NodeId
		{
			get
			{
				return _domInstance.NodePhasesInfo.NodeID;
			}

			set
			{
				_domInstance.NodePhasesInfo.NodeID = value;
			}
		}

		public string PhaseLayoutFingerprint
		{
			get
			{
				return _domInstance.NodePhasesInfo.PhaseLayoutFingerprint;
			}

			set
			{
				_domInstance.NodePhasesInfo.PhaseLayoutFingerprint = value;
			}
		}

		public ScheduledTaskId ResumeSchedulerReference
		{
			get
			{
				string taskId = _domInstance.NodePhasesInfo.ResumeSchedulerReference;
				if (String.IsNullOrEmpty(taskId) || !taskId.Contains("/"))
				{
					return null;
				}

				string[] splitTaskId = taskId.Split('/');

				return new ScheduledTaskId(Convert.ToInt32(splitTaskId[0]), Convert.ToInt32(splitTaskId[1]));
			}

			set
			{
				_domInstance.NodePhasesInfo.ResumeSchedulerReference = value?.ToString();
			}
		}

		public DateTimeOffset? WaitStarted
		{
			get
			{
				DateTime? value = _domInstance.NodePhasesInfo.WaitStarted;
				return value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
			}

			set
			{
				_domInstance.NodePhasesInfo.WaitStarted = value?.UtcDateTime;
			}
		}

		/// <summary>
		/// Gets or sets the position of the wait phase a scheduled resume continues at.
		/// </summary>
		public int? ResumePhaseIndex
		{
			get
			{
				return (int?)_domInstance.NodePhasesInfo.ResumePhaseIndex;
			}

			set
			{
				_domInstance.NodePhasesInfo.ResumePhaseIndex = value;
			}
		}

		/// <summary>
		/// Gets or sets the moment the scheduled resume is due.
		/// </summary>
		public DateTimeOffset? ResumeTime
		{
			get
			{
				DateTime? value = _domInstance.NodePhasesInfo.ResumeTime;
				return value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
			}

			set
			{
				_domInstance.NodePhasesInfo.ResumeTime = value?.UtcDateTime;
			}
		}

		public IList<OrchestrationPhaseStatus> Phases
		{
			get
			{
				return _wrappedPhases;
			}

			set
			{
				_wrappedPhases.Clear();
				_wrappedPhases.AddRange(value);
			}
		}
	}
}
