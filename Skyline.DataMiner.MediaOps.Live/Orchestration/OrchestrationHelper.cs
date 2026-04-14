namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Skyline.DataMiner.Solutions.MediaOps.Live.API;
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Subscriptions;
using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Helpers;
using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;
using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling;
using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Utils.PerformanceAnalyzer;
using Skyline.DataMiner.Solutions.MediaOps.Live.Plan;

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

		_orchestrationEventRepository = new OrchestrationEventRepository(api);
		_configurationRepository = new ConfigurationRepository(api);
		_jobInfoRepository = new JobInfoRepository(api);

		_slidingWindowScheduler = new OrchestrationSlidingWindowScheduler(
			_orchestrationEventRepository,
			TimeSpan.FromHours(Constants.SchedulerSlidingWindowRangeHours_Past),
			TimeSpan.FromHours(Constants.SchedulerSlidingWindowRangeHours_Future));

		Scripts = new OrchestrationScriptInfoHelper(api);
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
		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-GetOrCreateJob")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			OrchestrationJobInfo jobInfo = _jobInfoRepository.GetJobInfoByJobReference(jobReference);
			IEnumerable<OrchestrationEvent> events = _orchestrationEventRepository.GetEventsByJobInfoReference(jobInfo, performanceTracker);

			if (jobInfo != null)
			{
				return new OrchestrationJob(jobInfo, events);
			}

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
		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-GetOrCreateJobConfiguration")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			OrchestrationJobInfo jobInfo = _jobInfoRepository.GetJobInfoByJobReference(jobReference);
			IEnumerable<OrchestrationEventConfiguration> events = GetEventConfigurationsByJobInfoReference(jobInfo, performanceTracker);

			if (jobInfo != null)
			{
				return new OrchestrationJobConfiguration(jobInfo, events);
			}

			return new OrchestrationJobConfiguration(jobReference, events);
		}
	}

	/// <summary>
	///     Gets the <see cref="OrchestrationJob" /> object with all events for the given job reference,
	///     or <see langword="null" /> if no existing job is found.
	/// </summary>
	/// <param name="jobReference">The ID of the job to retrieve.</param>
	/// <returns>
	///     A <see cref="OrchestrationJob" /> object with all events found for the given job reference,
	///     or <see langword="null" /> if no job exists for the given reference.
	/// </returns>
	public OrchestrationJob GetOrchestrationJob(string jobReference)
	{
		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-GetJob")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			OrchestrationJobInfo jobInfo = _jobInfoRepository.GetJobInfoByJobReference(jobReference);

			if (jobInfo == null)
			{
				return null;
			}

			IEnumerable<OrchestrationEvent> events = _orchestrationEventRepository.GetEventsByJobInfoReference(jobInfo, performanceTracker);
			return new OrchestrationJob(jobInfo, events);
		}
	}

	/// <summary>
	///     Gets the <see cref="OrchestrationJobConfiguration" /> object with all event configurations for the given job
	///     reference, or <see langword="null" /> if no existing job is found.
	/// </summary>
	/// <param name="jobReference">The ID of the job to retrieve.</param>
	/// <returns>
	///     A <see cref="OrchestrationJobConfiguration" /> object with all event configurations found for the given job
	///     reference, or <see langword="null" /> if no job exists for the given reference.
	/// </returns>
	public OrchestrationJobConfiguration GetOrchestrationJobConfiguration(string jobReference)
	{
		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-GetJobConfiguration")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			OrchestrationJobInfo jobInfo = _jobInfoRepository.GetJobInfoByJobReference(jobReference);

			if (jobInfo == null)
			{
				return null;
			}

			IEnumerable<OrchestrationEventConfiguration> events = GetEventConfigurationsByJobInfoReference(jobInfo, performanceTracker);
			return new OrchestrationJobConfiguration(jobInfo, events);
		}
	}

	/// <summary>
	///     Creates a dictionary with <see cref="OrchestrationJob" /> objects with all events for the given jobs
	///     reference.
	/// </summary>
	/// <param name="jobReferences">The IDs of the jobs to retrieve.</param>
	/// <returns>
	///     A dictionary with <see cref="OrchestrationJob" /> objects with all event configurations found for the given jobs
	///     reference.
	/// </returns>
	public IEnumerable<OrchestrationJob> GetOrCreateNewOrchestrationJobs(IEnumerable<string> jobReferences)
	{
		Dictionary<string, OrchestrationJobInfo> jobInfos = _jobInfoRepository.GetJobInfosByJobReference(jobReferences);
		List<OrchestrationEvent> events = _orchestrationEventRepository.Read(
			new ORFilterElement<OrchestrationEvent>(jobInfos.Values.Select(reference => OrchestrationEventExposers.JobInfoReference.Equal(reference.ID)).ToArray())).ToList();

		List<OrchestrationJob> jobs = new List<OrchestrationJob>();

		foreach (OrchestrationJobInfo orchestrationJobInfo in jobInfos.Values)
		{
			var jobEvents = events.Where(ev => ev.JobInfoReference.Value.ID == orchestrationJobInfo.ID);

			var job = new OrchestrationJob(orchestrationJobInfo, jobEvents);

			jobs.Add(job);
		}

		return jobs;
	}

	/// <summary>
	///     Creates a collection <see cref="OrchestrationJobConfiguration" /> object with all event configurations for the given jobs
	///     reference.
	/// </summary>
	/// <param name="jobReferences">The ID of the job to retrieve.</param>
	/// <returns>
	///     A collection of <see cref="OrchestrationJobConfiguration" /> objects with all event configurations found for the given job
	///     reference.
	/// </returns>
	public IEnumerable<OrchestrationJobConfiguration> GetOrCreateNewOrchestrationJobConfigurations(IEnumerable<string> jobReferences)
	{
		Dictionary<string, OrchestrationJobInfo> jobInfos = _jobInfoRepository.GetJobInfosByJobReference(jobReferences);
		List<OrchestrationEvent> events = _orchestrationEventRepository.Read(
			new ORFilterElement<OrchestrationEvent>(jobInfos.Values.Select(reference => OrchestrationEventExposers.JobInfoReference.Equal(reference.ID)).ToArray())).ToList();

		List<OrchestrationEventConfiguration> allEventConfigurations = GetEventsAsEventConfigurations(events).Values.ToList();

		List<OrchestrationJobConfiguration> jobs = new List<OrchestrationJobConfiguration>();

		foreach (OrchestrationJobInfo orchestrationJobInfo in jobInfos.Values)
		{
			var jobEvents = allEventConfigurations.Where(ev => ev.JobInfoReference.Value.ID == orchestrationJobInfo.ID);

			var job = new OrchestrationJobConfiguration(orchestrationJobInfo, jobEvents);

			jobs.Add(job);
		}

		return jobs;
	}

	/// <summary>
	/// Get all <see cref="OrchestrationJob" /> objects in the DataMiner system.
	/// </summary>
	/// <returns>All <see cref="OrchestrationJob" /> objects.</returns>
	public IEnumerable<OrchestrationJob> GetAllJobs()
	{
		List<OrchestrationEvent> allEvents = _orchestrationEventRepository.ReadAll().ToList();
		List<OrchestrationJobInfo> allJobInfos = _jobInfoRepository.ReadAll().ToList();
		List<Configuration> allConfigurations = _configurationRepository.ReadAll().ToList();

		List<OrchestrationJob> jobs = new List<OrchestrationJob>();

		foreach (OrchestrationJobInfo orchestrationJobInfo in allJobInfos)
		{
			var jobEvents = allEvents.Where(ev => ev.JobInfoReference.Value.ID == orchestrationJobInfo.ID);

			var job = new OrchestrationJob(orchestrationJobInfo, jobEvents);

			jobs.Add(job);
		}

		return jobs;
	}

	/// <summary>
	/// Get all <see cref="OrchestrationJobConfiguration" /> objects in the DataMiner system.
	/// </summary>
	/// <returns>All <see cref="OrchestrationJobConfiguration" /> objects.</returns>
	public IEnumerable<OrchestrationJobConfiguration> GetAllJobConfigurations()
	{
		List<OrchestrationEvent> allEvents = _orchestrationEventRepository.ReadAll().ToList();
		List<OrchestrationJobInfo> allJobInfos = _jobInfoRepository.ReadAll().ToList();

		List<OrchestrationEventConfiguration> allEventConfigurations = GetEventsAsEventConfigurations(allEvents).Values.ToList();

		List<OrchestrationJobConfiguration> jobs = new List<OrchestrationJobConfiguration>();

		foreach (OrchestrationJobInfo orchestrationJobInfo in allJobInfos)
		{
			var jobEvents = allEventConfigurations.Where(ev => ev.JobInfoReference.Value.ID == orchestrationJobInfo.ID);

			var job = new OrchestrationJobConfiguration(orchestrationJobInfo, jobEvents);

			jobs.Add(job);
		}

		return jobs;
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
		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-SaveJobConfiguration")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			DeleteOrchestrationEvents(job.RemovedIds, performanceTracker);

			job.ValidateEventsBeforeSaving(_api);

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
		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-SaveJob")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			DeleteOrchestrationEvents(job.RemovedIds, performanceTracker);

			job.ValidateEventsBeforeSaving(_api);
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
		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-DeleteJob")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			DeleteOrchestrationEvents(job.OrchestrationEvents, performanceTracker);

			_jobInfoRepository.Delete(job.JobInfo);
		}
	}

	/// <summary>
	///     Deletes all events and configurations for the given jobs from the DataMiner system.
	/// </summary>
	/// <param name="jobs">Jobs to remove.</param>
	public void DeleteJobs(IEnumerable<OrchestrationJob> jobs)
	{
		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-DeleteJobs")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			IEnumerable<OrchestrationJob> orchestrationJobs = jobs.ToList();
			DeleteOrchestrationEvents(orchestrationJobs.SelectMany(job => job.OrchestrationEvents), performanceTracker);

			_jobInfoRepository.Delete(orchestrationJobs.Select(job => job.JobInfo));
		}
	}

	/// <summary>
	///     Deletes all events and configurations for the given job configuration from the DataMiner system.
	/// </summary>
	/// <param name="job">Job configuration to remove.</param>
	public void DeleteJobConfiguration(OrchestrationJobConfiguration job)
	{
		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-DeleteJobConfiguration")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			DeleteOrchestrationEvents(job.OrchestrationEvents, performanceTracker);

			_jobInfoRepository.Delete(job.JobInfo);
		}
	}

	/// <summary>
	///     Deletes all events and configurations for the given job configurations from the DataMiner system.
	/// </summary>
	/// <param name="jobs">Job configurations to remove.</param>
	public void DeleteJobConfigurations(IEnumerable<OrchestrationJobConfiguration> jobs)
	{
		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-DeleteJobConfigurations")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			IEnumerable<OrchestrationJobConfiguration> orchestrationJobs = jobs.ToList();
			DeleteOrchestrationEvents(orchestrationJobs.SelectMany(job => job.OrchestrationEvents), performanceTracker);

			_jobInfoRepository.Delete(orchestrationJobs.Select(job => job.JobInfo));
		}
	}

	/// <summary>
	///     Start execution for an event, based on ID.
	/// </summary>
	/// <param name="orchestrationIds">The IDs of the events to execute.</param>
	/// <param name="mediaOpsPlanHelper">The MediaOps.PLAN helper.</param>
	/// <param name="settings">Additional settings can be passed to override default orchestration settings.</param>
	public void ExecuteEventsNow(IEnumerable<Guid> orchestrationIds, IMediaOpsPlanHelper mediaOpsPlanHelper, OrchestrationSettings settings = null)
	{
		if (orchestrationIds is null)
		{
			throw new ArgumentNullException(nameof(orchestrationIds));
		}

		if (mediaOpsPlanHelper is null)
		{
			throw new ArgumentNullException(nameof(mediaOpsPlanHelper));
		}

		List<Guid> eventIds = orchestrationIds.ToList();
		if (!eventIds.Any())
		{
			return;
		}

		List<OrchestrationEventConfiguration> orchestrationEvents = GetEventConfigurationsById(eventIds).ToList();
		ExecuteEventsNow(orchestrationEvents, mediaOpsPlanHelper, settings);
	}

	/// <summary>
	///     Start execution for a set of events.
	/// </summary>
	/// <param name="orchestrationEvents">The events to execute.</param>
	/// <param name="mediaOpsPlanHelper">The MediaOps.PLAN helper.</param>
	/// <param name="settings">Additional settings can be passed to override default orchestration settings.</param>
	public void ExecuteEventsNow(IEnumerable<OrchestrationEventConfiguration> orchestrationEvents, IMediaOpsPlanHelper mediaOpsPlanHelper, OrchestrationSettings settings = null)
	{
		if (orchestrationEvents is null)
		{
			throw new ArgumentNullException(nameof(orchestrationEvents));
		}

		if (mediaOpsPlanHelper is null)
		{
			throw new ArgumentNullException(nameof(mediaOpsPlanHelper));
		}

		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-ExecuteEventsNow")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			var eventExecutionHelper = new OrchestrationEventExecutionHelper(_api, mediaOpsPlanHelper, settings);

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
	///     Start execution for a set of events.
	/// </summary>
	/// <param name="orchestrationEvents">The events to execute.</param>
	/// <param name="mediaOpsPlanHelper">The MediaOps.PLAN helper.</param>
	public void ExecuteEventsNow(IEnumerable<OrchestrationEvent> orchestrationEvents, IMediaOpsPlanHelper mediaOpsPlanHelper)
	{
		if (orchestrationEvents is null)
		{
			throw new ArgumentNullException(nameof(orchestrationEvents));
		}

		if (mediaOpsPlanHelper is null)
		{
			throw new ArgumentNullException(nameof(mediaOpsPlanHelper));
		}

		Dictionary<Guid, OrchestrationEventConfiguration> eventConfigs = GetEventsAsEventConfigurations(orchestrationEvents);
		ExecuteEventsNow(eventConfigs.Values, mediaOpsPlanHelper);
	}

	/// <summary>
	///     Start asynchronous execution for an event, based on ID.
	/// </summary>
	/// <param name="orchestrationIds">The IDs of the events to execute.</param>
	/// <param name="mediaOpsPlanHelper">The MediaOps.PLAN helper.</param>
	/// <param name="settings">Additional settings can be passed to override default orchestration settings.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public async Task ExecuteEventsNowAsync(IEnumerable<Guid> orchestrationIds, IMediaOpsPlanHelper mediaOpsPlanHelper, OrchestrationSettings settings = null)
	{
		if (orchestrationIds is null)
		{
			throw new ArgumentNullException(nameof(orchestrationIds));
		}

		if (mediaOpsPlanHelper is null)
		{
			throw new ArgumentNullException(nameof(mediaOpsPlanHelper));
		}

		List<Guid> eventIds = orchestrationIds.ToList();
		if (!eventIds.Any())
		{
			return;
		}

		List<OrchestrationEventConfiguration> orchestrationEvents = GetEventConfigurationsById(eventIds).ToList();
		await ExecuteEventsNowAsync(orchestrationEvents, mediaOpsPlanHelper, settings);
	}

	/// <summary>
	///     Start asynchronous execution for a set of events.
	/// </summary>
	/// <param name="orchestrationEvents">The events to execute.</param>
	/// <param name="mediaOpsPlanHelper">The MediaOps.PLAN helper.</param>
	/// <param name="settings">Additional settings can be passed to override default orchestration settings.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public async Task ExecuteEventsNowAsync(IEnumerable<OrchestrationEventConfiguration> orchestrationEvents, IMediaOpsPlanHelper mediaOpsPlanHelper, OrchestrationSettings settings = null)
	{
		if (orchestrationEvents is null)
		{
			throw new ArgumentNullException(nameof(orchestrationEvents));
		}

		if (mediaOpsPlanHelper is null)
		{
			throw new ArgumentNullException(nameof(mediaOpsPlanHelper));
		}

		using (PerformanceCollector collector = new(PerformanceLoggerFactory.Create("ORC-ExecuteEventsNowAsync")))
		using (PerformanceTracker performanceTracker = new(collector))
		{
			var eventExecutionHelper = new OrchestrationEventExecutionHelper(_api, mediaOpsPlanHelper, settings);

			IEnumerable<OrchestrationEventConfiguration> events = orchestrationEvents.ToList();
			if (!events.Any())
			{
				return;
			}

			// If an execute is called on an event that was set in the future, remove scheduled tasks for it since we only allow it to execute once.
			_slidingWindowScheduler.DeleteEvents(events.Where(e => e.EventTime > DateTimeOffset.Now));

			await eventExecutionHelper.ExecuteEventsNowAsync(events, performanceTracker);
		}
	}

	/// <summary>
	///     Start asynchronous execution for a set of events.
	/// </summary>
	/// <param name="orchestrationEvents">The events to execute.</param>
	/// <param name="mediaOpsPlanHelper">The MediaOps.PLAN helper.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public async Task ExecuteEventsNowAsync(IEnumerable<OrchestrationEvent> orchestrationEvents, IMediaOpsPlanHelper mediaOpsPlanHelper)
	{
		if (orchestrationEvents is null)
		{
			throw new ArgumentNullException(nameof(orchestrationEvents));
		}

		if (mediaOpsPlanHelper is null)
		{
			throw new ArgumentNullException(nameof(mediaOpsPlanHelper));
		}

		Dictionary<Guid, OrchestrationEventConfiguration> eventConfigs = GetEventsAsEventConfigurations(orchestrationEvents);
		await ExecuteEventsNowAsync(eventConfigs.Values, mediaOpsPlanHelper);
	}

	/// <summary>
	/// Provides a subscription to orchestration events.
	/// </summary>
	/// <returns>A subscription to the orchestration events repository.</returns>
	public RepositorySubscription<OrchestrationEvent> SubscribeOnEvents()
	{
		return _orchestrationEventRepository.Subscribe();
	}

	/// <summary>
	/// Retrieve all events in the given time range.
	/// </summary>
	/// <param name="start">The start time.</param>
	/// <param name="end">The end time.</param>
	/// <returns>The set of events that are found between the given start and end time.</returns>
	public IEnumerable<OrchestrationEvent> GetAllOrchestrationEventsInTimeRange(DateTime start, DateTime end)
	{
		return _orchestrationEventRepository.GetOrchestrationEventsInTimeRange(start, end);
	}

	/// <summary>
	/// Generic method to get a set of events, using a provided filter. The filter can be built using the OrchestrationEventExposers class.
	/// </summary>
	/// <param name="filter">A filter (or combination of filters) to get a set of events.</param>
	/// <returns>A set of events, matching the provided filter information.</returns>
	public IEnumerable<OrchestrationEvent> ReadEvents(FilterElement<OrchestrationEvent> filter)
	{
		return _orchestrationEventRepository.Read(filter);
	}

	/// <summary>
	/// Generic method to get a set of event configurations, using a provided filter. The filter can be built using the OrchestrationEventExposers class.
	/// </summary>
	/// <param name="filter">A filter (or combination of filters) to get a set of event configurations.</param>
	/// <returns>A set of event configurations, matching the provided filter information.</returns>
	public IEnumerable<OrchestrationEventConfiguration> ReadEventConfigurations(FilterElement<OrchestrationEvent> filter)
	{
		var events = _orchestrationEventRepository.Read(filter);
		return GetEventsAsEventConfigurations(events).Values;
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
	public Dictionary<Guid, OrchestrationEventConfiguration> GetEventsAsEventConfigurations(IEnumerable<OrchestrationEvent> events)
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

	internal IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsById(IEnumerable<Guid> eventIds)
	{
		if (eventIds is null)
		{
			throw new ArgumentNullException(nameof(eventIds));
		}

		List<Guid> instanceIds = eventIds.ToList();

		if (instanceIds.Any(guid => guid == Guid.Empty))
		{
			throw new ArgumentException($"'{nameof(eventIds)}' cannot contain empty Guids.", nameof(eventIds));
		}

		IEnumerable<OrchestrationEvent> orchestrationEvents = _orchestrationEventRepository.GetEventsById(instanceIds);

		return GetEventsAsEventConfigurations(orchestrationEvents).Values;
	}
}