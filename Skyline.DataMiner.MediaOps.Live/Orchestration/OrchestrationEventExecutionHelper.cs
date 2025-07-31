namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
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
					if (eventConfigurationsWithConnection.IsStopEvent)
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

						requests.Add(new VsgDisconnectRequest(dstVirtualSignalGroup, allInvolvedLevels.Select(level => new ApiObjectReference<Level>(level.ID)).ToHashSet())
						{
							MetaData = eventId.ToString(),
						});
					}
				}

				TakeHelper takeHelper = new TakeHelper(_api);
				takeHelper.EnableWaitForCompletion(_settings.Timeout);

				takeHelper.Disconnect(requests, performanceTracker.Collector);
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

				takeHelper.Take(requests, performanceTracker.Collector);
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
							if (TryExecuteOrchestrationScript(
									nodeConfiguration.OrchestrationScriptName,
									nodeConfiguration.OrchestrationScriptArguments,
									performanceTracker,
									out string[] errorMessages))
							{
								return;
							}

							errors.TryAdd($"\nError during orchestration for node {nodeConfiguration.NodeId}: {String.Join("\n", errorMessages)}");
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
				if (!TryExecuteOrchestrationScript(
						orchestrationEventConfiguration.GlobalOrchestrationScript,
						CombineOrchestrationScriptInputs(orchestrationEventConfiguration.GlobalOrchestrationScriptArguments, orchestrationEventConfiguration.Profile.Values),
						performanceTracker,
						out string[] errorMessages))
				{
					orchestrationEventConfiguration.FailureInfo += $"Error during global orchestration: {String.Join("\n", errorMessages)}";
					orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
				}
			}
		}

		private IList<OrchestrationScriptArgument> CombineOrchestrationScriptInputs(IList<OrchestrationScriptArgument> arguments, IList<OrchestrationProfileValue> profileValues)
		{
			List<OrchestrationScriptArgument> results = arguments.ToList();

			foreach (OrchestrationProfileValue orchestrationProfileValue in profileValues)
			{
				if (results.All(value => value.Name != orchestrationProfileValue.Name))
				{
					string value = orchestrationProfileValue.Value.Type == ParameterValue.ValueType.Double
						? orchestrationProfileValue.Value.DoubleValue.ToString(CultureInfo.InvariantCulture)
						: orchestrationProfileValue.Value.StringValue;

					results.Add(new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, orchestrationProfileValue.Name, value));
				}
			}

			return results;
		}

		private bool TryExecuteOrchestrationScript(string scriptName, IEnumerable<OrchestrationScriptArgument> arguments, PerformanceTracker performanceTracker, out string[] errorMessages)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				IDms dms = _api.Connection.GetDms();
				IDmsAutomationScript script = dms.GetScript(scriptName);

				List<DmsAutomationScriptParamValue> scriptParams = [];
				IEnumerable<OrchestrationScriptArgument> orchestrationScriptArguments = arguments.ToList();

				foreach (IDmsAutomationScriptParameter requiredParameter in script.Parameters)
				{
					OrchestrationScriptArgument matchingArgument = orchestrationScriptArguments.FirstOrDefault(arg => arg.Name == requiredParameter.Description);

					if (matchingArgument == null)
					{
						errorMessages = [$"Missing required script parameter: {requiredParameter.Description}"];
						return false;
					}

					scriptParams.Add(new DmsAutomationScriptParamValue(matchingArgument.Name, matchingArgument.Value));
				}

				List<DmsAutomationScriptDummyValue> scriptDummies = [];
				foreach (IDmsAutomationScriptDummy requiredDummy in script.Dummies)
				{
					OrchestrationScriptArgument matchingArgument = orchestrationScriptArguments.FirstOrDefault(arg => arg.Name == requiredDummy.Description);

					if (matchingArgument == null)
					{
						errorMessages = [$"Missing required script dummy: {requiredDummy.Description}"];
						return false;
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

				DmsAutomationScriptRunOptions scriptOptions = new()
				{
					ExtendedErrorInfo = true,
				};

				DmsAutomationScriptResult scriptResult = script.Execute(scriptParams, scriptDummies, scriptOptions);

				if (scriptResult.HadError)
				{
					errorMessages = scriptResult.ErrorMessages;
					return false;
				}

				errorMessages = [];
				return true;
			}
		}
	}
}
