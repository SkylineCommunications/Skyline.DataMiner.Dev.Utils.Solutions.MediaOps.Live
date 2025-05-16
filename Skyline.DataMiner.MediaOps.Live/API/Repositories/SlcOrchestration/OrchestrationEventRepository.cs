namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ToolsSpace.Collections;

	using SLDataGateway.API.Types.Querying;

	using TakeLevelMapping = Skyline.DataMiner.MediaOps.Live.Take.LevelMapping;
	using TakeLevel = Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement.Level;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	public class OrchestrationEventRepository : Repository<OrchestrationEvent>
	{
		private readonly ConfigurationRepository _configurationHelper;
		private readonly OrchestrationScheduler _scheduler;
		private readonly MediaOpsLiveApi _api;

		private const string TakeVsgScriptName = "VSG-AS-TakeVsg";
		private const string TakeVsgSourceIdParam = "Source VSG ID";
		private const string TakeVsgDestinationIdParam = "Destination VSG ID";

		public OrchestrationEventRepository(SlcOrchestrationHelper helper, MediaOpsLiveApi api) : base(helper)
		{
			_configurationHelper = new ConfigurationRepository(helper);
			_scheduler = new OrchestrationScheduler(api.Dms);
			_api = api;
		}

		protected internal override DomDefinitionId DomDefinition => OrchestrationEvent.DomDefinition;

		/// <summary>
		/// Creates a <see cref="OrchestrationJob"/> object with all events for the given job reference.
		/// </summary>
		/// <param name="jobReference">The ID of the job to retrieve.</param>
		/// <returns>A <see cref="OrchestrationJob"/> object with all events found for the given job reference.</returns>
		public OrchestrationJob GetOrCreateNewOrchestrationJob(string jobReference)
		{
			IEnumerable<OrchestrationEvent> events = GetEventsByJobReference(jobReference);

			return new OrchestrationJob(jobReference, events);
		}

		/// <summary>
		/// Creates a <see cref="OrchestrationJobConfiguration"/> object with all event configurations for the given job reference.
		/// </summary>
		/// <param name="jobReference">The ID of the job to retrieve.</param>
		/// <returns>A <see cref="OrchestrationJobConfiguration"/> object with all event configurations found for the given job reference.</returns>
		public OrchestrationJobConfiguration GetOrCreateNewOrchestrationJobConfiguration(string jobReference)
		{
			IEnumerable<OrchestrationEventConfiguration> events = GetEventConfigurationsByJobReference(jobReference);

			return new OrchestrationJobConfiguration(jobReference, events);
		}

		/// <summary>
		/// Saves the job configuration to the DataMiner system.
		/// </summary>
		/// <param name="job">The <see cref="OrchestrationJobConfiguration"/> object to save.</param>
		/// <returns>The saved <see cref="OrchestrationJobConfiguration"/>.</returns>
		public OrchestrationJobConfiguration SaveOrchestrationJobConfiguration(OrchestrationJobConfiguration job)
		{
			DeleteEvents(job.RemovedIds);

			job.ValidateEventsBeforeSaving();
			_scheduler.CreateOrUpdateEventTasks(job.OrchestrationEvents);
			var successes = SaveEventConfigurations(job.OrchestrationEvents);
			return new OrchestrationJobConfiguration(job.JobId, successes.ToList());
		}

		/// <summary>
		/// Saves the job to the DataMiner system.
		/// </summary>
		/// <param name="job">The <see cref="OrchestrationJob"/> object to save.</param>
		/// <returns>The saved <see cref="OrchestrationJob"/>.</returns>
		public OrchestrationJob SaveOrchestrationJob(OrchestrationJob job)
		{
			DeleteEvents(job.RemovedIds);

			job.ValidateEventsBeforeSaving();
			_scheduler.CreateOrUpdateEventTasks(job.OrchestrationEvents);
			var successes = CreateOrUpdateEvents(job.OrchestrationEvents);
			return new OrchestrationJob(job.JobId, successes.ToList());
		}

		/// <summary>
		/// Deletes all events and configurations for the given job from the DataMiner system.
		/// </summary>
		/// <param name="job">Job to remove.</param>
		public void DeleteJob(OrchestrationJob job)
		{
			_scheduler.DeleteEventTasks(job.OrchestrationEvents);
			DeleteEvents(job.OrchestrationEvents);
		}

		/// <summary>
		/// Deletes all events and configurations for the given job from the DataMiner system.
		/// </summary>
		/// <param name="job">Job to remove.</param>
		public void DeleteJobConfiguration(OrchestrationJobConfiguration job)
		{
			_scheduler.DeleteEventTasks(job.OrchestrationEvents);
			DeleteEvents(job.OrchestrationEvents);
		}

		/// <summary>
		/// Start execution for an event, based on ID.
		/// </summary>
		/// <param name="orchestrationEventGuid">The ID of the event to execute.</param>
		public void ExecuteEventNow(Guid orchestrationEventGuid)
		{
			OrchestrationEventConfiguration orchestrationEvent = GetEventConfigurationById(orchestrationEventGuid);

			if (orchestrationEvent == null)
			{
				return;
			}

			ExecuteEvent(orchestrationEvent);
		}

		private void ExecuteEvent(OrchestrationEventConfiguration orchestrationEvent)
		{
			orchestrationEvent.InternalSetState(SlcOrchestrationIds.Enums.EventState.Configuring);

			SaveEventConfigurations(new List<OrchestrationEventConfiguration> { orchestrationEvent });

			ExecuteEventConfiguration(orchestrationEvent);

			SaveEventConfigurations(new List<OrchestrationEventConfiguration> { orchestrationEvent });
		}

		private void ExecuteEventConfiguration(OrchestrationEventConfiguration orchestrationEventConfiguration)
		{
			if (!String.IsNullOrEmpty(orchestrationEventConfiguration.GlobalOrchestrationScript))
			{
				ExecuteGlobalConfiguration(orchestrationEventConfiguration);
				return;
			}

			ExecuteNodesConfiguration(orchestrationEventConfiguration);
			ExecuteConnections(orchestrationEventConfiguration);
		}

		private void ExecuteConnections(OrchestrationEventConfiguration orchestrationEventConfiguration)
		{
			TakeHelper takeHelper = new TakeHelper(_api);

			ConcurrentHashSet<string> errors = new ConcurrentHashSet<string>();

			List<VsgConnectionRequest> requests = new List<VsgConnectionRequest>();

			foreach (Connection connection in orchestrationEventConfiguration.Configuration.Connections)
			{
				try
				{
					var srcVirtualSignalGroup = _api.VirtualSignalGroups.Read(connection.SourceVsg.Value.ID);
					var dstVirtualSignalGroup = _api.VirtualSignalGroups.Read(connection.DestinationVsg.Value.ID);

					requests.Add(new VsgConnectionRequest(srcVirtualSignalGroup, dstVirtualSignalGroup));
				}
				catch (Exception e)
				{
					errors.TryAdd($"\n{e}");
					orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
				}
			}

			var performanceLogFilename = $"ORC-TAKE - {DateTime.UtcNow:yyyy-MM-dd}";
			var performanceFileLogger = new PerformanceFileLogger("ORC-TAKE", performanceLogFilename);

			using (var performanceCollector = new PerformanceCollector(performanceFileLogger))
			{
				takeHelper.Take(requests, performanceCollector);
			}

			if (orchestrationEventConfiguration.EventState == SlcOrchestrationIds.Enums.EventState.Failed)
			{
				orchestrationEventConfiguration.FailureInfo += String.Join("\n", errors);
				return;
			}

			orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Completed);
		}

		private void ExecuteNodesConfiguration(OrchestrationEventConfiguration orchestrationEventConfiguration)
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
						if (!TryExecuteOrchestrationScript(nodeConfiguration.OrchestrationScriptName, nodeConfiguration.OrchestrationScriptArguments, out string[] errorMessages))
						{
							errors.TryAdd($"\nError during orchestration for node {nodeConfiguration.NodeId}: {String.Join("\n", errorMessages)}" );
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

		private void ExecuteGlobalConfiguration(OrchestrationEventConfiguration orchestrationEventConfiguration)
		{
			if (!TryExecuteOrchestrationScript(orchestrationEventConfiguration.GlobalOrchestrationScript, orchestrationEventConfiguration.GlobalOrchestrationScriptArguments, out string[] errorMessages))
			{
				orchestrationEventConfiguration.FailureInfo += $"Error during global orchestration: {String.Join("\n", errorMessages)}";
				orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
				return;
			}

			orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Completed);
		}

		private bool TryExecuteOrchestrationScript(string scriptName, IEnumerable<OrchestrationScriptArgument> arguments, out string[] errorMessages)
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

		/// <summary>
		/// Get all <see cref="OrchestrationEvent"/> objects that contains the given job reference value.
		/// </summary>
		/// <param name="jobReference">Job reference value to filter.</param>
		/// <returns>A collection of <see cref="OrchestrationEvent"/> objects that contains the given job reference value.</returns>
		/// <exception cref="ArgumentException">Job reference can not be null or whitespace.</exception>
		private IEnumerable<OrchestrationEvent> GetEventsByJobReference(string jobReference)
		{
			if (String.IsNullOrEmpty(jobReference))
			{
				throw new ArgumentException($"'{nameof(jobReference)}' cannot be null or empty", nameof(jobReference));
			}

			var filter = DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobReference).Equal(jobReference);

			return Read(filter);
		}

		/// <summary>
		/// Get all <see cref="OrchestrationEventConfiguration"/> objects that contains the given job reference value.
		/// </summary>
		/// <param name="jobReference">Job reference value to filter.</param>
		/// <returns>A collection of <see cref="OrchestrationEventConfiguration"/> objects that contains the given job reference value.</returns>
		/// <exception cref="ArgumentException">Job reference can not be null or whitespace.</exception>
		internal IEnumerable<OrchestrationEventConfiguration> GetEventConfigurationsByJobReference(string jobReference)
		{
			if (String.IsNullOrEmpty(jobReference))
			{
				throw new ArgumentException($"'{nameof(jobReference)}' cannot be null or empty.", nameof(jobReference));
			}

			var events = GetEventsByJobReference(jobReference);
			return GetEventsAsEventConfigurations(events).Values;
		}

		/// <summary>
		/// Get the <see cref="OrchestrationEvent"/> object that matches the given event ID value.
		/// </summary>
		/// <param name="eventId">The ID of the instance to lookup.</param>
		/// <returns>A <see cref="OrchestrationEvent"/> object that matches the given event ID value, or null if no match is found.</returns>
		/// <exception cref="ArgumentException">Event ID can not be an empty Guid.</exception>
		private OrchestrationEvent GetEventById(Guid eventId)
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
		/// <exception cref="ArgumentException">Event ID can not be an empty Guid.</exception>
		private OrchestrationEventConfiguration GetEventConfigurationById(Guid eventId)
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
		/// <exception cref="ArgumentNullException">Event can not be null.</exception>
		private OrchestrationEventConfiguration GetEventsAsEventConfigurations(OrchestrationEvent @event)
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
		/// <exception cref="ArgumentNullException">Events can not be null.</exception>
		private Dictionary<Guid, OrchestrationEventConfiguration> GetEventsAsEventConfigurations(IEnumerable<OrchestrationEvent> events)
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
		private IEnumerable<OrchestrationEvent> CreateOrUpdateEvents(IEnumerable<OrchestrationEvent> events)
		{
			var results = CreateOrUpdateWithResult(events);

			return results.SuccessfulItems.Select(item => new OrchestrationEvent(domInstance: item));
		}

		/// <summary>
		/// Saves a list of new or updated <see cref="OrchestrationEventConfiguration"/> objects to the DataMiner System.
		/// </summary>
		/// <param name="events">A list of configured or updated event configurations.</param>
		/// <returns>Returns a list of all successfully saved <see cref="OrchestrationEventConfiguration"/> objects.</returns>
		private IEnumerable<OrchestrationEventConfiguration> SaveEventConfigurations(IEnumerable<OrchestrationEventConfiguration> events)
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
		/// Delete a collection of <see cref="OrchestrationEvent"/> objects from the DataMiner system.
		/// </summary>
		/// <param name="eventIds">The events to be deleted.</param>
		private void DeleteEvents(IEnumerable<Guid> eventIds)
		{
			var orchestrationEvents = Read(eventIds).Values;

			DeleteEvents(orchestrationEvents);
		}

		/// <summary>
		/// Delete a collection of <see cref="OrchestrationEvent"/> objects from the DataMiner system.
		/// </summary>
		/// <param name="events">The events to be deleted.</param>
		private void DeleteEvents(IEnumerable<OrchestrationEvent> events)
		{
			IEnumerable<OrchestrationEvent> orchestrationEvents = events.ToList();
			var configurationsToDelete = orchestrationEvents.Where(e => e.ConfigurationReference.HasValue).Select(e => e.ConfigurationReference.Value.ID);

			_configurationHelper.Delete(GetConfigurationInstances(configurationsToDelete).Values);

			Delete(orchestrationEvents);
		}

		protected override OrchestrationEvent CreateInstance(DomInstance domInstance)
		{
			return new OrchestrationEvent(domInstance: domInstance);
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
