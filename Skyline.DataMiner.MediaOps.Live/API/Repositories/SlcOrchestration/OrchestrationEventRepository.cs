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
		private readonly ConfigurationRepository _configurationHelper;

		public OrchestrationEventRepository(SlcOrchestrationHelper helper) : base(helper)
		{
			_configurationHelper = new ConfigurationRepository(helper);
		}

		protected internal override DomDefinitionId DomDefinition => OrchestrationEvent.DomDefinition;

		public IEnumerable<OrchestrationEvent> GetEventsByJobReference(string jobReference)
		{
			if (String.IsNullOrWhiteSpace(jobReference))
			{
				throw new ArgumentException($"'{nameof(jobReference)}' cannot be null or whitespace.", nameof(jobReference));
			}

			var filter = DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobReference).Equal(jobReference);

			return Read(filter);
		}

		public IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsByJobReference(string jobReference)
		{
			var events = GetEventsByJobReference(jobReference);
			return GetEventsAsEventConfigurations(events).Values;
		}

		public OrchestrationEvent GetEventById(Guid domInstanceId)
		{
			var filter = DomInstanceExposers.Id.Equal(domInstanceId);

			var result = Read(filter);

			IEnumerable<OrchestrationEvent> orchestrationEvents = result.ToList();

			return !orchestrationEvents.Any() ? null : orchestrationEvents.First();
		}

		public OrchestrationEventConfiguration GetEventConfigurationbyId(Guid domInstanceId)
		{
			var orchestrationEvent = GetEventById(domInstanceId);

			if (orchestrationEvent == null)
			{
				return null;
			}

			return GetEventsAsEventConfigurations(orchestrationEvent);
		}

		public OrchestrationEventConfiguration GetEventsAsEventConfigurations(OrchestrationEvent orchestrationEvent)
		{
			if (orchestrationEvent == null)
			{
				throw new ArgumentNullException(nameof(orchestrationEvent));
			}

			return GetEventsAsEventConfigurations(new List<OrchestrationEvent> { orchestrationEvent }).Values.FirstOrDefault();
		}

		public Dictionary<Guid, OrchestrationEventConfiguration> GetEventsAsEventConfigurations(IEnumerable<OrchestrationEvent> events)
		{
			if (events == null)
			{
				throw new ArgumentNullException(nameof(events));
			}

			IEnumerable<OrchestrationEvent> orchestrationEvents = events.ToList();
			List<Guid> instancesToRetrieve = orchestrationEvents.Where(e => e.ConfigurationReference.HasValue).Select(e => e.ConfigurationReference.Value.ID).ToList();

			IDictionary<Guid, Configuration> configurationMapping = GetConfigurationInstances(instancesToRetrieve);

			return orchestrationEvents
				.ToDictionary(
					x => x.ID,
					x => x.ToOrchestrationEventConfiguration(configurationMapping.TryGetValue(x.ConfigurationReference.GetValueOrDefault(), out Configuration configuration)
						? configuration.DomInstance
						: new DomInstance()));
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
			Delete(GetEventById(domInstanceId));
		}

		public void DeleteOrchestrationEvent(OrchestrationEvent orchestrationEvent)
		{
			Delete(orchestrationEvent);
		}

		public void DeleteOrchestrationEvents(IEnumerable<OrchestrationEvent> orchestrationEvents)
		{
			Delete(orchestrationEvents);
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

		private IDictionary<Guid, Configuration> GetConfigurationInstances(IEnumerable<Guid> instanceGuids)
		{
			return _configurationHelper.Read(instanceGuids);
		}
	}
}
