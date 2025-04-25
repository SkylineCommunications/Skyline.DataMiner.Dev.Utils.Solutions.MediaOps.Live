namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcOrchestration
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

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

		public OrchestrationJob GetOrchestrationJob(Guid jobReference)
		{
			IEnumerable<OrchestrationEvent> events = GetEventsByJobReference(jobReference);

			return new OrchestrationJob(jobReference) { OrchestrationEvents = events };
		}

		public OrchestrationJobConfiguration GetOrchestrationJobConfiguration(Guid jobReference)
		{
			IEnumerable<OrchestrationEventConfiguration> events = GetEventConfigurationsByJobReference(jobReference);

			return new OrchestrationJobConfiguration(jobReference) { OrchestrationEvents = events };
		}

		public OrchestrationJobConfiguration CreateOrUpdateOrchestrationJobConfiguration(OrchestrationJobConfiguration job)
		{
			DeleteEvents(job.RemovedIds);

			job.ValidateEventsBeforeSaving();
			var successes = CreateOrUpdateEventConfigurations(job.OrchestrationEvents);
			job.OrchestrationEvents = successes;
			return job;
		}

		public OrchestrationJob CreateOrUpdateOrchestrationJob(OrchestrationJob job)
		{
			DeleteEvents(job.RemovedIds);

			job.ValidateEventsBeforeSaving();
			var successes = CreateOrUpdateEvents(job.OrchestrationEvents);
			job.OrchestrationEvents = successes;
			return job;
		}

		internal OrchestrationJobConfiguration GetEventsAsEventConfigurations(OrchestrationJob job)
		{
			var convertedEvents = GetEventsAsEventConfigurations(job.OrchestrationEvents);
			return new OrchestrationJobConfiguration(job.JobId, convertedEvents.Values);
		}

		/// <summary>
		/// Get all <see cref="OrchestrationEvent"/> objects that contains the given job reference value.
		/// </summary>
		/// <param name="jobReference">Job reference value to filter.</param>
		/// <returns>A collection of <see cref="OrchestrationEvent"/> objects that contains the given job reference value.</returns>
		/// <exception cref="ArgumentException"><param name="jobReference"> can not be null or whitespace.</param></exception>
		internal IEnumerable<OrchestrationEvent> GetEventsByJobReference(Guid jobReference)
		{
			if (jobReference == Guid.Empty)
			{
				throw new ArgumentException($"'{nameof(jobReference)}' cannot be an empty Guid.", nameof(jobReference));
			}

			var filter = DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobReference).Equal(jobReference);

			return Read(filter);
		}

		/// <summary>
		/// Get all <see cref="OrchestrationEventConfiguration"/> objects that contains the given job reference value.
		/// </summary>
		/// <param name="jobReference">Job reference value to filter.</param>
		/// <returns>A collection of <see cref="OrchestrationEventConfiguration"/> objects that contains the given job reference value.</returns>
		/// <exception cref="ArgumentException"><param name="jobReference"> can not be null or whitespace.</param></exception>
		internal IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsByJobReference(Guid jobReference)
		{
			if (jobReference == Guid.Empty)
			{
				throw new ArgumentException($"'{nameof(jobReference)}' cannot be an empty Guid.", nameof(jobReference));
			}

			var events = GetEventsByJobReference(jobReference);
			return GetEventsAsEventConfigurations(events).Values;
		}

		/// <summary>
		/// Get the <see cref="OrchestrationEvent"/> object that matches the given event ID value.
		/// </summary>
		/// <param name="eventId">The ID of the instance to lookup.</param>
		/// <returns>A <see cref="OrchestrationEvent"/> object that matches the given event ID value, or null if no match is found.</returns>
		/// <exception cref="ArgumentException"><param name="eventId"> can not be an empty <see cref="Guid"/>>.</param></exception>
		internal OrchestrationEvent GetEventById(Guid eventId)
		{
			if (eventId == Guid.Empty)
			{
				throw new ArgumentException($"'{nameof(eventId)}' cannot be empty.", nameof(eventId));
			}

			var filter = DomInstanceExposers.Id.Equal(eventId);

			var result = Read(filter);

			IEnumerable<OrchestrationEvent> orchestrationEvents = result.ToList();

			return !orchestrationEvents.Any() ? null : orchestrationEvents.First();
		}

		/// <summary>
		/// Get the <see cref="OrchestrationEventConfiguration"/> object that matches the given event ID value.
		/// </summary>
		/// <param name="eventId">The ID of the instance to lookup.</param>
		/// <returns>A <see cref="OrchestrationEventConfiguration"/> object that matches the given event ID value, or null if no match is found.</returns>
		/// <exception cref="ArgumentException"><param name="eventId"> can not be an empty <see cref="Guid"/>>.</param></exception>
		internal OrchestrationEventConfiguration GetEventConfigurationById(Guid eventId)
		{
			if (eventId == Guid.Empty)
			{
				throw new ArgumentException($"'{nameof(eventId)}' cannot be empty.", nameof(eventId));
			}

			var orchestrationEvent = GetEventById(eventId);

			if (orchestrationEvent == null)
			{
				return null;
			}

			return GetEventsAsEventConfigurations(orchestrationEvent);
		}

		/// <summary>
		/// Convert a <see cref="OrchestrationEvent"/> object to a <see cref="OrchestrationEventConfiguration"/> object by retrieving configuration data from DataMiner.
		/// </summary>
		/// <param name="event">The <see cref="OrchestrationEvent"/> object to convert.</param>
		/// <returns>The <see cref="OrchestrationEventConfiguration"/> object that corresponds to the given input, or null if the operation failed.</returns>
		/// <exception cref="ArgumentNullException"><param name="event">can not be null</param></exception>
		internal OrchestrationEventConfiguration GetEventsAsEventConfigurations(OrchestrationEvent @event)
		{
			if (@event == null)
			{
				throw new ArgumentNullException(nameof(@event));
			}

			return GetEventsAsEventConfigurations(new List<OrchestrationEvent> { @event }).Values.FirstOrDefault();
		}

		/// <summary>
		/// Convert a collection of <see cref="OrchestrationEvent"/> objects to <see cref="OrchestrationEventConfiguration"/> objects by retrieving configuration data from DataMiner.
		/// </summary>
		/// <param name="events">The <see cref="OrchestrationEvent"/> objects to convert.</param>
		/// <returns>A mapping of each event ID to the converted <see cref="OrchestrationEventConfiguration"/> object.</returns>
		/// <exception cref="ArgumentNullException"><param name="events">can not be null</param></exception>
		internal Dictionary<Guid, OrchestrationEventConfiguration> GetEventsAsEventConfigurations(IEnumerable<OrchestrationEvent> events)
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

		/// <summary>
		/// Saves a list of new or updated <see cref="OrchestrationEvent"/> objects to the DataMiner System.
		/// </summary>
		/// <param name="events">A list of configured or updated event configurations.</param>
		/// <returns>Returns a list of all successfully saved event configurations.</returns>
		internal IEnumerable<OrchestrationEvent> CreateOrUpdateEvents(IEnumerable<OrchestrationEvent> events)
		{
			var results = CreateOrUpdateWithResult(events);

			return results.SuccessfulItems.Select(item => new OrchestrationEvent(item));
		}

		/// <summary>
		/// Saves a list of new or updated <see cref="OrchestrationEventConfiguration"/> objects to the DataMiner System.
		/// </summary>
		/// <param name="events">A list of configured or updated event configurations.</param>
		/// <returns>Returns a list of all successfully saved <see cref="OrchestrationEventConfiguration"/> objects.</returns>
		internal IEnumerable<OrchestrationEventConfiguration> CreateOrUpdateEventConfigurations(IEnumerable<OrchestrationEventConfiguration> events)
		{
			List<Configuration> configsToDelete = new List<Configuration>();
			List<Configuration> configsToWrite = new List<Configuration>();

			IEnumerable<OrchestrationEventConfiguration> orchestrationEventConfigurations = events.ToList();
			foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in orchestrationEventConfigurations)
			{
				if (orchestrationEventConfiguration.Configuration.IsEmpty())
				{
					orchestrationEventConfiguration.ConfigurationReference = null;
					configsToDelete.Add(orchestrationEventConfiguration.Configuration);
				}

				orchestrationEventConfiguration.ConfigurationReference = orchestrationEventConfiguration.Configuration.ID;
				configsToWrite.Add(orchestrationEventConfiguration.Configuration);
			}

			_configurationHelper.Delete(configsToDelete);
			_configurationHelper.CreateOrUpdate(configsToWrite);

			var results = CreateOrUpdateWithResult(orchestrationEventConfigurations);

			return orchestrationEventConfigurations.Where(config => results.SuccessfulIds.Contains(config.DomInstance.ID));
		}

		/// <summary>
		/// Saves a new or updated event to the DataMiner System.
		/// </summary>
		/// <param name="event">The configured or updated event.</param>
		/// <returns>Returns the <see cref="OrchestrationEvent"/> object as saved on DataMiner, or null if the event was not correctly saved.</returns>
		internal OrchestrationEvent CreateOrUpdateEvent(OrchestrationEvent @event)
		{
			var result = CreateOrUpdateEvents(new List<OrchestrationEvent> { @event });

			return result.FirstOrDefault();
		}

		/// <summary>
		/// Delete an event with the given event ID from the DataMiner system.
		/// </summary>
		/// <param name="eventId">ID of the event that will be deleted.</param>
		internal void DeleteEvent(Guid eventId)
		{
			OrchestrationEvent orchestrationEvent = GetEventById(eventId);

			DeleteEvent(orchestrationEvent);
		}

		/// <summary>
		/// Delete a <see cref="OrchestrationEvent"/> object from the DataMiner system.
		/// </summary>
		/// <param name="event">The event to be deleted.</param>
		internal void DeleteEvent(OrchestrationEvent @event)
		{
			if (@event.ConfigurationReference.HasValue)
			{
				_configurationHelper.Delete(GetConfigurationInstances(new List<Guid> { @event.ConfigurationReference.Value.ID }).Values);
			}

			Delete(@event);
		}


		/// <summary>
		/// Delete a collection of <see cref="OrchestrationEvent"/> objects from the DataMiner system.
		/// </summary>
		/// <param name="eventIds">The events to be deleted.</param>
		internal void DeleteEvents(IEnumerable<Guid> eventIds)
		{
			var orchestrationEvents = Read(eventIds).Values;

			DeleteEvents(orchestrationEvents);
		}


		/// <summary>
		/// Delete a collection of <see cref="OrchestrationEvent"/> objects from the DataMiner system.
		/// </summary>
		/// <param name="events">The events to be deleted.</param>
		internal void DeleteEvents(IEnumerable<OrchestrationEvent> events)
		{
			IEnumerable<OrchestrationEvent> orchestrationEvents = events.ToList();
			var configurationsToDelete = orchestrationEvents.Where(e => e.ConfigurationReference.HasValue).Select(e => e.ConfigurationReference.Value.ID);
			_configurationHelper.Delete(GetConfigurationInstances(configurationsToDelete).Values);

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
