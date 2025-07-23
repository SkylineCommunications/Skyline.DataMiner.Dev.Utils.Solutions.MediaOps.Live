namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.Linq;
	using System.Threading.Tasks;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ToolsSpace.Collections;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;

	using Connection = Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration.Connection;
	using Level = Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement.Level;
	using LevelMapping = Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration.LevelMapping;
	using ParameterValue = Skyline.DataMiner.Net.Profiles.ParameterValue;

	internal class OrchestrationEventExecutionHelper
	{
		private readonly MediaOpsLiveApi _api;

		internal OrchestrationEventExecutionHelper(MediaOpsLiveApi api)
		{
			_api = api;
		}

		internal void ExecuteEventsNow(IEnumerable<Guid> orchestrationIds, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				List<OrchestrationEventConfiguration> orchestrationEvents = _api.Orchestration.GetEventConfigurationsById(orchestrationIds, performanceTracker).ToList();

				ExecuteEventsNow(orchestrationEvents, performanceTracker);
			}
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
							ExecuteEventConfigurationScripts(orchestrationEventConfiguration, performanceTracker);
							ProcessConnections(new List<OrchestrationEventConfiguration> { orchestrationEventConfiguration }, performanceTracker);
						},
						TaskCreationOptions.LongRunning);

					tasks.Add(nodeOrchestrationTask);
				}

				ProcessConnections(eventConfigurations.Where(e => !e.HasScripts()), performanceTracker);

				Task.WaitAll(tasks.ToArray());

				_api.Orchestration.SaveEventConfigurations(eventConfigurations, performanceTracker);
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

					eventConfigurationsWithConnection.InternalSetState(SlcOrchestrationIds.Enums.EventState.Completed);
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

				TakeHelper takeHelper = new(_api);
				takeHelper.EnableWaitForCompletion(new ConnectionMonitor(_api),TimeSpan.FromSeconds(5));

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

				TakeHelper takeHelper = new(_api);
				takeHelper.EnableWaitForCompletion(new ConnectionMonitor(_api),TimeSpan.FromSeconds(5));

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

				takeHelper.Take(requests, performanceTracker.Collector);
			}
		}

		private void ExecuteNodesConfiguration(OrchestrationEventConfiguration orchestrationEventConfiguration, PerformanceTracker performanceTracker)
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
				if (!TryExecuteOrchestrationScript(
						orchestrationEventConfiguration.GlobalOrchestrationScript,
						CombineOrchestrationScriptInputs(orchestrationEventConfiguration.GlobalOrchestrationScriptArguments, orchestrationEventConfiguration.Profile.Values),
						performanceTracker,
						out string[] errorMessages))
				{
					orchestrationEventConfiguration.FailureInfo += $"Error during global orchestration: {String.Join("\n", errorMessages)}";
					orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Failed);
					return;
				}

				orchestrationEventConfiguration.InternalSetState(SlcOrchestrationIds.Enums.EventState.Completed);
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
				List<DmsAutomationScriptParamValue> scriptParams = arguments
					.Where(arg => arg.Type == OrchestrationScriptArgumentType.Parameter)
					.Select(arg => new DmsAutomationScriptParamValue(arg.Name, arg.Value))
					.ToList();

				List<DmsAutomationScriptDummyValue> scriptDummies = new List<DmsAutomationScriptDummyValue>();
				foreach (OrchestrationScriptArgument dummyArgument in arguments.Where(arg => arg.Type == OrchestrationScriptArgumentType.Element))
				{
					if (dummyArgument.Value.Contains("/")) // By ID
					{
						string[] splittedId = dummyArgument.Value.Split('/');
						int dmaId = Convert.ToInt32(splittedId[0]);
						int elementId = Convert.ToInt32(splittedId[1]);
						scriptDummies.Add(new DmsAutomationScriptDummyValue(dummyArgument.Name, new DmsElementId(dmaId, elementId)));
					}
					else // By Name
					{
						IDmsElement element = dms.GetElement(dummyArgument.Value);
						scriptDummies.Add(new DmsAutomationScriptDummyValue(dummyArgument.Name, element.DmsElementId));
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
