namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Remoting.Messaging;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	using Comparer = Net.Messages.SLDataGateway.Comparer;

	public class OrchestrationEventRepository : Repository<OrchestrationEvent>
	{
		public OrchestrationEventRepository(SlcOrchestrationHelper helper) : base(helper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => OrchestrationEvent.DomDefinition;

		public IEnumerable<OrchestrationEvent> GetByJobReference(string jobReference)
		{
			if (string.IsNullOrWhiteSpace(jobReference))
			{
				throw new ArgumentException($"'{nameof(jobReference)}' cannot be null or whitespace.", nameof(jobReference));
			}

			var filter = DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobReference).Equal(jobReference);

			return Read(filter);
		}

		public IEnumerable<OrchestrationEvent> CreateOrUpdateOrchestrationEvents(IEnumerable<OrchestrationEvent> events)
		{
			var results = CreateOrUpdateWithResult(events);

			return results.SuccessfulItems.Select(item => new OrchestrationEvent(item));
		}

		public OrchestrationEvent CreateOrUpdateOrchestrationEvent(OrchestrationEvent orchestrationEvent)
		{
			orchestrationEvent.Save(Helper);
			return orchestrationEvent;
		}

		public void DeleteOrchestrationEvent(Guid domInstanceId)
		{
			Delete(GetByDomInstanceId(domInstanceId));
		}

		public void DeleteOrchestrationEvent(OrchestrationEvent orchestrationEvent)
		{
			Delete(orchestrationEvent);
		}

		public void DeleteOrchestrationEvents(IEnumerable<OrchestrationEvent> orchestrationEvents)
		{
			Delete(orchestrationEvents);
		}

		public OrchestrationEvent GetByDomInstanceId(Guid domInstanceId)
		{
			var filter = DomInstanceExposers.Id.Equal(domInstanceId);

			var result = Read(filter);

			if (result == null || !result.Any())
			{
				return null;
			}

			return result.First();
		}

		public bool TrySetToDraftEventByDomInstanceId(Guid domInstanceId)
		{

			return TryUpdateState(domInstanceId, SlcOrchestrationIds.Enums.EventState.Draft);
		}

		public bool TryConfirmEventByDomInstanceId(Guid domInstanceId)
		{

			return TryUpdateState(domInstanceId, SlcOrchestrationIds.Enums.EventState.Confirmed);
		}

		public bool TryCancelEventByDomInstanceId(Guid domInstanceId)
		{
			return TryUpdateState(domInstanceId, SlcOrchestrationIds.Enums.EventState.Cancelled);
		}

		internal bool TryUpdateState(Guid domInstanceId, SlcOrchestrationIds.Enums.EventState state)
		{
			OrchestrationEvent orchestrationEvent = GetByDomInstanceId(domInstanceId);

			if (orchestrationEvent == null)
			{
				throw new NotSupportedException(
					$"Event with DOM instance id {nameof(domInstanceId)} could not be found");
			}

			switch (state)
			{
				case SlcOrchestrationIds.Enums.EventState.Cancelled:
					return orchestrationEvent.TryCancel();

				case SlcOrchestrationIds.Enums.EventState.Confirmed:
					return orchestrationEvent.TryConfirm();

				case SlcOrchestrationIds.Enums.EventState.Draft:
					return orchestrationEvent.TrySetToDraft();

				default:
					return false;
			}
		}

		protected override OrchestrationEvent CreateInstance(DomInstance domInstance)
		{
			return new OrchestrationEvent(domInstance);
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(OrchestrationEvent.Name):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventName), comparer, (string)value);
				case nameof(OrchestrationEvent.EventType):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventType), comparer, (int)value);
				case nameof(OrchestrationEvent.EventState):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventState), comparer, (int)value);
				case nameof(OrchestrationEvent.EventTime):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime), comparer, (double)value);
				case nameof(OrchestrationEvent.ReservationInstance):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.ReservationInstance), comparer, (string)value);
				case nameof(OrchestrationEvent.FailureInfo):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.FailureInfo), comparer, (string)value);
				case nameof(OrchestrationEvent.GlobalOrchestrationScript):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.GlobalConfiguration.OrchestrationScriptName), comparer, (string)value);
				case nameof(OrchestrationEvent.JobReference):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobReference), comparer, (string)value);
			}

			return base.CreateFilter(fieldName, comparer, value);
		}

		protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(OrchestrationEvent.Name):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventName), sortOrder, naturalSort);
				case nameof(OrchestrationEvent.EventType):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventType), sortOrder, naturalSort);
				case nameof(OrchestrationEvent.EventState):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventState), sortOrder, naturalSort);
				case nameof(OrchestrationEvent.EventTime):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime), sortOrder, naturalSort);
				case nameof(OrchestrationEvent.ReservationInstance):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime), sortOrder, naturalSort);
				case nameof(OrchestrationEvent.FailureInfo):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.FailureInfo), sortOrder, naturalSort);
				case nameof(OrchestrationEvent.GlobalOrchestrationScript):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.GlobalConfiguration.OrchestrationScriptName), sortOrder, naturalSort);
				case nameof(OrchestrationEvent.JobReference):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobReference), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}
	}
}
