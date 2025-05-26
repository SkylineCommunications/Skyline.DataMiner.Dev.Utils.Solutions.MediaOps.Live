namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcOrchestration
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ToolsSpace.Collections;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;

	using SLDataGateway.API.Types.Querying;

	using Comparer = Skyline.DataMiner.Net.Messages.SLDataGateway.Comparer;
	using Connection = Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration.Connection;

	public class OrchestrationEventRepository : Repository<OrchestrationEvent>
	{
		private readonly MediaOpsLiveApi _api;
		private readonly ConfigurationRepository _configurationHelper;
		private readonly OrchestrationScheduler _scheduler;

		public OrchestrationEventRepository(SlcOrchestrationHelper helper, MediaOpsLiveApi api) : base(helper)
		{
			_configurationHelper = new ConfigurationRepository(helper);
			_scheduler = new OrchestrationScheduler(api.Dms, api.Connection);
			_api = api;
		}

		protected internal override DomDefinitionId DomDefinition => OrchestrationEvent.DomDefinition;

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
		/// <returns>The saved <see cref="OrchestrationJobConfiguration" />.</returns>
		public OrchestrationJobConfiguration SaveOrchestrationJobConfiguration(OrchestrationJobConfiguration job)
		{
			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			PerformanceFileLogger performanceFileLogger = new PerformanceFileLogger("ORC-SaveJobConfiguration", performanceLogFilename);

			using (PerformanceCollector collector = new PerformanceCollector(performanceFileLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				DeleteEvents(job.RemovedIds, performanceTracker);

				job.ValidateEventsBeforeSaving();
				_scheduler.CreateOrUpdateEventScheduling(job.OrchestrationEvents);
				IEnumerable<OrchestrationEventConfiguration> successes = SaveEventConfigurations(job.OrchestrationEvents, performanceTracker);
				return new OrchestrationJobConfiguration(job.JobId, successes.ToList());
			}
		}

		/// <summary>
		///     Saves the job to the DataMiner system.
		/// </summary>
		/// <param name="job">The <see cref="OrchestrationJob" /> object to save.</param>
		/// <returns>The saved <see cref="OrchestrationJob" />.</returns>
		public OrchestrationJob SaveOrchestrationJob(OrchestrationJob job)
		{
			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			PerformanceFileLogger performanceFileLogger = new PerformanceFileLogger("ORC-SaveJob", performanceLogFilename);

			using (PerformanceCollector collector = new PerformanceCollector(performanceFileLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				DeleteEvents(job.RemovedIds, performanceTracker);

				job.ValidateEventsBeforeSaving();
				_scheduler.CreateOrUpdateEventScheduling(job.OrchestrationEvents);
				IEnumerable<OrchestrationEvent> successes = CreateOrUpdateEvents(job.OrchestrationEvents, performanceTracker);
				return new OrchestrationJob(job.JobId, successes.ToList());
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
				_scheduler.DeleteEventTasks(job.OrchestrationEvents);
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
				_scheduler.DeleteEventTasks(job.OrchestrationEvents);
				DeleteEvents(job.OrchestrationEvents, performanceTracker);
			}
		}

		/// <summary>
		///     Start execution for an event, based on ID.
		/// </summary>
		/// <param name="orchestrationIds">The IDs of the events to execute.</param>
		public void ExecuteEventsNow(IEnumerable<Guid> orchestrationIds)
		{
			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			PerformanceFileLogger performanceFileLogger = new PerformanceFileLogger("ORC-ExecuteEventNow", performanceLogFilename);

			using (PerformanceCollector collector = new PerformanceCollector(performanceFileLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				List<OrchestrationEventConfiguration> orchestrationEvents = GetEventConfigurationsById(orchestrationIds, performanceTracker).ToList();

				if (!orchestrationEvents.Any())
				{
					return;
				}

				ExecuteEvents(orchestrationEvents, performanceTracker);
			}
		}

		private void ExecuteEvents(List<OrchestrationEventConfiguration> orchestrationEvents, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				foreach (OrchestrationEventConfiguration orchestrationEvent in orchestrationEvents)
				{
					orchestrationEvent.InternalSetState(SlcOrchestrationIds.Enums.EventState.Configuring);
				}

				SaveEventConfigurations(orchestrationEvents, performanceTracker);

				List<Task> tasks = new List<Task>();
				foreach (OrchestrationEventConfiguration orchestrationEvent in orchestrationEvents)
				{
					Task nodeOrchestrationTask = Task.Factory.StartNew(
						() =>
						{
							ExecuteEventConfigurationScripts(orchestrationEvent, performanceTracker);
						},
						TaskCreationOptions.LongRunning);

					tasks.Add(nodeOrchestrationTask);
				}

				ExecuteConnections(orchestrationEvents, performanceTracker);

				Task.WaitAll(tasks.ToArray());

				SaveEventConfigurations(orchestrationEvents, performanceTracker);
			}
		}

		private void ExecuteEventConfigurationScripts(OrchestrationEventConfiguration orchestrationEventConfiguration, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				if (!String.IsNullOrEmpty(orchestrationEventConfiguration.GlobalOrchestrationScript))
				{
					ExecuteGlobalConfiguration(orchestrationEventConfiguration, performanceTracker);
					return;
				}

				ExecuteNodesConfiguration(orchestrationEventConfiguration, performanceTracker);
			}
		}

		private void ExecuteConnections(List<OrchestrationEventConfiguration> orchestrationEventConfigurations, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				List<Connection> allConnections = new List<Connection>();
				List<OrchestrationEventConfiguration> eventConfigurationsWithConnections = orchestrationEventConfigurations
					.Where(orchestrationEventConfiguration => orchestrationEventConfiguration?.Configuration?.Connections != null && orchestrationEventConfiguration.Configuration.Connections.Any()).ToList();

				foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in eventConfigurationsWithConnections)
				{
					allConnections.AddRange(orchestrationEventConfiguration.Configuration.Connections);
				}

				IEnumerable<IGrouping<Guid, Connection>> groupedByDestination = allConnections.GroupBy(conn => conn.DestinationVsg.Value.ID);
				IEnumerable<Guid> destinationsWithConflicts = groupedByDestination
					.Where(group => group
						.Select(conn => conn.SourceVsg.Value.ID).Distinct().Count() > 1)
					.Select(group => group.Key);

				List<Connection> connectionsToConfigure = new List<Connection>();
				foreach (OrchestrationEventConfiguration eventConfigurationsWithConnection in eventConfigurationsWithConnections)
				{
					List<Guid> conflictedConnectionIdsInEvent = eventConfigurationsWithConnection.Configuration.Connections
						.Where(conn => destinationsWithConflicts.Contains(conn.DestinationVsg.Value.ID))
						.Select(conn => conn.DestinationVsg.Value.ID).ToList();

					if (conflictedConnectionIdsInEvent.Any())
					{
						eventConfigurationsWithConnection.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
						eventConfigurationsWithConnection.FailureInfo +=
							$"\nFollowing Destination VSG(s) have a conflicting configuration for another event at the same time: {String.Join("/", conflictedConnectionIdsInEvent)}";
						continue;
					}

					connectionsToConfigure.AddRange(eventConfigurationsWithConnection.Configuration.Connections);
					eventConfigurationsWithConnection.InternalSetState(SlcOrchestrationIds.Enums.EventState.Completed);
				}

				ExecuteTakeForConnections(connectionsToConfigure, performanceTracker);
			}
		}

		private void ExecuteTakeForConnections(List<Connection> connections, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				if (!connections.Any())
				{
					return;
				}

				TakeHelper takeHelper = new TakeHelper(_api);

				ConcurrentHashSet<string> errors = new ConcurrentHashSet<string>();

				List<VsgConnectionRequest> requests = new List<VsgConnectionRequest>();

				HashSet<Guid> allInvolvedVsgIds = new HashSet<Guid>();
				foreach (Connection connection in connections)
				{
					allInvolvedVsgIds.Add(connection.SourceVsg.Value.ID);
					allInvolvedVsgIds.Add(connection.DestinationVsg.Value.ID);
				}

				IDictionary<Guid, VirtualSignalGroup> allInvolvedVsgs = _api.VirtualSignalGroups.Read(allInvolvedVsgIds);

				foreach (Connection connection in connections)
				{
					VirtualSignalGroup srcVirtualSignalGroup = allInvolvedVsgs[connection.SourceVsg.Value.ID];
					VirtualSignalGroup dstVirtualSignalGroup = allInvolvedVsgs[connection.DestinationVsg.Value.ID];

					requests.Add(new VsgConnectionRequest(srcVirtualSignalGroup, dstVirtualSignalGroup));
				}

				takeHelper.Take(_api.Engine, requests, performanceTracker.Collector);
			}
		}

		private void ExecuteNodesConfiguration(OrchestrationEventConfiguration orchestrationEventConfiguration, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				ConcurrentHashSet<string> errors = new ConcurrentHashSet<string>();
				List<Task> nodeOrchestrationTasks = new List<Task>();

				foreach (NodeConfiguration nodeConfiguration in orchestrationEventConfiguration.Configuration.NodeConfigurations)
				{
					if (String.IsNullOrEmpty(nodeConfiguration.OrchestrationScriptName))
					{
						continue;
					}

					Task nodeOrchestrationTask = Task.Factory.StartNew(
						() =>
						{
							if (!TryExecuteOrchestrationScript(nodeConfiguration.OrchestrationScriptName, nodeConfiguration.OrchestrationScriptArguments, performanceTracker,
								    out string[] errorMessages))
							{
								errors.TryAdd($"\nError during orchestration for node {nodeConfiguration.NodeId}: {String.Join("\n", errorMessages)}");
								orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
							}
						},
						TaskCreationOptions.LongRunning);

					nodeOrchestrationTasks.Add(nodeOrchestrationTask);
				}

				Task.WaitAll(nodeOrchestrationTasks.ToArray());

				if (orchestrationEventConfiguration.EventState == SlcOrchestrationIds.Enums.EventState.Failed)
				{
					orchestrationEventConfiguration.FailureInfo += String.Join("\n", errors);
					return;
				}

				orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Completed);
			}
		}

		private void ExecuteGlobalConfiguration(OrchestrationEventConfiguration orchestrationEventConfiguration, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				if (!TryExecuteOrchestrationScript(orchestrationEventConfiguration.GlobalOrchestrationScript, orchestrationEventConfiguration.GlobalOrchestrationScriptArguments, performanceTracker,
					    out string[] errorMessages))
				{
					orchestrationEventConfiguration.FailureInfo += $"Error during global orchestration: {String.Join("\n", errorMessages)}";
					orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
					return;
				}

				orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Completed);
			}
		}

		private bool TryExecuteOrchestrationScript(string scriptName, IEnumerable<OrchestrationScriptArgument> arguments, PerformanceTracker performanceTracker, out string[] errorMessages)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				IDmsAutomationScript script = _api.Dms.GetScript(scriptName);
				List<DmsAutomationScriptParamValue> scriptParams = arguments
					.Select(arg => new DmsAutomationScriptParamValue(arg.Name, arg.Value))
					.ToList();

				DmsAutomationScriptRunOptions scriptOptions = new DmsAutomationScriptRunOptions
				{
					ExtendedErrorInfo = true,
				};

				DmsAutomationScriptResult scriptResult = script.Execute(scriptParams, new List<DmsAutomationScriptDummyValue>(), scriptOptions);

				if (scriptResult.HadError)
				{
					errorMessages = scriptResult.ErrorMessages;
					return false;
				}

				errorMessages = Array.Empty<string>();
				return true;
			}
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
		internal IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsByJobReference(string jobReference, PerformanceTracker performanceTracker)
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
		/// <param name="eventId">The ID of the instance to lookup.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>
		///     A <see cref="OrchestrationEvent" /> object that matches the given event ID value, or null if no match is
		///     found.
		/// </returns>
		/// <exception cref="ArgumentException">Event ID can not be an empty Guid.</exception>
		private OrchestrationEvent GetEventById(Guid eventId, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				if (eventId == Guid.Empty)
				{
					throw new ArgumentException($"'{nameof(eventId)}' cannot be empty.", nameof(eventId));
				}

				ManagedFilter<DomInstance, Guid> filter = DomInstanceExposers.Id.Equal(eventId);

				IEnumerable<OrchestrationEvent> result = Read(filter);

				return result.FirstOrDefault();
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
		/// <param name="eventId">The ID of the instance to lookup.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>
		///     A <see cref="OrchestrationEventConfiguration" /> object that matches the given event ID value, or null if no
		///     match is found.
		/// </returns>
		/// <exception cref="ArgumentException">Event ID can not be an empty Guid.</exception>
		private OrchestrationEventConfiguration GetEventConfigurationById(Guid eventId, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				if (eventId == Guid.Empty)
				{
					throw new ArgumentException($"'{nameof(eventId)}' cannot be empty.", nameof(eventId));
				}

				OrchestrationEvent orchestrationEvent = GetEventById(eventId, performanceTracker);

				if (orchestrationEvent == null)
				{
					return null;
				}

				return GetEventsAsEventConfigurations(orchestrationEvent, performanceTracker);
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
		private IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsById(IEnumerable<Guid> eventIds, PerformanceTracker performanceTracker)
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
		///     Convert a <see cref="OrchestrationEvent" /> object to a <see cref="OrchestrationEventConfiguration" /> object by
		///     retrieving configuration data from DataMiner.
		/// </summary>
		/// <param name="event">The <see cref="OrchestrationEvent" /> object to convert.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>
		///     The <see cref="OrchestrationEventConfiguration" /> object that corresponds to the given input, or null if the
		///     operation failed.
		/// </returns>
		/// <exception cref="ArgumentNullException">Event can not be null.</exception>
		private OrchestrationEventConfiguration GetEventsAsEventConfigurations(OrchestrationEvent @event, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				if (@event == null)
				{
					throw new ArgumentNullException(nameof(@event));
				}

				return GetEventsAsEventConfigurations(new List<OrchestrationEvent> { @event }, performanceTracker).Values.FirstOrDefault();
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
		/// <returns>Returns a list of all successfully saved event configurations.</returns>
		private IEnumerable<OrchestrationEvent> CreateOrUpdateEvents(IEnumerable<OrchestrationEvent> events, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				BulkCreateOrUpdateResult<DomInstance, DomInstanceId> results = CreateOrUpdateWithResult(events);

				return results.SuccessfulItems.Select(item => new OrchestrationEvent(item));
			}
		}

		/// <summary>
		///     Saves a list of new or updated <see cref="OrchestrationEventConfiguration" /> objects to the DataMiner System.
		/// </summary>
		/// <param name="events">A list of configured or updated event configurations.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>Returns a list of all successfully saved <see cref="OrchestrationEventConfiguration" /> objects.</returns>
		private IEnumerable<OrchestrationEventConfiguration> SaveEventConfigurations(IEnumerable<OrchestrationEventConfiguration> events, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
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

				BulkCreateOrUpdateResult<DomInstance, DomInstanceId> results = CreateOrUpdateWithResult(orchestrationEventConfigurations);

				return orchestrationEventConfigurations.Where(config => results.SuccessfulIds.Contains(config.DomInstance.ID));
			}
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
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.ReservationInstance), comparer,
						(string)value);

				case nameof(OrchestrationEvent.FailureInfo):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.FailureInfo), comparer, (string)value);

				case nameof(OrchestrationEvent.GlobalOrchestrationScript):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.GlobalConfiguration.OrchestrationScriptName), comparer,
						(string)value);

				case nameof(OrchestrationEvent.JobReference):
					return FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobReference), comparer,
						Convert.ToString(value));
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
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.GlobalConfiguration.OrchestrationScriptName), sortOrder,
						naturalSort);

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