namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

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
		public OrchestrationEvent() : this(domInstance: new OrchestrationEventInstance())
		{
			_domInstance.OrchestrationEventInfo.EventState = SlcOrchestrationIds.Enums.EventState.Draft;
		}

		internal OrchestrationEvent(OrchestrationEventInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal OrchestrationEvent(DomInstance domInstance) : this(domInstance: new OrchestrationEventInstance(domInstance))
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
		public SlcOrchestrationIds.Enums.EventType EventType
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.EventType ?? SlcOrchestrationIds.Enums.EventType.Other;
			}

			set
			{
				_domInstance.OrchestrationEventInfo.EventType = value;
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
		public ScheduledTaskId ReservationInstance
		{
			get
			{
				string taskId = _domInstance.OrchestrationEventInfo.ReservationInstance;
				if (String.IsNullOrEmpty(taskId) || !taskId.Contains("/"))
				{
					return null;
				}

				string[] splitTaskId = taskId.Split('/');

				return new ScheduledTaskId(Convert.ToInt32(splitTaskId[0]), Convert.ToInt32(splitTaskId[1]));
			}

			internal set
			{
				_domInstance.OrchestrationEventInfo.ReservationInstance = value == null ? null : String.Join("/", value.DmaId, value.TaskId);
			}
		}

		/// <summary>
		/// Gets the string reference to the job that corresponds to this event.
		/// </summary>
		public string JobReference
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.JobReference;
			}

			internal set
			{
				_domInstance.OrchestrationEventInfo.JobReference = value;
			}
		}

		/// <summary>
		/// Gets or sets the state of the event.
		/// </summary>
		public SlcOrchestrationIds.Enums.EventState? EventState
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.EventState;
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
				_domInstance.OrchestrationEventInfo.EventTime = value?.UtcDateTime;
			}
		}

		/// <summary>
		/// Gets or sets the actual time at which the event has started.
		/// </summary>
		public DateTimeOffset? ActualStartTime { get; set; }

		/// <summary>
		/// Gets or sets the total duration of the orchestration.
		/// </summary>
		public DateTimeOffset? OrchestrationDuration { get; set; }

		internal ApiObjectReference<Configuration>? ConfigurationReference
		{
			get
			{
				return _domInstance.Configuration.ConfigurationInfo;
			}

			set
			{
				_domInstance.Configuration.ConfigurationInfo = value;
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

		internal OrchestrationEventConfiguration ToOrchestrationEventConfiguration(DomInstance configurationDomInstance)
		{
			return new OrchestrationEventConfiguration(_domInstance, new ConfigurationInstance(configurationDomInstance));
		}

		internal void InternalSetState(SlcOrchestrationIds.Enums.EventState? state)
		{
			_domInstance.OrchestrationEventInfo.EventState = state;
		}

		private void PublicSetState(SlcOrchestrationIds.Enums.EventState? state)
		{
			switch (state)
			{
				case SlcOrchestrationIds.Enums.EventState.Cancelled:
				case SlcOrchestrationIds.Enums.EventState.Draft:
				case SlcOrchestrationIds.Enums.EventState.Confirmed:
					_domInstance.OrchestrationEventInfo.EventState = state;
					return;

				default:
					throw new ArgumentException($"Event state {state} can not be applied.");
			}
		}
	}
}
