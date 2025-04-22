namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	public class OrchestrationEvent : ApiObject<OrchestrationEvent>
	{
		private readonly OrchestrationEventInstance _domInstance;

		private bool _existsOnDom;

		public OrchestrationEvent() : this(new OrchestrationEventInstance())
		{
			_domInstance.OrchestrationEventInfo.EventState = SlcOrchestrationIds.Enums.EventState.Draft;
		}

		internal OrchestrationEvent(OrchestrationEventInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal OrchestrationEvent(DomInstance domInstance) : this(new OrchestrationEventInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcOrchestrationIds.Definitions.OrchestrationEvent;

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

		public SlcOrchestrationIds.Enums.EventType? EventType
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.EventType;
			}

			set
			{
				_domInstance.OrchestrationEventInfo.EventType = value;
			}
		}

		public string FailureInfo
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.FailureInfo;
			}

			set
			{
				_domInstance.OrchestrationEventInfo.FailureInfo = value;
			}
		}

		public Guid? ReservationInstance
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.ReservationInstance;
			}

			set
			{
				_domInstance.OrchestrationEventInfo.ReservationInstance = value;
			}
		}

		public string JobReference
		{
			get
			{
				return _domInstance.OrchestrationEventInfo.JobReference;
			}

			set
			{
				_domInstance.OrchestrationEventInfo.JobReference = value;
			}
		}

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

		public ApiObjectReference<Configuration>? Configuration
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

		public string GlobalOrchestrationScript
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

		public IList<OrchestrationScriptArgument> GlobalOrchestrationScriptArguments
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

		public Guid Id
		{
			get
			{
				return _domInstance.ID.Id;
			}
		}

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
					_domInstance.OrchestrationEventInfo.EventState = state;
					return;
			}

			if (!result)
			{
				throw new ArgumentException($"Event state {state.ToString()} can not be applied.");
			}
		}

		internal void Save(DomHelper helper)
		{
			_domInstance.Save(helper);
		}
	}
}
