namespace Skyline.DataMiner.MediaOps.Live.Orchestration;

using System;
using System.Collections.Generic;
using System.Linq;

using Skyline.DataMiner.MediaOps.Live.API;
using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
using Skyline.DataMiner.MediaOps.Live.API.Repositories.Orchestration;
using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
using Skyline.DataMiner.MediaOps.Live.Orchestration.Scheduling;
using Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper;
using Skyline.DataMiner.Utils.PerformanceAnalyzer;
using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;

/// <summary>
/// Exposes API methods to interact with and orchestrate MediaOps Live Orchestration events.
/// </summary>
public class OrchestrationHelper
{
	private readonly MediaOpsLiveApi _api;
	private readonly ConfigurationRepository _configurationRepository;
	private readonly OrchestrationEventRepository _orchestrationEventRepository;
	private readonly JobInfoRepository _jobInfoRepository;
	private readonly OrchestrationSlidingWindowScheduler _slidingWindowScheduler;

	/// <summary>
	///     Initializes a new instance of the <see cref="OrchestrationHelper" /> class.
	/// </summary>
	/// <param name="helper">Orchestration helper.</param>
	/// <param name="api">Api that calls the repository.</param>
	internal OrchestrationHelper(SlcOrchestrationHelper helper, MediaOpsLiveApi api)
	{
		_api = api;

		_orchestrationEventRepository = new OrchestrationEventRepository(helper, api.Connection);
		_configurationRepository = new ConfigurationRepository(helper, api.Connection);
		_jobInfoRepository = new JobInfoRepository(helper, api.Connection);

		_slidingWindowScheduler = new OrchestrationSlidingWindowScheduler(
			_orchestrationEventRepository,
			TimeSpan.FromHours(Constants.SchedulerSlidingWindowRangeHours_Past),
			TimeSpan.FromHours(Constants.SchedulerSlidingWindowRangeHours_Future));

		Scripts = new OrchestrationScriptInfoHelper(api.Connection);
	}

	public OrchestrationScriptInfoHelper Scripts { get; }

	internal JobInfoRepository JobInfos => _jobInfoRepository;

	/// <summary>
	///     Executes a sync for the current timing window (default 1 hour in the past and 12 hours into the future).
	///     This consists of deleting past orchestration tasks and prepare tasks for upcoming events.
	/// </summary>
	public void SyncCurrentSlidingWindow()
	{
		_slidingWindowScheduler.SyncSchedulerWithWindow();
	}

	/// <summary>
	///     Creates a recurring scheduled task to prepare orchestration tasks in a sliding window manner.
	///     If the task already exists, no new task is created.
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
		PerformanceFileLogger performanceFileLogger = new("ORC-GetOrCreateJob", performanceLogFilename);

		using (PerformanceCollector collector = new(performanceFileLogger))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			OrchestrationJobInfo jobInfo = _jobInfoRepository.GetJobInfoByJobReference(jobReference);
			IEnumerable<OrchestrationEvent> events = _orchestrationEventRepository.GetEventsByJobInfoReference(jobInfo, performanceTracker);

			OrchestrationJob job = new(jobReference, events);

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
		PerformanceFileLogger performanceFileLogger = new("ORC-GetOrCreateJobConfiguration", performanceLogFilename);

		using (PerformanceCollector collector = new(performanceFileLogger))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			OrchestrationJobInfo jobInfo = _jobInfoRepository.GetJobInfoByJobReference(jobReference);
			IEnumerable<OrchestrationEventConfiguration> events = GetEventConfigurationsByJobInfoReference(jobInfo, performanceTracker);

			OrchestrationJobConfiguration job = new(jobReference, events);

			if (jobInfo != null)
			{
				job.JobInfo = jobInfo;
			}

			return job;
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
	private IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsByJobInfoReference(OrchestrationJobInfo jobInfo, PerformanceTracker performanceTracker)
	{
		using (performanceTracker = new PerformanceTracker(performanceTracker))
		{
			IEnumerable<OrchestrationEvent> events = _orchestrationEventRepository.GetEventsByJobInfoReference(jobInfo, performanceTracker);
			return GetEventsAsEventConfigurations(events, performanceTracker).Values;
		}
	}

