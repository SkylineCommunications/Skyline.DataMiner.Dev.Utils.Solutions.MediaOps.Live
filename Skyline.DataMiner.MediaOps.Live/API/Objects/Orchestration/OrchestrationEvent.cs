namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Scheduling;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Information about an orchestration event.
	/// The event configuration is referenced but can not be viewed or edited from this object.
	/// To update the event configuration, convert to <see cref="OrchestrationEventConfiguration"/> object.
	/// </summary>
	public class OrchestrationEvent : ApiObject<OrchestrationEvent>
	{
		private readonly OrchestrationEventInstance _domInstance;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationEvent"/> class.
		/// </summary>
		public OrchestrationEvent() : this(new OrchestrationEventInstance())
		{
			_domInstance.OrchestrationEventInfo.EventState = SlcOrchestrationIds.Enums.EventState.Draft;
		}

		internal OrchestrationEvent(OrchestrationEventInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal OrchestrationEvent(DomInstance eventInstance) : this(domInstance: new OrchestrationEventInstance(eventInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcOrchestrationIds.Definitions.OrchestrationEvent;

		/// <summary>
		/// Gets or sets the name of the event.
		/// </summary>
		public string Name
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.EventName;
			}

			set
			{
				_domInstance.OrchestrationEventInfo.EventName = value;
			}
		}

		/// <summary>
		/// Gets or sets the type of the event.
		/// </summary>
		public EventType EventType
		{
			get
			{
				if (_domInstance.OrchestrationEventInfo.EventType.HasValue)
				{
					return (EventType)(int)_domInstance.OrchestrationEventInfo.EventType.Value;
				}

				return EventType.Other;
			}

			set
			{
				_domInstance.OrchestrationEventInfo.EventType = (SlcOrchestrationIds.Enums.EventType)(int)value;
			}
		}

		/// <summary>
		/// Gets the failure information, in case of a failed event.
		/// </summary>
		public string FailureInfo
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.FailureInfo;
			}

			internal set
			{
				_domInstance.OrchestrationEventInfo.FailureInfo = value;
			}
		}

		/// <summary>
		/// Gets the reference to the DataMiner reservation corresponding to this event.
		/// </summary>
		public ScheduledTaskId SchedulerReference
		{
			get
			{
				string taskId = _domInstance.OrchestrationEventInfo.SchedulerReference;
				if (String.IsNullOrEmpty(taskId) || !taskId.Contains("/"))
				{
					return null;
				}

				string[] splitTaskId = taskId.Split('/');

				return new ScheduledTaskId(Convert.ToInt32(splitTaskId[0]), Convert.ToInt32(splitTaskId[1]));
			}

			internal set
			{
				_domInstance.OrchestrationEventInfo.SchedulerReference = value == null ? null : String.Join("/", value.DmaId, value.TaskId);
			}
		}

		/// <summary>
		/// Gets or sets the state of the event.
		/// </summary>
		public EventState EventState
		{
			get
			{
				if (_domInstance.OrchestrationEventInfo.EventState.HasValue)
				{
					return (EventState)(int)_domInstance.OrchestrationEventInfo.EventState.Value;
				}

				return EventState.Draft;
			}

			set
			{
				PublicSetState(value);
			}
		}

		/// <summary>
		/// Gets or sets the planned time at which the event will execute.
		/// </summary>
		public DateTimeOffset? EventTime
		{
			get
			{
				if (_domInstance.OrchestrationEventInfo.EventTime == null)
				{
					return null;
				}

				DateTimeOffset time = DateTime.SpecifyKind(_domInstance.OrchestrationEventInfo.EventTime.Value, DateTimeKind.Utc);
				return time;
			}

			set
			{
				if (value == null)
				{
					_domInstance.OrchestrationEventInfo.EventTime = null;
					return;
				}

				var valueUtcTime = value.Value.UtcDateTime;

				_domInstance.OrchestrationEventInfo.EventTime = new DateTime(valueUtcTime.Ticks - valueUtcTime.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
			}
		}

		/// <summary>
		/// Gets the actual time at which the event has started.
		/// </summary>
		public DateTimeOffset? ActualStartTime
		{
			get
			{
				if (_domInstance.OrchestrationEventInfo.ActualStartTime == null)
				{
					return null;
				}

				DateTimeOffset time = DateTime.SpecifyKind(_domInstance.OrchestrationEventInfo.ActualStartTime.Value, DateTimeKind.Utc);
				return time;
			}

			internal set
			{
				_domInstance.OrchestrationEventInfo.ActualStartTime = value?.UtcDateTime;
			}
		}

		/// <summary>
		/// Gets the total duration of the orchestration.
		/// </summary>
		public TimeSpan? OrchestrationDuration
		{
			get
			{
				if (_domInstance.OrchestrationEventInfo.OrchestrationDuration == null)
				{
					return TimeSpan.FromTicks(0);
				}

				return _domInstance.OrchestrationEventInfo.OrchestrationDuration.Value;
			}

			internal set
			{
				_domInstance.OrchestrationEventInfo.OrchestrationDuration = value;
			}
		}

		internal ApiObjectReference<Configuration>? ConfigurationReference
		{
			get
			{
				return _domInstance.ConfigurationInfo.Configuration;
			}

			set
			{
				_domInstance.ConfigurationInfo.Configuration = value;
			}
		}

		internal ApiObjectReference<OrchestrationJobInfo>? JobInfoReference
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.JobInformation;
			}

			set
			{
				_domInstance.OrchestrationEventInfo.JobInformation = value;
			}
		}

		internal string GlobalOrchestrationScript
		{
			get
			{
				return _domInstance.GlobalConfiguration.OrchestrationScriptName;
			}

			set
			{
				_domInstance.GlobalConfiguration.OrchestrationScriptName = value;
			}
		}

		internal IList<OrchestrationScriptArgument> GlobalOrchestrationScriptArguments
		{
			get
			{
				return _domInstance.GlobalConfiguration.OrchestrationScriptArgumentsList;
			}

			set
			{
				_domInstance.GlobalConfiguration.OrchestrationScriptArgumentsList.Clear();
				_domInstance.GlobalConfiguration.OrchestrationScriptArgumentsList.AddRange(value);
			}
		}

		internal OrchestrationProfile Profile
		{
			get
			{
				return _domInstance.GlobalConfiguration.Profile;
			}

			set
			{
				_domInstance.GlobalConfiguration.Profile = value;
			}
		}

		public OrchestrationJobInfo GetJobInfo(MediaOpsLiveApi api)
		{
			return api.Orchestration.JobInfos.Read(JobInfoReference.Value);
		}

		internal OrchestrationEventConfiguration ToOrchestrationEventConfiguration(DomInstance configurationDomInstance)
		{
			return new OrchestrationEventConfiguration(_domInstance, new ConfigurationInstance(configurationDomInstance));
		}

		internal void InternalSetState(EventState state)
		{
			_domInstance.OrchestrationEventInfo.EventState = (SlcOrchestrationIds.Enums.EventState)(int)state;
		}

		internal void SendPlanJobStateUpdate(MediaOpsLiveApi api)
		{
			if (EventType == EventType.Other)
			{
				return;
			}

			api.GetMediaOpsPlanHelper().UpdateJobState(this);
		}

		private void PublicSetState(EventState state)
		{
			switch (state)
			{
				case EventState.Cancelled:
					_domInstance.OrchestrationEventInfo.EventState = SlcOrchestrationIds.Enums.EventState.Cancelled;
					return;
				case EventState.Draft:
					_domInstance.OrchestrationEventInfo.EventState = SlcOrchestrationIds.Enums.EventState.Draft;
					return;
				case EventState.Confirmed:
					_domInstance.OrchestrationEventInfo.EventState = SlcOrchestrationIds.Enums.EventState.Confirmed;
					return;

				default:
					throw new ArgumentException($"Event state {state} can not be applied.");
			}
		}
	}

	public static class OrchestrationEventExposers
	{
		public static readonly Exposer<OrchestrationEvent, Guid> ID = new Exposer<OrchestrationEvent, Guid>(x => x.ID, nameof(OrchestrationEvent.ID));
		public static readonly Exposer<OrchestrationEvent, string> Name = new Exposer<OrchestrationEvent, string>(x => x.Name, nameof(OrchestrationEvent.Name));
		public static readonly Exposer<OrchestrationEvent, DateTimeOffset> EventTime = new Exposer<OrchestrationEvent, DateTimeOffset>(x => x.EventTime.Value, nameof(OrchestrationEvent.EventTime));
		public static readonly Exposer<OrchestrationEvent, DateTimeOffset> ActualStartTime = new Exposer<OrchestrationEvent, DateTimeOffset>(x => x.ActualStartTime.Value, nameof(OrchestrationEvent.ActualStartTime));
		public static readonly Exposer<OrchestrationEvent, Guid> JobInfoReference = new Exposer<OrchestrationEvent, Guid>(x => x.JobInfoReference.Value.ID, nameof(OrchestrationEvent.JobInfoReference));
	}
}
