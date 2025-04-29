namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
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
		public OrchestrationEvent() : this(new OrchestrationEventInstance())
		{
			_domInstance.OrchestrationEventInfo.EventState = SlcOrchestrationIds.Enums.EventState.Draft;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationEvent"/> class, inheriting the data from the given <see cref="OrchestrationEventInstance"/> object.
		/// </summary>
		internal OrchestrationEvent(OrchestrationEventInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));

			if (_domInstance.OrchestrationEventInfo.ReservationInstance == null)
			{
				_domInstance.OrchestrationEventInfo.ReservationInstance = Guid.Empty;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationEvent"/> class, inheriting the data from the given <see cref="DomInstance"/> object.
		/// </summary>
		internal OrchestrationEvent(DomInstance domInstance) : this(new OrchestrationEventInstance(domInstance))
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
		public Guid? ReservationInstance
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.ReservationInstance;
			}

			internal set
			{
				_domInstance.OrchestrationEventInfo.ReservationInstance = value;
			}
		}

		/// <summary>
		/// Gets the string reference to the job that corresponds to this event.
		/// </summary>
		public Guid JobReference
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.JobReference == null ? Guid.Empty : new Guid(_domInstance.OrchestrationEventInfo.JobReference);
			}

			internal set
			{
				_domInstance.OrchestrationEventInfo.JobReference = value.ToString();
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
				ApplyEventState(value);
			}
		}

		/// <summary>
		/// Gets or sets the time at which the event will execute.
		/// </summary>
		public DateTime? EventTime
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.EventTime;
			}

			set
			{
				_domInstance.OrchestrationEventInfo.EventTime = value;
			}
		}

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

		internal OrchestrationEventConfiguration ToOrchestrationEventConfiguration(Configuration configuration)
		{
			return new OrchestrationEventConfiguration(_domInstance, new ConfigurationInstance(configuration.DomInstance));
		}

		/// <summary>
		/// Apply the <see cref="SlcOrchestrationIds.Enums.EventState.Cancelled"/> state to the booking. This is only allowed if the current state is Confirmed.
		/// </summary>
		/// <returns>True if the new state could be applied, false if the state was blocked.</returns>
		public bool TryCancel()
		{
			switch (EventState)
			{
				case null:
				case SlcOrchestrationIds.Enums.EventState.Confirmed:
					_domInstance.OrchestrationEventInfo.EventState = SlcOrchestrationIds.Enums.EventState.Cancelled;
					return true;

				case SlcOrchestrationIds.Enums.EventState.Cancelled:
					return true;

				default:
					// Transition not allowed;
					return false;
			}
		}

		/// <summary>
		/// Apply the <see cref="SlcOrchestrationIds.Enums.EventState.Confirmed"/> state to the booking. This is only allowed if the current state is Cancelled or Draft.
		/// </summary>
		/// <returns>True if the new state could be applied, false if the state was blocked.</returns>
		public bool TryConfirm()
		{
			switch (EventState)
			{
				case null:
				case SlcOrchestrationIds.Enums.EventState.Draft:
				case SlcOrchestrationIds.Enums.EventState.Cancelled:
					_domInstance.OrchestrationEventInfo.EventState = SlcOrchestrationIds.Enums.EventState.Confirmed;
					return true;

				case SlcOrchestrationIds.Enums.EventState.Confirmed:
					return true;

				default:
					// Transition not allowed;
					return false;
			}
		}

		/// <summary>
		/// Apply the <see cref="SlcOrchestrationIds.Enums.EventState.Draft"/> state to the booking. This is only allowed if the current state is Confirmed or Cancelled.
		/// </summary>
		/// <returns>True if the new state could be applied, false if the state was blocked.</returns>
		public bool TrySetToDraft()
		{
			switch (EventState)
			{
				case null:
				case SlcOrchestrationIds.Enums.EventState.Confirmed:
				case SlcOrchestrationIds.Enums.EventState.Cancelled:
					_domInstance.OrchestrationEventInfo.EventState = SlcOrchestrationIds.Enums.EventState.Draft;
					return true;

				case SlcOrchestrationIds.Enums.EventState.Draft:
					return true;

				default:
					// Transition not allowed;
					return false;
			}
		}

		private void ApplyEventState(SlcOrchestrationIds.Enums.EventState? state)
		{
			bool result;
			switch (state)
			{
				case SlcOrchestrationIds.Enums.EventState.Cancelled:
					result = TryCancel();
					break;

				case SlcOrchestrationIds.Enums.EventState.Confirmed:
					result = TryConfirm();
					break;

				case SlcOrchestrationIds.Enums.EventState.Draft:
					result = TrySetToDraft();
					break;

				default:
					// Other states not supported for now.
					return;
			}

			if (!result)
			{
				throw new ArgumentException($"Event state {state.ToString()} can not be applied.");
			}
		}
	}
}
