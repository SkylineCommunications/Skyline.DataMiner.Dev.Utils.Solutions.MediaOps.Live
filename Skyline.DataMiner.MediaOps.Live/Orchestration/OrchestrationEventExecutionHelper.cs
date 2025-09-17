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
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Enums;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
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

				List<SlcOrchestrationIds.Enums.EventState> statesToDiscard =
				[
					SlcOrchestrationIds.Enums.EventState.Failed,
					SlcOrchestrationIds.Enums.EventState.Completed,
					SlcOrchestrationIds.Enums.EventState.Configuring,
				];

				ExecuteEvents(events.Where(e => !statesToDiscard.Contains(e.EventState.Value)), performanceTracker);
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
					orchestrationEvent.InternalSetState(SlcOrchestrationIds.Enums.EventState.Configuring);
					orchestrationEvent.ActualStartTime = DateTimeOffset.UtcNow;
				}

				_api.Orchestration.SaveEventConfigurations(eventConfigurations, performanceTracker);

				List<Task> tasks = [];
				foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in eventConfigurations.Where(e => e.HasScripts()))
				{
					Task nodeOrchestrationTask = Task.Factory.StartNew(
						() =>
						{
							ExecuteEventConfigurationScripts(orchestrationEventConfiguration, taskScheduler, performanceTracker);
						},
						CancellationToken.None,
						TaskCreationOptions.None,
						taskScheduler);

					tasks.Add(nodeOrchestrationTask);
				}

				ProcessConnections(eventConfigurations, performanceTracker);

				Task.WaitAll(tasks.ToArray());

				foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in eventConfigurations)
				{
					if (orchestrationEventConfiguration.EventState != SlcOrchestrationIds.Enums.EventState.Failed)
					{
						orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Completed);
					}
				}

				_api.Orchestration.SaveEventConfigurations(eventConfigurations, performanceTracker);
			}
		}

		private void ExecuteEventConfigurationScripts(OrchestrationEventConfiguration orchestrationEventConfiguration, TaskScheduler taskScheduler, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				if (!String.IsNullOrEmpty(orchestrationEventConfiguration.GlobalOrchestrationScript))
				{
					ExecuteGlobalConfiguration(orchestrationEventConfiguration, performanceTracker);
					return;
				}

				ExecuteNodesConfiguration(orchestrationEventConfiguration, taskScheduler, performanceTracker);
			}
		}

		private void ProcessConnections(IEnumerable<OrchestrationEventConfiguration> orchestrationEventConfigurations, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				IEnumerable<OrchestrationEventConfiguration> eventConfigurations = orchestrationEventConfigurations.ToList();
				List<OrchestrationEventConfiguration> eventConfigurationsWithConnections = eventConfigurations
					.Where(orchestrationEventConfiguration => orchestrationEventConfiguration?.Configuration?.Connections != null && orchestrationEventConfiguration.Configuration.Connections.Any()).ToList();

				Dictionary<Guid, List<Connection>> connectionsToConfigureByEvent = [];
				Dictionary<Guid, List<Connection>> disconnectsToConfigureByEvent = [];

				foreach (OrchestrationEventConfiguration eventConfigurationsWithConnection in eventConfigurationsWithConnections)
				{
					if (eventConfigurationsWithConnection.IsDisconnectEvent)
					{
						disconnectsToConfigureByEvent.Add(eventConfigurationsWithConnection.ID, eventConfigurationsWithConnection.Configuration.Connections.ToList());
					}
					else
					{
						connectionsToConfigureByEvent.Add(eventConfigurationsWithConnection.ID, eventConfigurationsWithConnection.Configuration.Connections.ToList());
					}
				}

				try
				{
					ExecuteConnections(connectionsToConfigureByEvent, performanceTracker);
				}
				catch (ConnectFailedException e)
				{
					IEnumerable<string> eventsForFailedRequests = e.FailedRequests.Select(fail => Convert.ToString(fail.MetaData));
					foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in orchestrationEventConfigurations.Where(eventConfig => eventsForFailedRequests.Contains(eventConfig.ID.ToString())))
					{
						orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
						orchestrationEventConfiguration.FailureInfo += $"\n{e.Message}";
					}
				}
				catch (Exception e)
				{
					foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in orchestrationEventConfigurations)
					{
						orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
						orchestrationEventConfiguration.FailureInfo += $"\n{e.Message}";
					}
				}

				try
				{
					ExecuteDisconnects(disconnectsToConfigureByEvent, performanceTracker);
				}
				catch (DisconnectFailedException e)
				{
					IEnumerable<string> eventsForFailedRequests = e.FailedRequests.Select(fail => Convert.ToString(fail.MetaData));
					foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in orchestrationEventConfigurations.Where(eventConfig => eventsForFailedRequests.Contains(eventConfig.ID.ToString())))
					{
						orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
						orchestrationEventConfiguration.FailureInfo += $"\n{e.Message}";
					}
				}
				catch (Exception e)
				{
					foreach (OrchestrationEventConfiguration orchestrationEventConfiguration in orchestrationEventConfigurations)
					{
						orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
						orchestrationEventConfiguration.FailureInfo += $"\n{e.Message}";
					}
				}

				foreach (OrchestrationEventConfiguration eventConfiguration in eventConfigurations.Where(e => e.ActualStartTime != null))
				{
					eventConfiguration.OrchestrationDuration = DateTimeOffset.UtcNow - eventConfiguration.ActualStartTime;
				}
			}
		}

		private void ExecuteDisconnects(Dictionary<Guid, List<Connection>> disconnectsPerId, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				if (!disconnectsPerId.Any())
				{
					return;
				}

				List<VsgDisconnectRequest> requests = [];

				HashSet<Guid> allInvolvedVsgIds = [];
				HashSet<int> allInvolvedLevelNumbers = [];
				foreach (Connection connection in disconnectsPerId.SelectMany(kv => kv.Value))
				{
					allInvolvedVsgIds.Add(connection.DestinationVsg.Value.ID);

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

				ORFilterElement<DomInstance> filter = new(allInvolvedLevelNumbers.Select(number => _api.Levels.CreateFilter(nameof(Level.Number), Comparer.Equals, number)).ToArray());
				List<Level> allInvolvedLevels = _api.Levels.Read(filter).ToList();

				foreach (KeyValuePair<Guid, List<Connection>> keyValuePair in disconnectsPerId)
				{
					Guid eventId = keyValuePair.Key;
					foreach (Connection connection in keyValuePair.Value)
					{
						VirtualSignalGroup dstVirtualSignalGroup = allInvolvedVsgs[connection.DestinationVsg.Value.ID];

						if (connection.LevelMappings == null || !connection.LevelMappings.Any())
						{
							requests.Add(new VsgDisconnectRequest(dstVirtualSignalGroup)
							{
								MetaData = eventId.ToString(),
							});
							continue;
						}

						requests.Add(new VsgDisconnectRequest(dstVirtualSignalGroup, Enumerable.ToHashSet(allInvolvedLevels.Select(level => new ApiObjectReference<Level>(level.ID))))
						{
							MetaData = eventId.ToString(),
						});
					}
				}

				TakeHelper takeHelper = new TakeHelper(_api);
				takeHelper.EnableWaitForCompletion(_settings.Timeout);

				takeHelper.Disconnect(requests, performanceTracker);
			}
		}

		private void ExecuteConnections(Dictionary<Guid, List<Connection>> connectionsPerEventId, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				if (!connectionsPerEventId.Any())
				{
					return;
				}

				List<VsgConnectionRequest> requests = [];

				HashSet<Guid> allInvolvedVsgIds = [];
				HashSet<int> allInvolvedLevelNumbers = [];
				foreach (Connection connection in connectionsPerEventId.SelectMany(kv => kv.Value))
				{
					allInvolvedVsgIds.Add(connection.SourceVsg.Value.ID);
					allInvolvedVsgIds.Add(connection.DestinationVsg.Value.ID);

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

				ORFilterElement<DomInstance> filter = new(allInvolvedLevelNumbers.Select(number => _api.Levels.CreateFilter(nameof(Level.Number), Comparer.Equals, number)).ToArray());
				List<Level> allInvolvedLevels = _api.Levels.Read(filter).ToList();

				foreach (KeyValuePair<Guid, List<Connection>> keyValuePair in connectionsPerEventId)
				{
					Guid eventId = keyValuePair.Key;
					foreach (Connection connection in keyValuePair.Value)
					{
						VirtualSignalGroup srcVirtualSignalGroup = allInvolvedVsgs[connection.SourceVsg.Value.ID];
						VirtualSignalGroup dstVirtualSignalGroup = allInvolvedVsgs[connection.DestinationVsg.Value.ID];

						if (connection.LevelMappings == null || !connection.LevelMappings.Any())
						{
							requests.Add(new VsgConnectionRequest(srcVirtualSignalGroup, dstVirtualSignalGroup)
							{
								MetaData = eventId.ToString(),
							});
							continue;
						}

						List<Take.LevelMapping> levelMappings = connection.LevelMappings.Select(map => new Take.LevelMapping(
							allInvolvedLevels.FirstOrDefault(level => level.Number == map.Source.Number),
							allInvolvedLevels.FirstOrDefault(level => level.Number == map.Destination.Number))).ToList();

						requests.Add(new VsgConnectionRequest(srcVirtualSignalGroup, dstVirtualSignalGroup, levelMappings)
						{
							MetaData = eventId.ToString(),
						});
					}
				}

				TakeHelper takeHelper = new TakeHelper(_api);
				takeHelper.EnableWaitForCompletion(_settings.Timeout);

				takeHelper.Take(requests, performanceTracker);
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
							orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
						},
						CancellationToken.None,
						TaskCreationOptions.None,
						taskScheduler);

					nodeOrchestrationTasks.Add(nodeOrchestrationTask);
				}

				Task.WaitAll(nodeOrchestrationTasks.ToArray());

				if (orchestrationEventConfiguration.EventState == SlcOrchestrationIds.Enums.EventState.Failed)
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
					orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
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
			ProfileHelper profileHelper = new ProfileHelper(connection.HandleMessages);

			IDmsAutomationScript script = dms.GetScript(scriptName);
			IEnumerable<OrchestrationScriptArgument> orchestrationScriptArguments = arguments.ToList();

			Lazy<ProfileInstance> profileInstance = new(() =>
			{
				ProfileInstance instance = profileHelper.ProfileInstances.Read(ProfileInstanceExposers.Name.Equal(profile.Instance)).FirstOrDefault();
				if (instance == null)
				{
					throw new InvalidOperationException($"No profile instance found with name '{profile.Instance}'");
				}

				return instance;
			});

			List<DmsAutomationScriptParamValue> scriptParams = [];
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
					scriptParams.Add(new DmsAutomationScriptParamValue(profileParameter.Name, profileParameter.Value.ToString()));
					continue;
				}

				var profileInstanceParameter = profileInstance.Value.Values.FirstOrDefault(value => value.Parameter.Name == requiredParameter.Description);
				if (profileInstanceParameter != null)
				{
					scriptParams.Add(new DmsAutomationScriptParamValue(profileInstanceParameter.Parameter.Name, profileInstanceParameter.Value.ToString()));
					continue;
				}

				return new OrchestrationScriptResult
				{
					ErrorMessages = [$"Missing required script parameter: {requiredParameter.Description}"],
					HadError = true,
				};
			}

			List<DmsAutomationScriptDummyValue> scriptDummies = [];
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
					string[] splittedId = matchingArgument.Value.Split('/');
					int dmaId = Convert.ToInt32(splittedId[0]);
					int elementId = Convert.ToInt32(splittedId[1]);
					scriptDummies.Add(new DmsAutomationScriptDummyValue(matchingArgument.Name, new DmsElementId(dmaId, elementId)));
				}
				else // By Name
				{
					IDmsElement element = dms.GetElement(matchingArgument.Value);
					scriptDummies.Add(new DmsAutomationScriptDummyValue(matchingArgument.Name, element.DmsElementId));
				}
			}

			OrchestrationScriptInput input = new(
				profile.Values.ToDictionary(
					value => value.Name,
					value => value.Value.Type == ParameterValue.ValueType.Double ? (object)value.Value.DoubleValue : value.Value.StringValue),
				profile.Instance);

			foreach (OrchestrationScriptArgument orchestrationScriptArgument in orchestrationScriptArguments.Where(arg => arg.Type == OrchestrationScriptArgumentType.Metadata))
			{
				input.Metadata.Add(orchestrationScriptArgument.Name, orchestrationScriptArgument.Value);
			}

			var result = AutomationHelper.TryExecuteOrchestrationScript(connection, scriptName, scriptParams, scriptDummies, input, out string[] errorMessages);
			OrchestrationScriptResult scriptResult = new OrchestrationScriptResult
			{
				ErrorMessages = errorMessages,
				HadError = result.HadError || errorMessages.Any(),
			};

			if (result.ScriptOutput.TryGetValue(OrchestrationScript.ScriptOutputRequestScriptInfoKey, out string orchestrationOutputString))
			{
				OrchestrationScriptOutput orchestrationOutput = JsonConvert.DeserializeObject<OrchestrationScriptOutput>(orchestrationOutputString);
				scriptResult.ServiceId = String.Join($"/", orchestrationOutput.OrchestrationServiceAgentId, orchestrationOutput.OrchestrationServiceId);
			}

			return scriptResult;
		}
	}
}
