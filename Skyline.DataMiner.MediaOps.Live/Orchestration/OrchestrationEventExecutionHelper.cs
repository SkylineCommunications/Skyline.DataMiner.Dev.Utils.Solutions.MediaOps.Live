namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.ExceptionServices;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Net.ToolsSpace.Collections;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Plan;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Tools;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	using Connection = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration.Connection;
	using Level = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement.Level;
	using LevelMapping = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration.LevelMapping;
	using ParameterValue = Skyline.DataMiner.Net.Profiles.ParameterValue;

	internal class OrchestrationEventExecutionHelper
	{
		private readonly MediaOpsLiveApi _api;
		private readonly IMediaOpsPlanHelper _planHelper;
		private readonly OrchestrationSettings _settings;

		internal OrchestrationEventExecutionHelper(MediaOpsLiveApi api, IMediaOpsPlanHelper planHelper, OrchestrationSettings settings)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
			_planHelper = planHelper ?? throw new ArgumentNullException(nameof(planHelper));
			_settings = settings ?? new OrchestrationSettings();
		}

		internal void ExecuteEventsNow(IEnumerable<OrchestrationEventConfiguration> orchestrationEvents, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				// Only allow execution of events that are in Draft or Confirmed state.
				// Configuring events are already being executed, and Cancelled, Completed or Failed events should not be executed.
				var eventsToExecute = orchestrationEvents
					.Where(e => e.EventState == EventState.Draft || e.EventState == EventState.Confirmed)
					.ToList();

				if (!eventsToExecute.Any())
				{
					return;
				}

				ExecuteEventsAsync(eventsToExecute, performanceTracker).GetAwaiter().GetResult();
			}
		}

		private async Task ExecuteEventsAsync(List<OrchestrationEventConfiguration> eventConfigurations, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(OrchestrationEventExecutionHelper), nameof(ExecuteEventsAsync)))
			using (var taskScheduler = new MediaOpsTaskScheduler())
			using (var writeBuffer = new WriteBuffer<OrchestrationEvent>(new OrchestrationEventRepository(_api)))
			{
				var jobTasks = new List<Task>();

				foreach (var groupedByJob in eventConfigurations.GroupBy(x => x.JobInfoReference))
				{
					var jobTask = Task.Factory.StartNew(
						() => ExecuteJobEventsAsync(groupedByJob, taskScheduler, writeBuffer, performanceTracker),
						CancellationToken.None,
						TaskCreationOptions.None,
						taskScheduler).Unwrap();

					jobTasks.Add(jobTask);
				}

				await Task.WhenAll(jobTasks);
			}
		}

		private async Task ExecuteJobEventsAsync(IEnumerable<OrchestrationEventConfiguration> eventConfigurations, MediaOpsTaskScheduler taskScheduler, WriteBuffer<OrchestrationEvent> writeBuffer, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(OrchestrationEventExecutionHelper), nameof(ExecuteJobEventsAsync)))
			{
				var groupedAndSortedEvents = eventConfigurations
					.GroupBy(x => x.EventType)
					.OrderBy(x => x.Key, OrchestrationJob.EventTypeOrderComparer)
					.ToList();

				foreach (var jobEvents in groupedAndSortedEvents)
				{
					var jobEventTasks = new List<Task>();

					foreach (var jobEvent in jobEvents)
					{
						var jobEventTask = Task.Factory.StartNew(
							() => ExecuteJobEventAsync(jobEvent, taskScheduler, writeBuffer, performanceTracker),
							CancellationToken.None,
							TaskCreationOptions.None,
							taskScheduler).Unwrap();

						jobEventTasks.Add(jobEventTask);
					}

					await Task.WhenAll(jobEventTasks);
				}
			}
		}

		private async Task ExecuteJobEventAsync(OrchestrationEventConfiguration orchestrationEvent, MediaOpsTaskScheduler taskScheduler, WriteBuffer<OrchestrationEvent> writeBuffer, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(OrchestrationEventExecutionHelper), nameof(ExecuteJobEventAsync)))
			{
				SetConfiguringState(orchestrationEvent, writeBuffer);

				try
				{
					if (orchestrationEvent.HasGlobalOrchestrationScript)
					{
						ExecuteGlobalOrchestrationScriptEvent(orchestrationEvent, performanceTracker);
					}
					else if (orchestrationEvent.HasScripts)
					{
						await ExecuteNodeScriptsEventAsync(orchestrationEvent, taskScheduler, performanceTracker);
					}
					else if (orchestrationEvent.HasConnections)
					{
						await ExecuteConnectionsOnlyEventAsync(orchestrationEvent, performanceTracker);
					}
					else
					{
						// No scripts or connections, mark as completed right away
					}
				}
				catch (Exception ex)
				{
					orchestrationEvent.InternalSetState(EventState.Failed);
					orchestrationEvent.AppendFailureInfo($"An unexpected error occurred during orchestration: {ex.Message}");
				}
				finally
				{
					SaveOrchestrationResult(orchestrationEvent, writeBuffer, performanceTracker);
				}
			}
		}

		private void ExecuteGlobalOrchestrationScriptEvent(OrchestrationEventConfiguration orchestrationEventConfiguration, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				IEnumerable<OrchestrationScriptArgument> addedInputParams = new OrchestrationScriptInternalInput(orchestrationEventConfiguration.ID, OrchestrationLevel.Global).ToMetadataArguments();

				OrchestrationScriptResult globalScriptResult = ExecuteOrchestrationScript(
					orchestrationEventConfiguration.GlobalOrchestrationScript,
					orchestrationEventConfiguration.GlobalOrchestrationScriptArguments.Union(addedInputParams).ToList(),
					orchestrationEventConfiguration.Profile,
					performanceTracker);

				if (globalScriptResult.HadError)
				{
					orchestrationEventConfiguration.InternalSetState(EventState.Failed);
					orchestrationEventConfiguration.AppendFailureInfo($"Error during global orchestration: {String.Join("\n", globalScriptResult.ErrorMessages)}");
				}
			}
		}

		private async Task ExecuteNodeScriptsEventAsync(OrchestrationEventConfiguration orchestrationEvent, MediaOpsTaskScheduler taskScheduler, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(OrchestrationEventExecutionHelper), nameof(ExecuteNodeScriptsEventAsync)))
			{
				var tasks = new List<Task>();

				if (orchestrationEvent.HasConnections)
				{
					var connectionsTask = ExecuteConnectionsAsync(orchestrationEvent, performanceTracker);
					tasks.Add(connectionsTask);
				}

				var nodeScriptsTask = ExecuteNodesConfigurationAsync(orchestrationEvent, taskScheduler, performanceTracker);
				tasks.Add(nodeScriptsTask);

				await Task.WhenAll(tasks);
			}
		}

		private async Task ExecuteConnectionsOnlyEventAsync(OrchestrationEventConfiguration orchestrationEvent, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(OrchestrationEventExecutionHelper), nameof(ExecuteConnectionsOnlyEventAsync)))
			{
				await ExecuteConnectionsAsync(orchestrationEvent, performanceTracker);
			}
		}

		private void SetConfiguringState(OrchestrationEventConfiguration orchestrationEvent, WriteBuffer<OrchestrationEvent> writeBuffer)
		{
			orchestrationEvent.InternalSetState(EventState.Configuring);
			orchestrationEvent.ActualStartTime = DateTimeOffset.UtcNow;

			writeBuffer.Enqueue(orchestrationEvent);
		}

		private void SaveOrchestrationResult(OrchestrationEventConfiguration orchestrationEventConfiguration, WriteBuffer<OrchestrationEvent> writeBuffer, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				if (orchestrationEventConfiguration.ActualStartTime != null)
				{
					orchestrationEventConfiguration.OrchestrationDuration = DateTimeOffset.UtcNow - orchestrationEventConfiguration.ActualStartTime;
				}

				if (orchestrationEventConfiguration.EventState != EventState.Failed)
				{
					orchestrationEventConfiguration.InternalSetState(EventState.Completed);
				}

				orchestrationEventConfiguration.SendPlanJobStateUpdate(_planHelper);

				writeBuffer.Enqueue(orchestrationEventConfiguration);
			}
		}

		internal void ExecuteConnections(OrchestrationEventConfiguration orchestrationEvent, PerformanceTracker performanceTracker)
		{
			ExecuteConnectionsAsync(orchestrationEvent, performanceTracker).GetAwaiter().GetResult();
		}

		private async Task ExecuteConnectionsAsync(OrchestrationEventConfiguration orchestrationEvent, PerformanceTracker performanceTracker)
		{
			if (!orchestrationEvent.HasConnections)
			{
				return;
			}

			using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(OrchestrationEventExecutionHelper), nameof(ExecuteConnectionsAsync)))
			{
				try
				{
					var connections = orchestrationEvent.Configuration.Connections.ToList();

					if (orchestrationEvent.IsConnectEvent)
					{
						var results = await ExecuteConnectionsAndReturnResultsAsync(orchestrationEvent, connections, performanceTracker);

						foreach (var result in results.Where(r => !r.IsSuccessful))
						{
							var source = result.Request.Source.Name;
							var destination = result.Request.Destination.Name;

							orchestrationEvent.InternalSetState(EventState.Failed);
							orchestrationEvent.AppendFailureInfo($"Could not connect {source} to {destination}");
						}
					}

					if (orchestrationEvent.IsDisconnectEvent)
					{
						var results = await ExecuteDisconnectionsAndReturnResultsAsync(orchestrationEvent, connections, performanceTracker);

						foreach (var result in results.Where(r => !r.IsSuccessful))
						{
							var destination = result.Request.Destination.Name;

							orchestrationEvent.InternalSetState(EventState.Failed);
							orchestrationEvent.AppendFailureInfo($"Could not disconnect from {destination}");
						}
					}
				}
				catch (Exception e)
				{
					orchestrationEvent.InternalSetState(EventState.Failed);
					orchestrationEvent.AppendFailureInfo($"Error during connection operations: {e.Message}");
				}
			}
		}

		private async Task<ICollection<VsgDisconnectResult>> ExecuteDisconnectionsAndReturnResultsAsync(OrchestrationEventConfiguration orchestrationEvent, IList<Connection> disconnects, PerformanceTracker performanceTracker)
		{
			if (disconnects.Count == 0)
			{
				return [];
			}

			using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(OrchestrationEventExecutionHelper), nameof(ExecuteDisconnectionsAndReturnResultsAsync)))
			{
				List<VsgDisconnectRequest> requests = [];
				List<VirtualSignalGroup> virtualSignalGroupsToUnlock = [];

				HashSet<Guid> allInvolvedVsgIds = [];
				HashSet<int> allInvolvedLevelNumbers = [];

				foreach (Connection connection in disconnects)
				{
					if (connection.DestinationVsg.HasValue)
					{
						allInvolvedVsgIds.Add(connection.DestinationVsg.Value.ID);
					}

					if (connection.LevelMappings != null)
					{
						foreach (LevelMapping connectionLevelMapping in connection.LevelMappings)
						{
							allInvolvedLevelNumbers.Add(connectionLevelMapping.Destination.Number);
						}
					}
				}

				IDictionary<Guid, VirtualSignalGroup> allInvolvedVsgs = _api.VirtualSignalGroups.Read(allInvolvedVsgIds);

				if (allInvolvedVsgIds.Count != allInvolvedVsgs.Count)
				{
					throw new InvalidOperationException("One or more Virtual Signal Groups involved in the connections could not be found.");
				}

				ORFilterElement<Level> filter = new(allInvolvedLevelNumbers.Select(number => LevelExposers.Number.Equal(number)).ToArray());
				List<Level> allInvolvedLevels = _api.Levels.Read(filter).ToList();

				foreach (Connection connection in disconnects.Where(x => x.HasDestination()))
				{
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
						MetaData = orchestrationEvent,
						Timeout = _settings.Timeout,
					});
				}

				// Perform disconnects
				ICollection<VsgDisconnectResult> results = null;
				Exception disconnectException = null;

				try
				{
					var takeHelper = _api.GetConnectionHandler();
					results = await takeHelper.DisconnectAsync(
						requests,
						performanceTracker,
						new() { WaitForCompletion = true, BypassLockValidation = true });
				}
				catch (Exception ex)
				{
					disconnectException = ex;
				}

				try
				{
					// Unlock all involved VSGs after disconnect
					_api.VirtualSignalGroups.UnlockVirtualSignalGroups(virtualSignalGroupsToUnlock);
				}
				catch (Exception ex)
				{
					if (disconnectException != null)
					{
						throw new AggregateException("Disconnect and unlock both failed.", disconnectException, ex);
					}

					throw;
				}

				if (disconnectException != null)
				{
					ExceptionDispatchInfo.Capture(disconnectException).Throw();
				}

				// Return results
				return results;
			}
		}

		private async Task<ICollection<VsgConnectionResult>> ExecuteConnectionsAndReturnResultsAsync(OrchestrationEventConfiguration orchestrationEvent, IList<Connection> connections, PerformanceTracker performanceTracker)
		{
			if (connections.Count == 0)
			{
				return [];
			}

			using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(OrchestrationEventExecutionHelper), nameof(ExecuteConnectionsAndReturnResultsAsync)))
			{
				List<VsgConnectionRequest> requests = [];
				List<VirtualSignalGroupLockRequest> lockRequests = [];

				HashSet<Guid> allInvolvedVsgIds = [];
				HashSet<int> allInvolvedLevelNumbers = [];

				foreach (Connection connection in connections)
				{
					if (connection.SourceVsg.HasValue)
					{
						allInvolvedVsgIds.Add(connection.SourceVsg.Value.ID);
					}

					if (connection.DestinationVsg.HasValue)
					{
						allInvolvedVsgIds.Add(connection.DestinationVsg.Value.ID);
					}

					if (connection.LevelMappings != null)
					{
						foreach (LevelMapping connectionLevelMapping in connection.LevelMappings)
						{
							allInvolvedLevelNumbers.Add(connectionLevelMapping.Source.Number);
							allInvolvedLevelNumbers.Add(connectionLevelMapping.Destination.Number);
						}
					}
				}

				IDictionary<Guid, VirtualSignalGroup> allInvolvedVsgs = _api.VirtualSignalGroups.Read(allInvolvedVsgIds);

				if (allInvolvedVsgIds.Count != allInvolvedVsgs.Count)
				{
					throw new InvalidOperationException("One or more Virtual Signal Groups involved in the connections could not be found.");
				}

				ORFilterElement<Level> filter = new(allInvolvedLevelNumbers.Select(number => LevelExposers.Number.Equal(number)).ToArray());
				List<Level> allInvolvedLevels = _api.Levels.Read(filter).ToList();

				OrchestrationJobInfo jobInfo = orchestrationEvent.GetJobInfo(_api)
					?? throw new InvalidOperationException($"No job info found for orchestration event with ID '{orchestrationEvent.ID}'.");

				foreach (Connection connection in connections.Where(x => x.HasDestination()))
				{
					VirtualSignalGroup dstVirtualSignalGroup = allInvolvedVsgs[connection.DestinationVsg.Value.ID];

					lockRequests.Add(new VirtualSignalGroupLockRequest(
						dstVirtualSignalGroup,
						"Orchestration Engine",
						$"Locked for job: {jobInfo.JobReference}",
						$"{jobInfo.JobReference}",
						orchestrationEvent.EventTime));

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

					requests.Add(new VsgConnectionRequestWithMetadata(srcVirtualSignalGroup, dstVirtualSignalGroup, levelMappings)
					{
						MetaData = orchestrationEvent,
						Timeout = _settings.Timeout,
					});
				}

				// Lock all involved VSGs before connecting
				_api.VirtualSignalGroups.LockVirtualSignalGroups(lockRequests);

				// Perform connections
				var takeHelper = _api.GetConnectionHandler();

				var results = await takeHelper.TakeAsync(
					requests,
					performanceTracker,
					new() { WaitForCompletion = true, BypassLockValidation = true });

				// Return results
				return results;
			}
		}

		private async Task ExecuteNodesConfigurationAsync(OrchestrationEventConfiguration orchestrationEventConfiguration, TaskScheduler taskScheduler, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker, nameof(OrchestrationEventExecutionHelper), nameof(ExecuteNodesConfigurationAsync)))
			{
				ConcurrentHashSet<string> errors = new();

				try
				{
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
									nodeConfiguration.OrchestrationScriptArguments.Union(addedInputParams).ToList(),
									nodeConfiguration.Profile,
									performanceTracker);

								if (nodeScriptResult.HadError)
								{
									errors.TryAdd($"\nError during orchestration for node {nodeConfiguration.NodeId}: " +
										$"{String.Join("\n", nodeScriptResult.ErrorMessages)}");
								}
							},
							CancellationToken.None,
							TaskCreationOptions.None,
							taskScheduler);

						nodeOrchestrationTasks.Add(nodeOrchestrationTask);
					}

					await Task.WhenAll(nodeOrchestrationTasks);
				}
				finally
				{
					if (errors.Count > 0)
					{
						orchestrationEventConfiguration.InternalSetState(EventState.Failed);
						orchestrationEventConfiguration.AppendFailureInfo($"Errors occurred during node orchestration: {String.Join("\n", errors)}");
					}
				}
			}
		}

		private OrchestrationScriptResult ExecuteOrchestrationScript(string scriptName, ICollection<OrchestrationScriptArgument> arguments, OrchestrationProfile profile, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				return ExecuteOrchestrationScript(_api.Connection, scriptName, arguments, profile);
			}
		}

		internal static OrchestrationScriptResult ExecuteOrchestrationScript(IConnection connection, string scriptName, ICollection<OrchestrationScriptArgument> arguments, OrchestrationProfile profile)
		{
			try
			{
				IDms dms = connection.GetDms();
				IDmsAutomationScript script = dms.GetScript(scriptName);

				if (!OrchestrationScriptInfoHelper.IsValidOrchestrationScript(script))
				{
					throw new InvalidOperationException($"The script '{scriptName}' is not a valid orchestration script.");
				}

				var scriptParams = PrepareScriptParams(dms, script, arguments, profile);
				var scriptDummies = PrepareScriptDummies(dms, script, arguments);

				ExecuteScriptResponseMessage result;

				try
				{
					// First try to execute with the entry point
					var input = new OrchestrationScriptInput(
						profile.Values.ToDictionary(value => value.Name, value => value.Value.Type == ParameterValue.ValueType.Double ? (object)value.Value.DoubleValue : value.Value.StringValue),
						profile.Instance);

					foreach (OrchestrationScriptArgument orchestrationScriptArgument in arguments.Where(arg => arg.Type == OrchestrationScriptArgumentType.Metadata))
					{
						input.Metadata.Add(orchestrationScriptArgument.Name, orchestrationScriptArgument.Value);
					}

					result = OrchestrationAutomationHelper.ExecuteOrchestrationScript(connection, scriptName, scriptParams, scriptDummies, input);
				}
				catch (ScriptExecutionFailedException ex) when (Regex.IsMatch(ex.Message, @"No method found in assembly (.+?) matching the specified entrypoint"))
				{
					// If the script does not contain the expected entry point, try executing without it for backward compatibility with older scripts.
					result = OrchestrationAutomationHelper.ExecuteScript(connection, scriptName, scriptParams, scriptDummies);
				}

				return ProcessOrchestrationScriptResult(result);
			}
			catch (Exception ex)
			{
				return new OrchestrationScriptResult
				{
					ErrorMessages = [ex.Message],
					HadError = true,
				};
			}
		}

		private static OrchestrationScriptResult ProcessOrchestrationScriptResult(ExecuteScriptResponseMessage result)
		{
			if (result.EntryPointResult?.Result is RequestScriptInfoOutput scriptOutput
				&& scriptOutput.Data.TryGetValue(OrchestrationScriptConstants.ScriptOutputError, out string errors))
			{
				return new OrchestrationScriptResult
				{
					ErrorMessages = [errors],
					HadError = true,
				};
			}

			return new OrchestrationScriptResult
			{
				ErrorMessages = result.ErrorMessages,
				HadError = result.HadError || result.ErrorMessages.Any(),
			};
		}

		private static List<DmsAutomationScriptParamValue> PrepareScriptParams(
			IDms dms,
			IDmsAutomationScript script,
			IEnumerable<OrchestrationScriptArgument> orchestrationScriptArguments,
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
					scriptParams.Add(new DmsAutomationScriptParamValue(profileParameter.Name, GetProfileParameterValue(profileParameter.Value).ToString()));
					continue;
				}

				var profileInstanceParameter = profileInstance.Value.Values.FirstOrDefault(value => value.Parameter.Name == requiredParameter.Description);
				if (profileInstanceParameter != null)
				{
					scriptParams.Add(new DmsAutomationScriptParamValue(profileInstanceParameter.Parameter.Name, GetProfileParameterValue(profileInstanceParameter.Value).ToString()));
					continue;
				}

				throw new InvalidOperationException($"Missing required script parameter: {requiredParameter.Description}");
			}

			return scriptParams;
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

		private static List<DmsAutomationScriptDummyValue> PrepareScriptDummies(
			IDms dms,
			IDmsAutomationScript script,
			IEnumerable<OrchestrationScriptArgument> orchestrationScriptArguments)
		{
			List<DmsAutomationScriptDummyValue> scriptDummies = [];

			foreach (IDmsAutomationScriptDummy requiredDummy in script.Dummies)
			{
				OrchestrationScriptArgument matchingArgument = orchestrationScriptArguments.FirstOrDefault(arg => arg.Name == requiredDummy.Description && arg.Type == OrchestrationScriptArgumentType.Element);

				if (matchingArgument == null)
				{
					throw new InvalidOperationException($"Missing required script dummy: {requiredDummy.Description}");
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

			return scriptDummies;
		}
	}
}
