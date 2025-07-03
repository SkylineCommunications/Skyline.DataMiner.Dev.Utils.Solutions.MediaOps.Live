namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.Orchestration
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;

	using SLDataGateway.API.Types.Querying;

	using Comparer = Skyline.DataMiner.Net.Messages.SLDataGateway.Comparer;

	/// <summary>
	/// Exposes API methods to interact with and orchestrate MediaOps Live Orchestration events.
	/// </summary>
	public class OrchestrationEventRepository : Repository<OrchestrationEvent>
	{
		private readonly MediaOpsLiveApi _api;
		private readonly ConfigurationRepository _configurationHelper;
		private readonly OrchestrationSlidingWindowScheduler _slidingWindowScheduler;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationEventRepository"/> class.
		/// </summary>
		/// <param name="helper">Orchestration helper.</param>
		/// <param name="api">Api that calls the repository.</param>
		internal OrchestrationEventRepository(SlcOrchestrationHelper helper, MediaOpsLiveApi api) : base(helper, api.Connection)
		{
			_configurationHelper = new ConfigurationRepository(helper, api.Connection);
			_api = api;
			_slidingWindowScheduler = new OrchestrationSlidingWindowScheduler(
				this,
				TimeSpan.FromHours(Constants.SchedulerSlidingWindowRangeHours_Past),
				TimeSpan.FromHours(Constants.SchedulerSlidingWindowRangeHours_Future));
		}

		/// <summary>
		/// Gets the DOM definition GUID.
		/// </summary>
		protected internal override DomDefinitionId DomDefinition => OrchestrationEvent.DomDefinition;

		/// <summary>
		/// Executes a sync for the current timing window (default 1 hour in the past and 12 hours into the future).
		/// This consists of deleting past orchestration tasks and prepare tasks for upcoming events.
		/// </summary>
		public void SyncCurrentSlidingWindow()
		{
			_slidingWindowScheduler.SyncSchedulerWithWindow();
		}

		/// <summary>
		/// Creates a recurring scheduled task to prepare orchestration tasks in a sliding window manner.
		/// If the task already exists, no new task is created.
		/// </summary>
		public void SetupSlidingWindowSchedulerTasks()
		{
			_slidingWindowScheduler.SetupSchedulerTasks();
		}

		/// <summary>
		///     Creates a <see cref="OrchestrationJob" /> object with all events for the given job reference.
		/// </summary>
		/// <param name="jobReference">The ID of the job to retrieve.</param>
		/// <returns>A <see cref="OrchestrationJob" /> object with all events found for the given job reference.</returns>
		public OrchestrationJob GetOrCreateNewOrchestrationJob(string jobReference)
		{
			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			PerformanceFileLogger performanceFileLogger = new PerformanceFileLogger("ORC-GetOrCreateJob", performanceLogFilename);

			using (PerformanceCollector collector = new PerformanceCollector(performanceFileLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				IEnumerable<OrchestrationEvent> events = GetEventsByJobReference(jobReference, performanceTracker);

				return new OrchestrationJob(jobReference, events);
			}
		}

		/// <summary>
		///     Creates a <see cref="OrchestrationJobConfiguration" /> object with all event configurations for the given job
		///     reference.
		/// </summary>
		/// <param name="jobReference">The ID of the job to retrieve.</param>
		/// <returns>
		///     A <see cref="OrchestrationJobConfiguration" /> object with all event configurations found for the given job
		///     reference.
		/// </returns>
		public OrchestrationJobConfiguration GetOrCreateNewOrchestrationJobConfiguration(string jobReference)
		{
			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			PerformanceFileLogger performanceFileLogger = new PerformanceFileLogger("ORC-GetOrCreateJobConfiguration", performanceLogFilename);

			using (PerformanceCollector collector = new PerformanceCollector(performanceFileLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				IEnumerable<OrchestrationEventConfiguration> events = GetEventConfigurationsByJobReference(jobReference, performanceTracker);

				return new OrchestrationJobConfiguration(jobReference, events);
			}
		}

		/// <summary>
		///     Saves the job configuration to the DataMiner system.
		/// </summary>
		/// <param name="job">The <see cref="OrchestrationJobConfiguration" /> object to save.</param>
		public void SaveOrchestrationJobConfiguration(OrchestrationJobConfiguration job)
		{
			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			PerformanceFileLogger performanceFileLogger = new PerformanceFileLogger("ORC-SaveJobConfiguration", performanceLogFilename);

			using (PerformanceCollector collector = new PerformanceCollector(performanceFileLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				DeleteEvents(job.RemovedIds, performanceTracker);

				job.ValidateEventsBeforeSaving(_api.Connection);

				_slidingWindowScheduler.ScheduleEvents(job.OrchestrationEvents);

				SaveEventConfigurations(job.OrchestrationEvents, performanceTracker);
			}
		}

		/// <summary>
		///     Saves the job to the DataMiner system.
		/// </summary>
		/// <param name="job">The <see cref="OrchestrationJob" /> object to save.</param>
		public void SaveOrchestrationJob(OrchestrationJob job)
		{
			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			PerformanceFileLogger performanceFileLogger = new PerformanceFileLogger("ORC-SaveJob", performanceLogFilename);

			using (PerformanceCollector collector = new PerformanceCollector(performanceFileLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				_slidingWindowScheduler.DeleteEvents(job.OrchestrationEvents);
				DeleteEvents(job.RemovedIds, performanceTracker);

				job.ValidateEventsBeforeSaving(_api.Connection);
				_slidingWindowScheduler.ScheduleEvents(job.OrchestrationEvents);
				CreateOrUpdateEvents(job.OrchestrationEvents, performanceTracker);
			}
		}

		/// <summary>
		///     Deletes all events and configurations for the given job from the DataMiner system.
		/// </summary>
		/// <param name="job">Job to remove.</param>
		public void DeleteJob(OrchestrationJob job)
		{
			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			PerformanceFileLogger performanceFileLogger = new PerformanceFileLogger("ORC-DeleteJob", performanceLogFilename);

			using (PerformanceCollector collector = new PerformanceCollector(performanceFileLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				_slidingWindowScheduler.DeleteEvents(job.OrchestrationEvents);
				DeleteEvents(job.OrchestrationEvents, performanceTracker);
			}
		}

		/// <summary>
		///     Deletes all events and configurations for the given job from the DataMiner system.
		/// </summary>
		/// <param name="job">Job to remove.</param>
		public void DeleteJobConfiguration(OrchestrationJobConfiguration job)
		{
			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			PerformanceFileLogger performanceFileLogger = new PerformanceFileLogger("ORC-DeleteJobConfiguration", performanceLogFilename);

			using (PerformanceCollector collector = new PerformanceCollector(performanceFileLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				_slidingWindowScheduler.DeleteEvents(job.OrchestrationEvents);
				DeleteEvents(job.OrchestrationEvents, performanceTracker);
			}
		}

		/// <summary>
		///     Start execution for an event, based on ID.
		/// </summary>
		/// <param name="orchestrationIds">The IDs of the events to execute.</param>
		public void ExecuteEventsNow(IEnumerable<Guid> orchestrationIds)
		{
			var eventExecutionHelper = new OrchestrationEventExecutionHelper(_api);

			IEnumerable<Guid> eventIds = orchestrationIds.ToList();
			if (!eventIds.Any())
			{
				return;
			}

			eventExecutionHelper.ExecuteEventsNow(eventIds);
		}

		internal IEnumerable<OrchestrationEvent> GetOrchestrationEventsInTimeRange(DateTime start, DateTime end)
		{
			DateTime localStart = start.ToUniversalTime();
			DateTime localEnd = end.ToUniversalTime();

			if (localStart > localEnd)
			{
				throw new ArgumentException("End time of range filter can not be lower than start time");
			}

			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).GreaterThanOrEqual(localStart))
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).LessThanOrEqual(localEnd));

			return Read(filter);
		}

		internal IEnumerable<OrchestrationEvent> GetOrchestrationEventsAfterTime(DateTime time)
		{
			DateTime localStart = time.ToUniversalTime();

			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).GreaterThanOrEqual(localStart));

			return Read(filter);
		}

		internal IEnumerable<OrchestrationEvent> GetOrchestrationEventsBeforeTime(DateTime time)
		{
			DateTime localEnd = time.ToUniversalTime();

			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).LessThanOrEqual(localEnd));

			return Read(filter);
		}

		/// <summary>
		///     Get all <see cref="OrchestrationEvent" /> objects that contains the given job reference value.
		/// </summary>
		/// <param name="jobReference">Job reference value to filter.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>A collection of <see cref="OrchestrationEvent" /> objects that contains the given job reference value.</returns>
		/// <exception cref="ArgumentException">Job reference can not be null or whitespace.</exception>
		private IEnumerable<OrchestrationEvent> GetEventsByJobReference(string jobReference, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				if (String.IsNullOrEmpty(jobReference))
				{
					throw new ArgumentException($"'{nameof(jobReference)}' cannot be null or empty", nameof(jobReference));
				}

				ManagedFilter<DomInstance, IEnumerable> filter = DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobReference).Equal(jobReference);

				return Read(filter);
			}
		}

		/// <summary>
		///     Get all <see cref="OrchestrationEventConfiguration" /> objects that contains the given job reference value.
		/// </summary>
		/// <param name="jobReference">Job reference value to filter.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>
		///     A collection of <see cref="OrchestrationEventConfiguration" /> objects that contains the given job reference
		///     value.
		/// </returns>
		/// <exception cref="ArgumentException">Job reference can not be null or whitespace.</exception>
		private IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsByJobReference(string jobReference, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				if (String.IsNullOrEmpty(jobReference))
				{
					throw new ArgumentException($"'{nameof(jobReference)}' cannot be null or empty.", nameof(jobReference));
				}

				IEnumerable<OrchestrationEvent> events = GetEventsByJobReference(jobReference, performanceTracker);
				return GetEventsAsEventConfigurations(events, performanceTracker).Values;
			}
		}

		/// <summary>
		///     Get the <see cref="OrchestrationEvent" /> object that matches the given event ID value.
		/// </summary>
		/// <param name="eventIds">The ID of the instance to lookup.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>
		///     A <see cref="OrchestrationEvent" /> object that matches the given event ID value, or null if no match is
		///     found.
		/// </returns>
		/// <exception cref="ArgumentException">Event ID can not be an empty Guid.</exception>
		private IEnumerable<OrchestrationEvent> GetEventsById(IEnumerable<Guid> eventIds, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				List<Guid> instanceIds = eventIds.ToList();
				if (instanceIds == null || instanceIds.Any(guid => guid == Guid.Empty))
				{
					throw new ArgumentException($"'{nameof(eventIds)}' cannot contain empty Guids.", nameof(eventIds));
				}

				ORFilterElement<DomInstance> combinedFilter = new ORFilterElement<DomInstance>(instanceIds.Select(id => FilterElementFactory.Create(DomInstanceExposers.Id, Comparer.Equals, id)).ToArray());

				IEnumerable<OrchestrationEvent> result = Read(combinedFilter);

				return result;
			}
		}

		/// <summary>
		///     Get the <see cref="OrchestrationEventConfiguration" /> object that matches the given event ID value.
		/// </summary>
		/// <param name="eventIds">The IDs of the instances to lookup.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>
		///     A <see cref="OrchestrationEventConfiguration" /> object that matches the given event ID value, or null if no
		///     match is found.
		/// </returns>
		/// <exception cref="ArgumentException">Event ID can not be an empty Guid.</exception>
		internal IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsById(IEnumerable<Guid> eventIds, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				List<Guid> instanceIds = eventIds.ToList();
				if (instanceIds == null || instanceIds.Any(guid => guid == Guid.Empty))
				{
					throw new ArgumentException($"'{nameof(eventIds)}' cannot contain empty Guids.", nameof(eventIds));
				}

				IEnumerable<OrchestrationEvent> orchestrationEvents = GetEventsById(instanceIds, performanceTracker);

				return GetEventsAsEventConfigurations(orchestrationEvents, performanceTracker).Values;
			}
		}

		/// <summary>
		///     Convert a collection of <see cref="OrchestrationEvent" /> objects to <see cref="OrchestrationEventConfiguration" />
		///     objects by retrieving configuration data from DataMiner.
		/// </summary>
		/// <param name="events">The <see cref="OrchestrationEvent" /> objects to convert.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>A mapping of each event ID to the converted <see cref="OrchestrationEventConfiguration" /> object.</returns>
		/// <exception cref="ArgumentNullException">Events can not be null.</exception>
		private Dictionary<Guid, OrchestrationEventConfiguration> GetEventsAsEventConfigurations(IEnumerable<OrchestrationEvent> events, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
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
		}

		/// <summary>
		///     Saves a list of new or updated <see cref="OrchestrationEvent" /> objects to the DataMiner System.
		/// </summary>
		/// <param name="events">A list of configured or updated event configurations.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		private void CreateOrUpdateEvents(IEnumerable<OrchestrationEvent> events, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				CreateOrUpdate(events);
			}
		}

		/// <summary>
		///     Saves a list of new or updated <see cref="OrchestrationEventConfiguration" /> objects to the DataMiner System.
		/// </summary>
		/// <param name="events">A list of configured or updated event configurations.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		internal void SaveEventConfigurations(IEnumerable<OrchestrationEventConfiguration> events, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				SaveEventConfigurations(events);
			}
		}

		internal void SaveEventConfigurations(IEnumerable<OrchestrationEventConfiguration> events)
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

			CreateOrUpdate(orchestrationEventConfigurations);
		}

		/// <summary>
		///     Delete a collection of <see cref="OrchestrationEvent" /> objects from the DataMiner system.
		/// </summary>
		/// <param name="eventIds">The events to be deleted.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		private void DeleteEvents(IEnumerable<Guid> eventIds, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				ICollection<OrchestrationEvent> orchestrationEvents = Read(eventIds).Values;

				DeleteEvents(orchestrationEvents, performanceTracker);
			}
		}

		/// <summary>
		///     Delete a collection of <see cref="OrchestrationEvent" /> objects from the DataMiner system.
		/// </summary>
		/// <param name="events">The events to be deleted.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		private void DeleteEvents(IEnumerable<OrchestrationEvent> events, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				IEnumerable<OrchestrationEvent> orchestrationEvents = events.ToList();
				IEnumerable<Guid> configurationsToDelete = orchestrationEvents.Where(e => e.ConfigurationReference.HasValue).Select(e => e.ConfigurationReference.Value.ID);

				_configurationHelper.Delete(GetConfigurationInstances(configurationsToDelete).Values);

				Delete(orchestrationEvents);
			}
		}

		protected internal override OrchestrationEvent CreateInstance(DomInstance domInstance)
		{
			return new OrchestrationEvent(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<OrchestrationEvent> instances)
		{
		}

		protected override void ValidateBeforeDelete(ICollection<OrchestrationEvent> instances)
		{
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
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobReference), comparer, Convert.ToString(value));
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