namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.Orchestration
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Scheduling;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper;
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
		private readonly JobInfoRepository _jobInfoHelper;
		private readonly OrchestrationSlidingWindowScheduler _slidingWindowScheduler;
		private readonly OrchestrationScriptInfoHelper _orchestrationScriptInfoHelper;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationEventRepository"/> class.
		/// </summary>
		/// <param name="api">Api that calls the repository.</param>
		internal OrchestrationEventRepository(MediaOpsLiveApi api) : base(api, api.SlcOrchestrationHelper)
		{
			_configurationHelper = new ConfigurationRepository(api);
			_jobInfoHelper = new JobInfoRepository(api);
			_api = api;
			_slidingWindowScheduler = new OrchestrationSlidingWindowScheduler(
				this,
				TimeSpan.FromHours(Constants.SchedulerSlidingWindowRangeHours_Past),
				TimeSpan.FromHours(Constants.SchedulerSlidingWindowRangeHours_Future));

			_orchestrationScriptInfoHelper = new OrchestrationScriptInfoHelper(api.Connection);
		}

		public OrchestrationScriptInfoHelper Scripts => _orchestrationScriptInfoHelper;

		internal JobInfoRepository JobInfos => _jobInfoHelper;

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
				var jobInfo = _jobInfoHelper.GetJobInfoByJobReference(jobReference);
				IEnumerable<OrchestrationEvent> events = GetEventsByJobInfoReference(jobInfo, performanceTracker);

				OrchestrationJob job = new OrchestrationJob(jobReference, events);

				if (jobInfo != null)
				{
					job.JobInfo = jobInfo;
				}

				return job;
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
				var jobInfo = _jobInfoHelper.GetJobInfoByJobReference(jobReference);
				IEnumerable<OrchestrationEventConfiguration> events = GetEventConfigurationsByJobReference(jobInfo, performanceTracker);

				OrchestrationJobConfiguration job = new OrchestrationJobConfiguration(jobReference, events);

				if (jobInfo != null)
				{
					job.JobInfo = jobInfo;
				}

				return job;
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
				Delete(job.RemovedIds, performanceTracker);

				job.ValidateEventsBeforeSaving(_api.Connection);

				_slidingWindowScheduler.ScheduleEvents(job.OrchestrationEvents);

				_jobInfoHelper.CreateOrUpdate(job.JobInfo);
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
				Delete(job.RemovedIds, performanceTracker);

				job.ValidateEventsBeforeSaving(_api.Connection);
				_slidingWindowScheduler.ScheduleEvents(job.OrchestrationEvents);

				_jobInfoHelper.CreateOrUpdate(job.JobInfo);
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
				Delete(job.OrchestrationEvents, performanceTracker);
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
				Delete(job.OrchestrationEvents, performanceTracker);
			}
		}

		/// <summary>
		///     Start execution for an event, based on ID.
		/// </summary>
		/// <param name="orchestrationIds">The IDs of the events to execute.</param>
		/// <param name="settings">Additional settings can be passed to override default orchestration settings.</param>
		public void ExecuteEventsNow(IEnumerable<Guid> orchestrationIds, OrchestrationSettings settings = null)
		{
			List<Guid> eventIds = orchestrationIds.ToList();
			if (!eventIds.Any())
			{
				return;
			}

			List<OrchestrationEventConfiguration> orchestrationEvents = _api.Orchestration.GetEventConfigurationsById(eventIds).ToList();
			ExecuteEventsNow(orchestrationEvents, settings);
		}

		/// <summary>
		///     Start execution for an event, based on ID.
		/// </summary>
		/// <param name="orchestrationEvents">The events to execute.</param>
		/// <param name="settings">Additional settings can be passed to override default orchestration settings.</param>
		public void ExecuteEventsNow(IEnumerable<OrchestrationEventConfiguration> orchestrationEvents, OrchestrationSettings settings = null)
		{
			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			PerformanceFileLogger performanceFileLogger = new PerformanceFileLogger("ORC-ExecuteEventsNow", performanceLogFilename);

			using (PerformanceCollector collector = new PerformanceCollector(performanceFileLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				var eventExecutionHelper = new OrchestrationEventExecutionHelper(_api, settings);

				IEnumerable<OrchestrationEventConfiguration> events = orchestrationEvents.ToList();
				if (!events.Any())
				{
					return;
				}

				// If an execute is called on an event that was set in the future, remove scheduled tasks for it since we only allow it to execute once.
				_slidingWindowScheduler.DeleteEvents(events.Where(e => e.EventTime > DateTimeOffset.Now));

				eventExecutionHelper.ExecuteEventsNow(events, performanceTracker);
			}
		}

		/// <summary>
		///     Start execution for an event, based on ID.
		/// </summary>
		/// <param name="orchestrationEvents">The events to execute.</param>
		public void ExecuteEventsNow(IEnumerable<OrchestrationEvent> orchestrationEvents)
		{
			var eventConfigs = GetEventsAsEventConfigurations(orchestrationEvents);
			ExecuteEventsNow(eventConfigs.Values);
		}

		public override void Delete(OrchestrationEvent orchestrationEvent)
		{
			Delete(new List<OrchestrationEvent> { orchestrationEvent });
		}

		public override void Delete(IEnumerable<OrchestrationEvent> events)
		{
			IEnumerable<OrchestrationEvent> orchestrationEvents = events.ToList();
			IEnumerable<Guid> configurationsToDelete = orchestrationEvents.Where(e => e.ConfigurationReference.HasValue).Select(e => e.ConfigurationReference.Value.ID);

			_slidingWindowScheduler.DeleteEvents(orchestrationEvents);

			_configurationHelper.Delete(GetConfigurationInstances(configurationsToDelete).Values);

			HashSet<Guid> jobInfosToDelete = new HashSet<Guid>();
			foreach (OrchestrationEvent orchestrationEvent in orchestrationEvents)
			{
				if (orchestrationEvent.JobInfoReference.HasValue)
				{
					jobInfosToDelete.Add(orchestrationEvent.JobInfoReference.Value.ID);
				}
			}

			_jobInfoHelper.Delete(_jobInfoHelper.Read(jobInfosToDelete).Values);

			base.Delete(orchestrationEvents);
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
		/// <param name="jobInfo">Job reference object to filter.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>A collection of <see cref="OrchestrationEvent" /> objects that contains the given job reference value.</returns>
		/// <exception cref="ArgumentException">Job reference can not be null or whitespace.</exception>
		private IEnumerable<OrchestrationEvent> GetEventsByJobInfoReference(OrchestrationJobInfo jobInfo, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				if (jobInfo == null)
				{
					return new List<OrchestrationEvent>();
				}

				ManagedFilter<DomInstance, IEnumerable> filter = DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobInformation).Equal(jobInfo.ID);

				return Read(filter);
			}
		}

		/// <summary>
		///     Get all <see cref="OrchestrationEventConfiguration" /> objects that contains the given job reference value.
		/// </summary>
		/// <param name="jobInfo">Job reference object to filter.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>
		///     A collection of <see cref="OrchestrationEventConfiguration" /> objects that contains the given job reference
		///     value.
		/// </returns>
		/// <exception cref="ArgumentException">Job reference can not be null or whitespace.</exception>
		private IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsByJobReference(OrchestrationJobInfo jobInfo, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				IEnumerable<OrchestrationEvent> events = GetEventsByJobInfoReference(jobInfo, performanceTracker);
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
				return GetEventsById(eventIds);
			}
		}

		private IEnumerable<OrchestrationEvent> GetEventsById(IEnumerable<Guid> eventIds)
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

		internal IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsById(IEnumerable<Guid> eventIds)
		{
			List<Guid> instanceIds = eventIds.ToList();

			if (instanceIds == null || instanceIds.Any(guid => guid == Guid.Empty))
			{
				throw new ArgumentException($"'{nameof(eventIds)}' cannot contain empty Guids.", nameof(eventIds));
			}

			IEnumerable<OrchestrationEvent> orchestrationEvents = GetEventsById(instanceIds);

			return GetEventsAsEventConfigurations(orchestrationEvents).Values;
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
				return GetEventsAsEventConfigurations(events);
			}
		}

		/// <summary>
		///     Convert a collection of <see cref="OrchestrationEvent" /> objects to <see cref="OrchestrationEventConfiguration" />
		///     objects by retrieving configuration data from DataMiner.
		/// </summary>
		/// <param name="events">The <see cref="OrchestrationEvent" /> objects to convert.</param>
		/// <returns>A mapping of each event ID to the converted <see cref="OrchestrationEventConfiguration" /> object.</returns>
		/// <exception cref="ArgumentNullException">Events can not be null.</exception>
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
						: new ConfigurationInstance()));
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
			List<Configuration> configsToDelete = [];
			List<Configuration> configsToWrite = [];

			IEnumerable<OrchestrationEventConfiguration> orchestrationEventConfigurations = events.ToList();
			foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in orchestrationEventConfigurations)
			{
				if (orchestrationEventConfiguration.Configuration.IsEmpty())
				{
					orchestrationEventConfiguration.ConfigurationReference = null;
					configsToDelete.Add(orchestrationEventConfiguration.Configuration);
					continue;
				}

				orchestrationEventConfiguration.ConfigurationReference = orchestrationEventConfiguration.Configuration.ID;
				configsToWrite.Add(orchestrationEventConfiguration.Configuration);
			}

			if (configsToDelete.Any())
			{
				_configurationHelper.Delete(configsToDelete);
			}

			_configurationHelper.CreateOrUpdate(configsToWrite);

			CreateOrUpdate(orchestrationEventConfigurations);
		}

		/// <summary>
		///     Delete a collection of <see cref="OrchestrationEvent" /> objects from the DataMiner system.
		/// </summary>
		/// <param name="eventIds">The events to be deleted.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		private void Delete(IEnumerable<Guid> eventIds, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				ICollection<OrchestrationEvent> orchestrationEvents = Read(eventIds).Values;

				Delete(orchestrationEvents, performanceTracker);
			}
		}

		/// <summary>
		///     Delete a collection of <see cref="OrchestrationEvent" /> objects from the DataMiner system.
		/// </summary>
		/// <param name="events">The events to be deleted.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		private void Delete(IEnumerable<OrchestrationEvent> events, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				Delete(events);
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
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventName), comparer, value);

				case nameof(OrchestrationEvent.EventType):
					return FilterElementFactory.Create<int>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventType), comparer, value);

				case nameof(OrchestrationEvent.EventState):
					return FilterElementFactory.Create<int>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventState), comparer, value);

				case nameof(OrchestrationEvent.EventTime):
					return FilterElementFactory.Create<DateTimeOffset>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime), comparer, value);

				case nameof(OrchestrationEvent.SchedulerReference):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.SchedulerReference), comparer, value);

				case nameof(OrchestrationEvent.FailureInfo):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.FailureInfo), comparer, value);

				case nameof(OrchestrationEvent.GlobalOrchestrationScript):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.GlobalConfiguration.OrchestrationScriptName), comparer, value);
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

				case nameof(OrchestrationEvent.SchedulerReference):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime), sortOrder, naturalSort);

				case nameof(OrchestrationEvent.FailureInfo):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.FailureInfo), sortOrder, naturalSort);

				case nameof(OrchestrationEvent.GlobalOrchestrationScript):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.GlobalConfiguration.OrchestrationScriptName), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}

		private IDictionary<Guid, Configuration> GetConfigurationInstances(IEnumerable<Guid> instanceGuids)
		{
			return _configurationHelper.Read(instanceGuids);
		}
	}
}