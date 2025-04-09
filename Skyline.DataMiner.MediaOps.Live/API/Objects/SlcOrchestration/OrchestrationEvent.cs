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

		private readonly WrappedList<NodeConfigurationSection, NodeConfiguration> _wrappedNodeConfigurations;

		private bool _existsOnDom;

		public OrchestrationEvent() : this(new OrchestrationEventInstance())
		{
			_existsOnDom = false;
		}

		internal OrchestrationEvent(OrchestrationEventInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
			_existsOnDom = true;

			_wrappedNodeConfigurations = new WrappedList<NodeConfigurationSection, NodeConfiguration>(
				_domInstance.NodeConfiguration,
				x => new NodeConfiguration(x),
				x => x.DomSection);
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

			protected set
			{
				_domInstance.OrchestrationEventInfo.EventState = value;
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

		public IList<NodeConfiguration> NodeConfigurations
		{
			get
			{
				return _wrappedNodeConfigurations;
			}

			set
			{
				_wrappedNodeConfigurations.Clear();
				_wrappedNodeConfigurations.AddRange(value);
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

		public Guid DomInstanceId
		{
			get
			{
				return _domInstance.ID.Id;
			}
		}

		protected bool ExistsOnDom
		{
			get
			{
				return _existsOnDom;
			}

			set
			{
				_existsOnDom = value;
			}
		}

		public bool TryCancel()
		{
			if (!ExistsOnDom)
			{
				EventState = SlcOrchestrationIds.Enums.EventState.Cancelled;
				return true;
			}

			switch (EventState)
			{
				case SlcOrchestrationIds.Enums.EventState.Confirmed:
					EventState = SlcOrchestrationIds.Enums.EventState.Cancelled;
					return true;

				default:
					// Transition not allowed;
					return false;
			}
		}

		public bool TryConfirm()
		{
			if (!ExistsOnDom)
			{
				EventState = SlcOrchestrationIds.Enums.EventState.Confirmed;
				return true;
			}

			switch (EventState)
			{
				case SlcOrchestrationIds.Enums.EventState.Draft:
				case SlcOrchestrationIds.Enums.EventState.Cancelled:
					EventState = SlcOrchestrationIds.Enums.EventState.Confirmed;
					return true;

				default:
					// Transition not allowed;
					return false;
			}
		}

		public bool TrySetToDraft()
		{
			if (!ExistsOnDom)
			{
				EventState = SlcOrchestrationIds.Enums.EventState.Draft;
				return true;
			}

			switch (EventState)
			{
				case SlcOrchestrationIds.Enums.EventState.Confirmed:
				case SlcOrchestrationIds.Enums.EventState.Cancelled:
					EventState = SlcOrchestrationIds.Enums.EventState.Draft;
					return true;

				default:
					// Transition not allowed;
					return false;
			}
		}
	}
}
