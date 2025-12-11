namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Enums;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Net.ToolsSpace.Collections;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	using Connection = Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration.Connection;
	using Level = Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement.Level;
	using LevelMapping = Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration.LevelMapping;
	using ParameterValue = Skyline.DataMiner.Net.Profiles.ParameterValue;

	internal class OrchestrationEventExecutionHelper
	{
		private readonly MediaOpsLiveApi _api;
		private readonly OrchestrationSettings _settings;

		internal OrchestrationEventExecutionHelper(MediaOpsLiveApi api, OrchestrationSettings settings)
		{
			_api = api;
			_settings = settings ?? new OrchestrationSettings();
		}

		internal void ExecuteEventsNow(IEnumerable<OrchestrationEventConfiguration> orchestrationEvents, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				IEnumerable<OrchestrationEventConfiguration> events = orchestrationEvents.ToList();
				if (!events.Any())
				{
					return;
				}

				List<EventState> statesToDiscard =
				[
					EventState.Failed,
					EventState.Completed,
					EventState.Configuring,
				];

				ExecuteEvents(events.Where(e => !statesToDiscard.Contains(e.EventState)), performanceTracker);
			}
		}

		private void ExecuteEvents(IEnumerable<OrchestrationEventConfiguration> orchestrationEventConfigurations, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			using (var taskScheduler = new MediaOpsTaskScheduler())
			{
				IEnumerable<OrchestrationEventConfiguration> eventConfigurations = orchestrationEventConfigurations.ToList();
				foreach (OrchestrationEventConfiguration orchestrationEvent in eventConfigurations)
				{
					orchestrationEvent.InternalSetState(EventState.Configuring);
					orchestrationEvent.ActualStartTime = DateTimeOffset.UtcNow;
				}

				_api.Orchestration.SaveEventConfigurations(eventConfigurations, performanceTracker);

				// Events with global orchestration script
				List<Task> tasks = GetGlobalOrchestrationTasks(eventConfigurations.Where(config => !String.IsNullOrEmpty(config.GlobalOrchestrationScript)), taskScheduler, performanceTracker);

				// Events with node-by-node orchestration scripts
				tasks.AddRange(GetNodeByNodeOrchestrationTasks(eventConfigurations.Where(config => config.HasScripts() && String.IsNullOrEmpty(config.GlobalOrchestrationScript)), taskScheduler, performanceTracker));

				// Events without scripts but with connections
				tasks.AddRange(GetProcessConnectionTasks(eventConfigurations.Where(config => !config.HasScripts() && config.HasConnections() ), taskScheduler, true, performanceTracker));

				// Events without scripts or connections (nothing to do) can be marked as completed right away.
				SaveOrchestrationResults(eventConfigurations.Where(config => !config.HasScripts() && !config.HasConnections()), performanceTracker);

				Task.WaitAll(tasks.ToArray());
			}
		}

		private List<Task> GetGlobalOrchestrationTasks(IEnumerable<OrchestrationEventConfiguration> eventConfigurations, MediaOpsTaskScheduler taskScheduler, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				List<Task> tasks = [];
				var eventConfigurationWithGlobalScripts = eventConfigurations.Where(e => !String.IsNullOrEmpty(e.GlobalOrchestrationScript)).ToList();

				foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in eventConfigurationWithGlobalScripts)
				{
					Task scriptsExecutionTask = Task.Factory.StartNew(
						() =>
						{
							// Execute global script
							ExecuteGlobalConfiguration(orchestrationEventConfiguration, performanceTracker);

							// Update event state once finished
							SaveOrchestrationResults(new List<OrchestrationEventConfiguration> { orchestrationEventConfiguration }, performanceTracker);
						},
						CancellationToken.None,
						TaskCreationOptions.None,
						taskScheduler);

					tasks.Add(scriptsExecutionTask);
				}

				return tasks;
			}
		}

		private List<Task> GetNodeByNodeOrchestrationTasks(IEnumerable<OrchestrationEventConfiguration> eventConfigurations, MediaOpsTaskScheduler taskScheduler, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				List<Task> tasks = [];
				var eventConfigurationWithNodeScripts = eventConfigurations.Where(e => e.HasScripts()).ToList();

				foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in eventConfigurationWithNodeScripts.Where(e => e.HasScripts()))
				{
					// Connections tasks
					List<Task> connectionsTasks = GetProcessConnectionTasks(new List<OrchestrationEventConfiguration> { orchestrationEventConfiguration }, taskScheduler, false, performanceTracker);

					// Node scripts task
					Task nodeScriptsTask = Task.Factory.StartNew(
						() =>
						{
							ExecuteNodesConfiguration(orchestrationEventConfiguration, taskScheduler, performanceTracker);
						},
						CancellationToken.None,
						TaskCreationOptions.None,
						taskScheduler);

					connectionsTasks.Add(nodeScriptsTask);

					Task combinedTask = Task.Factory.StartNew(
						() =>
						{
							// Wait for both connections and node scripts to finish
							Task.WaitAll(connectionsTasks.ToArray());

							// Update event state once finished
							SaveOrchestrationResults(new List<OrchestrationEventConfiguration> { orchestrationEventConfiguration }, performanceTracker);
						},
						CancellationToken.None,
						TaskCreationOptions.None,
						taskScheduler);

					tasks.Add(combinedTask);
				}

				return tasks;
			}
		}

		private List<Task> GetProcessConnectionTasks(IEnumerable<OrchestrationEventConfiguration> eventConfigurations, MediaOpsTaskScheduler taskScheduler, bool pushResults, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var tasks = new List<Task>();

				var connectionTasksPerEvent = ProcessAndReturnConnectionResults(eventConfigurations, performanceTracker);

				foreach (KeyValuePair<OrchestrationEventConfiguration, ICollection<Task>> keyValuePair in connectionTasksPerEvent)
				{
					OrchestrationEventConfiguration eventConfig = keyValuePair.Key;
					var tasksForEvent = keyValuePair.Value;

					var eventConnectionTask = Task.Factory.StartNew(
						() =>
						{
							Task.WaitAll(tasksForEvent.ToArray());

							foreach (Task completedTask in tasksForEvent)
							{
								if (!completedTask.IsFaulted)
								{
									continue;
								}

								eventConfig.InternalSetState(EventState.Failed);
								eventConfig.FailureInfo += $"\n{completedTask.Exception?.GetBaseException().Message}";
							}

							if (pushResults)
							{
								SaveOrchestrationResults(new List<OrchestrationEventConfiguration> { eventConfig }, performanceTracker);
							}
						},
						CancellationToken.None,
						TaskCreationOptions.None,
						taskScheduler);

					tasks.Add(eventConnectionTask);
				}

				return tasks;
			}
		}

		private void SaveOrchestrationResults(IEnumerable<OrchestrationEventConfiguration> orchestrationEventConfigurations, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				IEnumerable<OrchestrationEventConfiguration> eventConfigurations = orchestrationEventConfigurations.ToList();
				foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in eventConfigurations)
				{
					if (orchestrationEventConfiguration.ActualStartTime != null)
					{
						orchestrationEventConfiguration.OrchestrationDuration = DateTimeOffset.UtcNow - orchestrationEventConfiguration.ActualStartTime;
					}

					if (orchestrationEventConfiguration.EventState != EventState.Failed)
					{
						orchestrationEventConfiguration.InternalSetState(EventState.Completed);
					}

					orchestrationEventConfiguration.SendPlanJobStateUpdate(_api);
				}

				_api.Orchestration.SaveEventConfigurations(eventConfigurations, performanceTracker);
			}
		}

		internal Dictionary<OrchestrationEventConfiguration, ICollection<Task>> ProcessAndReturnConnectionResults(IEnumerable<OrchestrationEventConfiguration> orchestrationEventConfigurations, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				IEnumerable<OrchestrationEventConfiguration> eventConfigurations = orchestrationEventConfigurations.ToList();
				List<OrchestrationEventConfiguration> eventConfigurationsWithConnections = eventConfigurations.Where(ev => ev.HasConnections()).ToList();

				Dictionary<OrchestrationEventConfiguration, List<Connection>> connectionsToConfigureByEvent = [];
				Dictionary<OrchestrationEventConfiguration, List<Connection>> disconnectsToConfigureByEvent = [];

				foreach (OrchestrationEventConfiguration eventConfigurationsWithConnection in eventConfigurationsWithConnections)
				{
					if (eventConfigurationsWithConnection.IsDisconnectEvent)
					{
						disconnectsToConfigureByEvent.Add(eventConfigurationsWithConnection, eventConfigurationsWithConnection.Configuration.Connections.ToList());
					}
					else
					{
						connectionsToConfigureByEvent.Add(eventConfigurationsWithConnection, eventConfigurationsWithConnection.Configuration.Connections.ToList());
					}
				}

				var resultTasksByEvent = new Dictionary<OrchestrationEventConfiguration, ICollection<Task>>();
				ProcessAndAddConnectResults(connectionsToConfigureByEvent, resultTasksByEvent, performanceTracker);
				ProcessAndAddDisconnectResults(disconnectsToConfigureByEvent, resultTasksByEvent, performanceTracker);

				return resultTasksByEvent;
			}
		}

		private void ProcessAndAddDisconnectResults(Dictionary<OrchestrationEventConfiguration, List<Connection>> disconnectsToConfigureByEvent, Dictionary<OrchestrationEventConfiguration, ICollection<Task>> resultTasksByEvent, PerformanceTracker performanceTracker)
		{
			try
			{
				var disconnectResults = ExecuteAndReturnDisconnectResults(disconnectsToConfigureByEvent, performanceTracker);

				foreach (VsgDisconnectResult vsgDisconnectResult in disconnectResults)
				{
					var metaDataConnectionResult = (VsgDisconnectRequestWithMetadata)vsgDisconnectResult.VsgDisconnectRequest;
					OrchestrationEventConfiguration matchingEvent = disconnectsToConfigureByEvent.Keys.FirstOrDefault(ev => ev.ID.ToString() == metaDataConnectionResult.MetaData.ToString());

					if (matchingEvent == null)
					{
						continue;
					}

					if (!resultTasksByEvent.ContainsKey(matchingEvent))
					{
						resultTasksByEvent[matchingEvent] = [];
					}

					resultTasksByEvent[matchingEvent].Add(vsgDisconnectResult.CompletionTask);
				}
			}
			catch (Exception e)
			{
				foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in disconnectsToConfigureByEvent.Keys)
				{
					orchestrationEventConfiguration.InternalSetState(EventState.Failed);
					orchestrationEventConfiguration.FailureInfo += $"\n{e.Message}";
				}
			}
		}

		private void ProcessAndAddConnectResults(Dictionary<OrchestrationEventConfiguration, List<Connection>> connectionsToConfigureByEvent, Dictionary<OrchestrationEventConfiguration, ICollection<Task>> resultTasksByEvent, PerformanceTracker performanceTracker)
		{
			try
			{
				var connectionResults = ExecuteAndReturnConnectResults(connectionsToConfigureByEvent, performanceTracker);
				foreach (VsgConnectionResult vsgConnectionResult in connectionResults)
				{
					var metaDataConnectionResult = (VsgConnectionRequestWithMetaData)vsgConnectionResult.VsgConnectionRequest;
					OrchestrationEventConfiguration matchingEvent = connectionsToConfigureByEvent.Keys.FirstOrDefault(ev => ev.ID.ToString() == metaDataConnectionResult.MetaData.ToString());

					if (matchingEvent == null)
					{
						continue;
					}

					if (!resultTasksByEvent.ContainsKey(matchingEvent))
					{
						resultTasksByEvent[matchingEvent] = [];
					}

					resultTasksByEvent[matchingEvent].Add(vsgConnectionResult.CompletionTask);
				}
			}
			catch (Exception e)
			{
				foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in connectionsToConfigureByEvent.Keys)
				{
					orchestrationEventConfiguration.InternalSetState(EventState.Failed);
					orchestrationEventConfiguration.FailureInfo += $"\n{e.Message}";
				}
			}
		}

		private ICollection<VsgDisconnectResult> ExecuteAndReturnDisconnectResults(Dictionary<OrchestrationEventConfiguration, List<Connection>> disconnectsPerEvent, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				if (!disconnectsPerEvent.Any())
				{
					return new List<VsgDisconnectResult>();
				}

				List<VsgDisconnectRequest> requests = [];
				List<VirtualSignalGroup> virtualSignalGroupsToUnlock = [];

				HashSet<Guid> allInvolvedVsgIds = [];
				HashSet<int> allInvolvedLevelNumbers = [];

				foreach (Connection connection in disconnectsPerEvent.SelectMany(kv => kv.Value))
				{
					if (connection.DestinationVsg.HasValue)
					{
						allInvolvedVsgIds.Add(connection.DestinationVsg.Value.ID);
					}

					if (connection.LevelMappings == null || !connection.LevelMappings.Any())
					{
						continue;
					}

					foreach (LevelMapping connectionLevelMapping in connection.LevelMappings)
					{
						allInvolvedLevelNumbers.Add(connectionLevelMapping.Destination.Number);
					}
				}

				IDictionary<Guid, VirtualSignalGroup> allInvolvedVsgs = _api.VirtualSignalGroups.Read(allInvolvedVsgIds);

				if (allInvolvedVsgIds.Count != allInvolvedVsgs.Count)
				{
					throw new InvalidOperationException("One or more Virtual Signal Groups involved in the connections could not be found.");
				}

				ORFilterElement<Level> filter = new(allInvolvedLevelNumbers.Select(number => LevelExposers.Number.Equal(number)).ToArray());
				List<Level> allInvolvedLevels = _api.Levels.Read(filter).ToList();

				foreach (KeyValuePair<OrchestrationEventConfiguration, List<Connection>> keyValuePair in disconnectsPerEvent)
				{
					Guid eventId = keyValuePair.Key.ID;

					foreach (Connection connection in keyValuePair.Value)
					{
						if (!connection.HasDestination())
						{
							continue;
						}

						VirtualSignalGroup dstVirtualSignalGroup = allInvolvedVsgs[connection.DestinationVsg.Value.ID];
						virtualSignalGroupsToUnlock.Add(dstVirtualSignalGroup);

						ICollection<ApiObjectReference<Level>> levels = null;

						if (connection.LevelMappings != null && connection.LevelMappings.Any())
						{
							levels = allInvolvedLevels
								.Select(level => level.Reference)
								.Distinct()
								.ToList();
						}

						requests.Add(new VsgDisconnectRequestWithMetadata(dstVirtualSignalGroup, levels)
						{
							MetaData = eventId.ToString(),
						});
					}
				}

				// Perform disconnects
				var takeHelper = _api.GetConnectionHandler();

				var results = takeHelper.Disconnect(
					requests,
					performanceTracker,
					new() { WaitForCompletion = true, Timeout = _settings.Timeout, BypassLockValidation = true });

				// Unlock all involved VSGs after disconnect
				_api.VirtualSignalGroups.UnlockVirtualSignalGroups(virtualSignalGroupsToUnlock);

				return results;
			}
		}

		private ICollection<VsgConnectionResult> ExecuteAndReturnConnectResults(Dictionary<OrchestrationEventConfiguration, List<Connection>> connectionsPerEvent, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				if (!connectionsPerEvent.Any())
				{
					return new List<VsgConnectionResult>();
				}

				List<VsgConnectionRequest> requests = [];
				List<VirtualSignalGroupLockRequest> lockRequests = [];

				HashSet<Guid> allInvolvedVsgIds = [];
				HashSet<int> allInvolvedLevelNumbers = [];
				foreach (Connection connection in connectionsPerEvent.SelectMany(kv => kv.Value))
				{
					if (connection.SourceVsg.HasValue)
					{
						allInvolvedVsgIds.Add(connection.SourceVsg.Value.ID);
					}

					if (connection.DestinationVsg.HasValue)
					{
						allInvolvedVsgIds.Add(connection.DestinationVsg.Value.ID);
					}

					if (connection.LevelMappings == null || !connection.LevelMappings.Any())
					{
						continue;
					}

					foreach (LevelMapping connectionLevelMapping in connection.LevelMappings)
					{
						allInvolvedLevelNumbers.Add(connectionLevelMapping.Source.Number);
						allInvolvedLevelNumbers.Add(connectionLevelMapping.Destination.Number);
					}
				}

				IDictionary<Guid, VirtualSignalGroup> allInvolvedVsgs = _api.VirtualSignalGroups.Read(allInvolvedVsgIds);

				if (allInvolvedVsgIds.Count != allInvolvedVsgs.Count)
				{
					throw new InvalidOperationException("One or more Virtual Signal Groups involved in the connections could not be found.");
				}

				ORFilterElement<Level> filter = new(allInvolvedLevelNumbers.Select(number => LevelExposers.Number.Equal(number)).ToArray());
				List<Level> allInvolvedLevels = _api.Levels.Read(filter).ToList();

				foreach (KeyValuePair<OrchestrationEventConfiguration, List<Connection>> keyValuePair in connectionsPerEvent)
				{
					OrchestrationEventConfiguration eventConfig = keyValuePair.Key;
					Guid eventId = eventConfig.ID;

					OrchestrationJobInfo jobInfo = eventConfig.GetJobInfo(_api)
						?? throw new InvalidOperationException($"No job info found for orchestration event with ID '{eventConfig.ID}'.");

					foreach (Connection connection in keyValuePair.Value)
					{
						if (!connection.HasDestination())
						{
							continue;
						}

						VirtualSignalGroup dstVirtualSignalGroup = allInvolvedVsgs[connection.DestinationVsg.Value.ID];

						lockRequests.Add(new VirtualSignalGroupLockRequest(
							dstVirtualSignalGroup,
							"Orchestration Engine",
							$"Locked for job: {jobInfo.JobReference}",
							$"{jobInfo.JobReference}",
							eventConfig.EventTime));

						if (!connection.HasSource())
						{
							continue;
						}

						VirtualSignalGroup srcVirtualSignalGroup = allInvolvedVsgs[connection.SourceVsg.Value.ID];

						ICollection<Take.LevelMapping> levelMappings = null;

						if (connection.LevelMappings != null && connection.LevelMappings.Any())
						{
							levelMappings = connection.LevelMappings
								.Select(map => new Take.LevelMapping(
									allInvolvedLevels.FirstOrDefault(level => level.Number == map.Source.Number),
									allInvolvedLevels.FirstOrDefault(level => level.Number == map.Destination.Number)))
								.ToList();
						}

						requests.Add(new VsgConnectionRequestWithMetaData(srcVirtualSignalGroup, dstVirtualSignalGroup, levelMappings)
						{
							MetaData = eventId.ToString(),
						});
					}
				}

				// Lock all involved VSGs before connecting
				_api.VirtualSignalGroups.LockVirtualSignalGroups(lockRequests);

				// Perform connections
				var takeHelper = _api.GetConnectionHandler();

				return takeHelper.Take(
					requests,
					performanceTracker,
					new() { WaitForCompletion = true, Timeout = _settings.Timeout, BypassLockValidation = true });
			}
		}

		private void ExecuteNodesConfiguration(OrchestrationEventConfiguration orchestrationEventConfiguration, TaskScheduler taskScheduler, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				ConcurrentHashSet<string> errors = new();
				List<Task> nodeOrchestrationTasks = [];

				foreach (NodeConfiguration nodeConfiguration in orchestrationEventConfiguration.Configuration.NodeConfigurations)
				{
					if (String.IsNullOrEmpty(nodeConfiguration.OrchestrationScriptName))
					{
						continue;
					}

					Task nodeOrchestrationTask = Task.Factory.StartNew(
						() =>
						{
							IEnumerable<OrchestrationScriptArgument> addedInputParams = new OrchestrationScriptInternalInput(orchestrationEventConfiguration.ID, OrchestrationLevel.Node).ToMetadataArguments();

							OrchestrationScriptResult nodeScriptResult = ExecuteOrchestrationScript(
								nodeConfiguration.OrchestrationScriptName,
								nodeConfiguration.OrchestrationScriptArguments.Union(addedInputParams),
								nodeConfiguration.Profile,
								performanceTracker);

							if (!nodeScriptResult.HadError)
							{
								return;
							}

							errors.TryAdd($"\nError during orchestration for node {nodeConfiguration.NodeId}: {String.Join("\n", nodeScriptResult.ErrorMessages)}");
							orchestrationEventConfiguration.InternalSetState(EventState.Failed);
						},
						CancellationToken.None,
						TaskCreationOptions.None,
						taskScheduler);

					nodeOrchestrationTasks.Add(nodeOrchestrationTask);
				}

				Task.WaitAll(nodeOrchestrationTasks.ToArray());

				if (orchestrationEventConfiguration.EventState == EventState.Failed)
				{
					orchestrationEventConfiguration.FailureInfo += String.Join("\n", errors);
				}
			}
		}

		private void ExecuteGlobalConfiguration(OrchestrationEventConfiguration orchestrationEventConfiguration, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				IEnumerable<OrchestrationScriptArgument> addedInputParams = new OrchestrationScriptInternalInput(orchestrationEventConfiguration.ID, OrchestrationLevel.Global).ToMetadataArguments();

				OrchestrationScriptResult globalScriptResult = ExecuteOrchestrationScript(
					orchestrationEventConfiguration.GlobalOrchestrationScript,
					orchestrationEventConfiguration.GlobalOrchestrationScriptArguments.Union(addedInputParams),
					orchestrationEventConfiguration.Profile,
					performanceTracker);

				if (globalScriptResult.HadError)
				{
					orchestrationEventConfiguration.FailureInfo += $"Error during global orchestration: {String.Join("\n", globalScriptResult.ErrorMessages)}";
					orchestrationEventConfiguration.InternalSetState(EventState.Failed);
				}
			}
		}

		private OrchestrationScriptResult ExecuteOrchestrationScript(string scriptName, IEnumerable<OrchestrationScriptArgument> arguments, OrchestrationProfile profile, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				return ExecuteOrchestrationScript(_api.Connection, scriptName, arguments, profile);
			}
		}

		internal static OrchestrationScriptResult ExecuteOrchestrationScript(IConnection connection, string scriptName, IEnumerable<OrchestrationScriptArgument> arguments, OrchestrationProfile profile)
		{
			IDms dms = connection.GetDms();
			IDmsAutomationScript script = dms.GetScript(scriptName);
			IEnumerable<OrchestrationScriptArgument> orchestrationScriptArguments = arguments.ToList();

			List<DmsAutomationScriptParamValue> scriptParams = [];
			OrchestrationScriptResult scriptParamResult = PrepareScriptParams(dms, script, orchestrationScriptArguments, scriptParams, profile);
			if (scriptParamResult.HadError)
			{
				return scriptParamResult;
			}

			List<DmsAutomationScriptDummyValue> scriptDummies = [];
			OrchestrationScriptResult scriptDummyResult = PrepareScriptDummies(dms, script, orchestrationScriptArguments, scriptDummies);
			if (scriptDummyResult.HadError)
			{
				return scriptDummyResult;
			}

			ExecuteScriptResponseMessage result;
			string[] errorMessages;
			if (OrchestrationScriptInfoHelper.IsOrchestrationScript(script))
			{
				OrchestrationScriptInput input = new(
					profile.Values.ToDictionary(value => value.Name, value => value.Value.Type == ParameterValue.ValueType.Double ? (object)value.Value.DoubleValue : value.Value.StringValue),
					profile.Instance);

				foreach (OrchestrationScriptArgument orchestrationScriptArgument in orchestrationScriptArguments.Where(arg => arg.Type == OrchestrationScriptArgumentType.Metadata))
				{
					input.Metadata.Add(orchestrationScriptArgument.Name, orchestrationScriptArgument.Value);
				}

				result = OrchestrationAutomationHelper.TryExecuteOrchestrationScript(connection, scriptName, scriptParams, scriptDummies, input, out errorMessages);
			}
			else
			{
				result = OrchestrationAutomationHelper.TryExecuteScript(connection, scriptName, scriptParams, scriptDummies, out errorMessages);
			}

			OrchestrationScriptResult scriptResult = new OrchestrationScriptResult
			{
				ErrorMessages = errorMessages,
				HadError = result.HadError || errorMessages.Any(),
			};

			if (result.ScriptOutput.TryGetValue(OrchestrationScriptConstants.ScriptOutputRequestScriptInfoKey, out string orchestrationOutputString))
			{
				OrchestrationScriptOutput orchestrationOutput = JsonConvert.DeserializeObject<OrchestrationScriptOutput>(orchestrationOutputString);
				scriptResult.ServiceId = String.Join($"/", orchestrationOutput.OrchestrationServiceAgentId, orchestrationOutput.OrchestrationServiceId);
			}

			return scriptResult;
		}

		private static OrchestrationScriptResult PrepareScriptParams(
			IDms dms,
			IDmsAutomationScript script,
			IEnumerable<OrchestrationScriptArgument> orchestrationScriptArguments,
			List<DmsAutomationScriptParamValue> scriptParams,
			OrchestrationProfile profile)
		{
			ProfileHelper profileHelper = new ProfileHelper(dms.Communication.SendMessages);

			Lazy<ProfileInstance> profileInstance = new(() =>
			{
				ProfileInstance instance = profileHelper.ProfileInstances.Read(ProfileInstanceExposers.Name.Equal(profile.Instance)).FirstOrDefault();
				if (instance == null)
				{
					throw new InvalidOperationException($"No profile instance found with name '{profile.Instance}'");
				}

				return instance;
			});

			foreach (IDmsAutomationScriptParameter requiredParameter in script.Parameters)
			{
				OrchestrationScriptArgument matchingArgument = orchestrationScriptArguments.FirstOrDefault(arg => arg.Name == requiredParameter.Description && arg.Type == OrchestrationScriptArgumentType.Parameter);
				if (matchingArgument != null)
				{
					scriptParams.Add(new DmsAutomationScriptParamValue(matchingArgument.Name, matchingArgument.Value));
					continue;
				}

				var profileParameter = profile.Values.FirstOrDefault(value => value.Name == requiredParameter.Description);
				if (profileParameter != null)
				{
					scriptParams.Add(new DmsAutomationScriptParamValue(profileParameter.Name, GetProfileParameterValue(profileParameter.Value).ToString()));
					continue;
				}

				var profileInstanceParameter = profileInstance.Value.Values.FirstOrDefault(value => value.Parameter.Name == requiredParameter.Description);
				if (profileInstanceParameter != null)
				{
					scriptParams.Add(new DmsAutomationScriptParamValue(profileInstanceParameter.Parameter.Name, GetProfileParameterValue(profileInstanceParameter.Value).ToString()));
					continue;
				}

				return new OrchestrationScriptResult
				{
					ErrorMessages = [$"Missing required script parameter: {requiredParameter.Description}"],
					HadError = true,
				};
			}

			return new OrchestrationScriptResult
			{
				HadError = false,
			};
		}

		private static object GetProfileParameterValue(ParameterValue value)
		{
			if (value.Type == ParameterValue.ValueType.Double)
			{
				return value.DoubleValue;
			}
			else
			{
				return value.StringValue;
			}
		}

		private static OrchestrationScriptResult PrepareScriptDummies(
			IDms dms,
			IDmsAutomationScript script,
			IEnumerable<OrchestrationScriptArgument> orchestrationScriptArguments,
			List<DmsAutomationScriptDummyValue> scriptDummies)
		{
			foreach (IDmsAutomationScriptDummy requiredDummy in script.Dummies)
			{
				OrchestrationScriptArgument matchingArgument = orchestrationScriptArguments.FirstOrDefault(arg => arg.Name == requiredDummy.Description && arg.Type == OrchestrationScriptArgumentType.Element);

				if (matchingArgument == null)
				{
					return new OrchestrationScriptResult
					{
						ErrorMessages = [$"Missing required script dummy: {requiredDummy.Description}"],
						HadError = true,
					};
				}

				if (matchingArgument.Value.Contains("/")) // By ID
				{
					string[] splitId = matchingArgument.Value.Split('/');
					int dmaId = Convert.ToInt32(splitId[0]);
					int elementId = Convert.ToInt32(splitId[1]);
					scriptDummies.Add(new DmsAutomationScriptDummyValue(matchingArgument.Name, new DmsElementId(dmaId, elementId)));
				}
				else // By Name
				{
					IDmsElement element = dms.GetElement(matchingArgument.Value);
					scriptDummies.Add(new DmsAutomationScriptDummyValue(matchingArgument.Name, element.DmsElementId));
				}
			}

			return new OrchestrationScriptResult
			{
				HadError = false,
			};
		}
	}
}