	/// <summary>
	///     Saves the job configuration to the DataMiner system.
	/// </summary>
	/// <param name="job">The <see cref="OrchestrationJobConfiguration" /> object to save.</param>
	public void SaveOrchestrationJobConfiguration(OrchestrationJobConfiguration job)
	{
		string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
		PerformanceFileLogger performanceFileLogger = new("ORC-SaveJobConfiguration", performanceLogFilename);

		using (PerformanceCollector collector = new(performanceFileLogger))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			DeleteOrchestrationEvents(job.RemovedIds, performanceTracker);

			job.ValidateEventsBeforeSaving(_api.Connection);

			_slidingWindowScheduler.ScheduleEvents(job.OrchestrationEvents);

			_jobInfoRepository.CreateOrUpdate(job.JobInfo);
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
		PerformanceFileLogger performanceFileLogger = new("ORC-SaveJob", performanceLogFilename);

		using (PerformanceCollector collector = new(performanceFileLogger))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			DeleteOrchestrationEvents(job.RemovedIds, performanceTracker);

			job.ValidateEventsBeforeSaving(_api.Connection);
			_slidingWindowScheduler.ScheduleEvents(job.OrchestrationEvents);

			_jobInfoRepository.CreateOrUpdate(job.JobInfo);
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
		PerformanceFileLogger performanceFileLogger = new("ORC-DeleteJob", performanceLogFilename);

		using (PerformanceCollector collector = new(performanceFileLogger))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			DeleteOrchestrationEvents(job.OrchestrationEvents, performanceTracker);

			_jobInfoRepository.Delete(job.JobInfo);
		}
	}

	/// <summary>
	///     Deletes all events and configurations for the given job from the DataMiner system.
	/// </summary>
	/// <param name="job">Job to remove.</param>
	public void DeleteJobConfiguration(OrchestrationJobConfiguration job)
	{
		string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
		PerformanceFileLogger performanceFileLogger = new("ORC-DeleteJobConfiguration", performanceLogFilename);

		using (PerformanceCollector collector = new(performanceFileLogger))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			DeleteOrchestrationEvents(job.OrchestrationEvents, performanceTracker);

			_jobInfoRepository.Delete(job.JobInfo);
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

		List<OrchestrationEventConfiguration> orchestrationEvents = GetEventConfigurationsById(eventIds).ToList();
		ExecuteEventsNow(orchestrationEvents, settings);
	}

	internal IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsById(IEnumerable<Guid> eventIds)
	{
		List<Guid> instanceIds = eventIds.ToList();

		if (instanceIds == null || instanceIds.Any(guid => guid == Guid.Empty))
		{
			throw new ArgumentException($"'{nameof(eventIds)}' cannot contain empty Guids.", nameof(eventIds));
		}

		IEnumerable<OrchestrationEvent> orchestrationEvents = _orchestrationEventRepository.GetEventsById(instanceIds);

		return GetEventsAsEventConfigurations(orchestrationEvents).Values;
	}

	/// <summary>
	///     Start execution for an event, based on ID.
	/// </summary>
	/// <param name="orchestrationEvents">The events to execute.</param>
	/// <param name="settings">Additional settings can be passed to override default orchestration settings.</param>
	public void ExecuteEventsNow(IEnumerable<OrchestrationEventConfiguration> orchestrationEvents, OrchestrationSettings settings = null)
	{
		string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
		PerformanceFileLogger performanceFileLogger = new("ORC-ExecuteEventsNow", performanceLogFilename);

		using (PerformanceCollector collector = new(performanceFileLogger))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			OrchestrationEventExecutionHelper eventExecutionHelper = new(_api, settings);

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
		Dictionary<Guid, OrchestrationEventConfiguration> eventConfigs = GetEventsAsEventConfigurations(orchestrationEvents);
		ExecuteEventsNow(eventConfigs.Values);
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
			_configurationRepository.Delete(configsToDelete);
		}

		_configurationRepository.CreateOrUpdate(configsToWrite);

		_orchestrationEventRepository.CreateOrUpdate(orchestrationEventConfigurations);
	}

	private void DeleteOrchestrationEvents(IEnumerable<OrchestrationEvent> events)
	{
		IEnumerable<OrchestrationEvent> orchestrationEvents = events.ToList();
		IEnumerable<Guid> configurationsToDelete = orchestrationEvents.Where(e => e.ConfigurationReference.HasValue).Select(e => e.ConfigurationReference.Value.ID);

		_slidingWindowScheduler.DeleteEvents(orchestrationEvents);

		_configurationRepository.Delete(_configurationRepository.Read(configurationsToDelete).Values);

		_orchestrationEventRepository.Delete(orchestrationEvents);
	}

	/// <summary>
	///     Delete a collection of <see cref="OrchestrationEvent" /> objects from the DataMiner system.
	/// </summary>
	/// <param name="eventIds">The events to be deleted.</param>
	/// <param name="performanceTracker">Performance tracking object.</param>
	private void DeleteOrchestrationEvents(IEnumerable<Guid> eventIds, PerformanceTracker performanceTracker)
	{
		using (performanceTracker = new PerformanceTracker(performanceTracker))
		{
			ICollection<OrchestrationEvent> orchestrationEvents = _orchestrationEventRepository.Read(eventIds).Values;

			DeleteOrchestrationEvents(orchestrationEvents, performanceTracker);
		}
	}

	/// <summary>
	///     Delete a collection of <see cref="OrchestrationEvent" /> objects from the DataMiner system.
	/// </summary>
	/// <param name="events">The events to be deleted.</param>
	/// <param name="performanceTracker">Performance tracking object.</param>
	private void DeleteOrchestrationEvents(IEnumerable<OrchestrationEvent> events, PerformanceTracker performanceTracker)
	{
		using (new PerformanceTracker(performanceTracker))
		{
			DeleteOrchestrationEvents(events);
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

		IDictionary<Guid, Configuration> configurationMapping = _configurationRepository.Read(instancesToRetrieve);

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
			_orchestrationEventRepository.CreateOrUpdate(events);
		}
	}
}