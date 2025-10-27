namespace Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.Automation;
	using Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.Dialogs;
	using Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes;
	using Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Enums;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;

	using DropdownParameterDisplayInfo = Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes.DropdownParameterDisplayInfo;
	using GroupPresetOption = Skyline.DataMiner.Utils.InteractiveAutomationScript.Option<Mvc.DisplayTypes.PresetGroupDisplayInfo.PresetInfo>;
	using NumericParameterDisplayInfo = Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes.NumericParameterDisplayInfo;
	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;
	using PresetGroupDisplayInfo = Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes.PresetGroupDisplayInfo;
	using ValueOption = Skyline.DataMiner.Utils.InteractiveAutomationScript.Option<object>;

	public abstract class OrchestrationScript
	{
		private List<ParameterInfo> _parameterInfos;
		private Dictionary<string, string> _metadata = new Dictionary<string, string>();

		private Lazy<OrchestrationEventConfiguration> _eventConfiguration;

		private IEngine _engine;

		public OrchestrationEventConfiguration EventConfiguration => _eventConfiguration?.Value;

		public abstract void Orchestrate(IEngine engine);

		public abstract IEnumerable<IOrchestrationParameters> GetParameters();

		public virtual DmsServiceId SetupService(IEngine engine)
		{
			return default(DmsServiceId);
		}

		public virtual void TearDownService(IEngine engine)
		{
			// No base logic.
		}

		public DmsServiceId GetEventMonitoringService()
		{
			MediaOpsLiveApi api = _engine.GetMediaOpsLiveApi();
			OrchestrationJobInfo eventJobInfo = EventConfiguration.GetJobInfo(api);

			if (eventJobInfo == null)
			{
				return default;
			}

			return eventJobInfo.MonitoringService;
		}

		[AutomationEntryPoint(AutomationEntryPointType.Types.OnRequestScriptInfo)]
		public RequestScriptInfoOutput OnRequestScriptInfoRequest(IEngine engine, RequestScriptInfoInput inputData)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
			_eventConfiguration = new Lazy<OrchestrationEventConfiguration>(() => LoadEventFromMetaData(engine));

			return new RequestScriptInfoOutput
			{
				Data = HandleRequestInfoEntryPoint(inputData.Data),
			};
		}

		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(IEngine engine)
		{
			try
			{
				RunSafe(engine);
			}
			catch (ScriptAbortException)
			{
				// Catch normal abort exceptions (engine.ExitFail or engine.ExitSuccess)
				throw; // Comment if it should be treated as a normal exit of the script.
			}
			catch (ScriptForceAbortException)
			{
				// Catch forced abort exceptions, caused via external maintenance messages.
				throw;
			}
			catch (ScriptTimeoutException)
			{
				// Catch timeout exceptions for when a script has been running for too long.
				throw;
			}
			catch (InteractiveUserDetachedException)
			{
				// Catch a user detaching from the interactive script by closing the window.
				// Only applicable for interactive scripts, can be removed for non-interactive scripts.
				throw;
			}
			catch (Exception e)
			{
				engine.ExitFail("Run|Something went wrong: " + e);
			}
		}

		public object GetParameterValue(string paramName)
		{
			ParameterInfo param = _parameterInfos.FirstOrDefault(paramInfo => paramInfo.Id == paramName);

			if (param == null)
			{
				throw new InvalidOperationException($"Parameter with name '{paramName}' is missing");
			}

			return param.Value;
		}

		public bool TryGetMetadataValue(string metadataParam, out string metadataValue)
		{
			return _metadata.TryGetValue(metadataParam, out metadataValue);
		}

		public bool TryGetNodeConfiguration(string nodeLabel, out NodeConfiguration nodeConfiguration)
		{
			if (EventConfiguration?.Configuration == null)
			{
				throw new InvalidOperationException("No event configuration was found");
			}

			nodeConfiguration = EventConfiguration.Configuration.NodeConfigurations.FirstOrDefault(nc => nc.NodeLabel == nodeLabel);

			return nodeConfiguration != null;
		}

		public void OrchestrateNode(NodeConfiguration nodeConfig)
		{
			if (nodeConfig == null)
			{
				throw new ArgumentNullException(nameof(nodeConfig));
			}

			IEnumerable<OrchestrationScriptArgument> addedInputParams = new OrchestrationScriptInternalInput(EventConfiguration.ID, OrchestrationLevel.Node).ToMetadataArguments();

			OrchestrationEventExecutionHelper.ExecuteOrchestrationScript(_engine.GetUserConnection(), nodeConfig.OrchestrationScriptName, nodeConfig.OrchestrationScriptArguments.Union(addedInputParams), nodeConfig.Profile);
		}

		/// <summary>
		/// Orchestrates all connections for the event. Based on event type, this will be a connect or disconnect operation.
		/// </summary>
		/// <param name="timeoutSeconds">Optional argument to override timeout (default 60 seconds).</param>
		public void OrchestrateAllConnections(int timeoutSeconds = 60)
		{
			MediaOpsLiveApi api = _engine.GetMediaOpsLiveApi();

			OrchestrationEventExecutionHelper orchestrationEventExecutionHelper = new OrchestrationEventExecutionHelper(api, new OrchestrationSettings { Timeout = TimeSpan.FromSeconds(timeoutSeconds) });

			string performanceLogFilename = $"ORC-API - {DateTime.UtcNow:yyyy-MM-dd}";
			PerformanceFileLogger performanceFileLogger = new PerformanceFileLogger("ORC-OrchestrateAllConnections", performanceLogFilename);

			using (PerformanceCollector collector = new PerformanceCollector(performanceFileLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				orchestrationEventExecutionHelper.ProcessConnections(new List<OrchestrationEventConfiguration> { EventConfiguration }, performanceTracker);
			}
		}

		private void RunSafe(IEngine engine)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));

			ScriptInfo scriptInfo = GetScriptInfo();

			_parameterInfos = CreateParameterInfos(scriptInfo, new OrchestrationScriptInput());

			if (GetIncompleteInfos(_parameterInfos).Any())
			{
				GetValuesFromUser(_parameterInfos);
			}

			Orchestrate(engine);
		}

		private OrchestrationEventConfiguration LoadEventFromMetaData(IEngine engine)
		{
			MediaOpsLiveApi api = engine.GetMediaOpsLiveApi();

			if (!TryGetMetadataValue("{Event ID}", out string eventId) || !Guid.TryParse(eventId, out Guid eventGuid) || eventGuid == Guid.Empty)
			{
				return null;
			}

			List<OrchestrationEventConfiguration> events = api.Orchestration.GetEventConfigurationsById(new List<Guid> { eventGuid }).ToList();

			if (!events.Any())
			{
				return null;
			}

			return events.First();
		}

		private Dictionary<string, string> HandleRequestInfoEntryPoint(IReadOnlyDictionary<string, string> metaData)
		{
			string unparsedOrchestrationScriptAction = null;
			if (metaData is null ||
			    !metaData.TryGetValue(OrchestrationScriptConstants.OrchestrationScriptActionRequestScriptInfoKey, out unparsedOrchestrationScriptAction) ||
			    !Enum.TryParse(unparsedOrchestrationScriptAction, out OrchestrationScriptAction orchestrationScriptAction))
			{
				throw new InvalidOperationException($"No orchestration script action was provided (got {unparsedOrchestrationScriptAction}");
			}

			switch (orchestrationScriptAction)
			{
				case OrchestrationScriptAction.OrchestrationScriptInfo:
				{
					ScriptInfo scriptInfo = GetScriptInfo();
					return new Dictionary<string, string> { { OrchestrationScriptConstants.OrchestrationScriptInfoRequestScriptInfoKey, scriptInfo == null ? null : JsonConvert.SerializeObject(scriptInfo) } };
				}

				case OrchestrationScriptAction.PerformOrchestration:
				case OrchestrationScriptAction.PerformOrchestrationAskMissingValues:
				{
					OrchestrationScriptOutput orchestrationScriptOutput = PerformOrchestrationFromEntryPoint(metaData, orchestrationScriptAction == OrchestrationScriptAction.PerformOrchestrationAskMissingValues);
					return new Dictionary<string, string> { { OrchestrationScriptConstants.ScriptOutputRequestScriptInfoKey, orchestrationScriptOutput == null ? null : JsonConvert.SerializeObject(orchestrationScriptOutput) } };
				}

				default:
					throw new NotSupportedException($"No support for orchestration script action {orchestrationScriptAction}");
			}
		}

		private ScriptInfo GetScriptInfo()
		{
			ScriptInfo info = new ScriptInfo();
			foreach (IOrchestrationParameters orchestrationParameters in GetParameters())
			{
				if (orchestrationParameters is OrchestrationProfileDefinition definition)
				{
					info.ProfileDefinitionReferences.Add(definition.GetDefinitionReference(_engine));
					info.ProfileDefinitions.Add(definition.GetDefinitionReference(_engine).ID);
				}

				foreach (KeyValuePair<string, Parameter> keyValuePair in orchestrationParameters.GetParameterReferences(_engine))
				{
					info.ProfileParameterReferences.Add(keyValuePair.Key, keyValuePair.Value);
					info.ProfileParameters.Add(keyValuePair.Key, keyValuePair.Value.ID);
				}
			}

			return info;
		}

		private void GetValuesFromUser(List<ParameterInfo> infos)
		{
			InteractiveController controller = new InteractiveController(_engine);
			GetOrchestrationValuesDialog dialog = new GetOrchestrationValuesDialog(_engine, infos);

			dialog.Button.Pressed += (sender, args) =>
			{
				controller.Stop();
				dialog.UpdateValues();
			};

			controller.ShowDialog(dialog);
		}

		private List<ParameterInfo> CreateParameterInfos(ScriptInfo scriptInfo, OrchestrationScriptInput input)
		{
			List<ParameterInfo> parameterInfos = new List<ParameterInfo>();

			// Create objects for all orchestration parameters.
			foreach (KeyValuePair<string, Parameter> profileParameter in scriptInfo.ProfileParameterReferences)
			{
				ProfileParameterID reference = new ProfileParameterID(profileParameter.Value.ID);
				ParameterInfo info = new ParameterInfo
				{
					Id = profileParameter.Key,
					Reference = reference,
				};

				switch (profileParameter.Value?.Type)
				{
					case Parameter.ParameterType.Text:
						info.Type = "TextParameter";
						break;
				}

				parameterInfos.Add(info);
			}

			// Add profile instance parameter values from provided instance in input.
			GetParamsFromInstance(input, parameterInfos);

			// Add single parameter values from the input.
			GetSeparateParams(input, parameterInfos);

			LinkParameters(scriptInfo, parameterInfos);

			return parameterInfos;
		}

		private void GetParamsFromInstance(OrchestrationScriptInput input, List<ParameterInfo> parameterInfos)
		{
			if (String.IsNullOrEmpty(input.ProfileInstance))
			{
				return;
			}

			ProfileHelper helper = new ProfileHelper(_engine.SendSLNetMessages);
			List<ProfileInstance> instances = helper.ProfileInstances.Read(ProfileInstanceExposers.Name.Equal(input.ProfileInstance));

			if (instances.Count == 0)
			{
				throw new InvalidOperationException($"No profile instance found with name {input.ProfileInstance}");
			}

			if (instances.Count > 1)
			{
				throw new InvalidOperationException($"Multiple profile instances found with name {input.ProfileInstance}");
			}

			ProfileInstance instance = instances.First();

			foreach (ProfileParameterEntry profileParameterEntry in instance.Values)
			{
				ParameterInfo matchInfo = parameterInfos
					.FirstOrDefault(x => (x.Reference as ProfileParameterID).Id == profileParameterEntry.ParameterID);

				if (matchInfo != null)
				{
					matchInfo.Value = profileParameterEntry.Value.Type == ParameterValue.ValueType.Double
						? profileParameterEntry.Value.DoubleValue
						: profileParameterEntry.Value.StringValue;
				}
			}
		}

		private static void GetSeparateParams(OrchestrationScriptInput input, List<ParameterInfo> parameterInfos)
		{
			foreach (KeyValuePair<string, object> parameterValue in input.ProfileParameterValues)
			{
				ParameterInfo matchInfo = parameterInfos
					.FirstOrDefault(x => x.Id == parameterValue.Key);

				if (matchInfo != null)
				{
					matchInfo.Value = parameterValue.Value;
				}
			}
		}

		private void LinkParameters(ScriptInfo scriptInfo, List<ParameterInfo> infos)
		{
			Dictionary<Guid, ParameterInfo> profileParameterInfos = infos
				.Where(x => x.Reference is ProfileParameterID)
				.ToDictionary(x => (x.Reference as ProfileParameterID).Id);

			Dictionary<string, Parameter>.ValueCollection profileParameters = scriptInfo.ProfileParameterReferences.Values;

			foreach (Parameter parameter in profileParameters)
			{
				if (!profileParameterInfos.TryGetValue(parameter.ID, out ParameterInfo parameterInfo))
				{
					throw new InvalidOperationException($"Parameter {parameter} wasn't requested");
				}

				_engine.GenerateInformation($"Interprete Types|{JsonConvert.SerializeObject(profileParameters)}");

				parameterInfo.Description = parameter.Name;
				parameterInfo.Type = "ProfileParameter";
				switch (parameter.InterpreteType.Type)
				{
					case InterpreteType.TypeEnum.Double:
						parameterInfo.ValueType = typeof(double);
						break;

					case InterpreteType.TypeEnum.String:
						parameterInfo.ValueType = typeof(string);
						break;

					// Tip: the use case for these types is unclear. Perhaps using them should fail.
					case InterpreteType.TypeEnum.HighNibble:
					case InterpreteType.TypeEnum.Undefined:
					default:
						parameterInfo.ValueType = typeof(object);
						break;
				}

				switch (parameter.Type)
				{
					case Parameter.ParameterType.Discrete:
						{
							Queue<string> queue = new Queue<string>(parameter.DiscreetDisplayValues);
							List<ValueOption> options = new List<ValueOption>();
							foreach (string discreet in parameter.Discretes)
							{
								if (discreet.GetType() != parameterInfo.ValueType)
								{
									// Tip: warn or fail when this happens
									continue;
								}

								string display = queue.Dequeue();

								// Tip: make sure the display value is unique
								options.Add(new ValueOption(display, discreet));
							}

							parameterInfo.DisplayInfo = new DropdownParameterDisplayInfo
							{
								Label = parameterInfo.Id,
								Options = options,
							};
						}

						break;

					case Parameter.ParameterType.Number:
						{
							parameterInfo.DisplayInfo = new NumericParameterDisplayInfo()
							{
								Label = parameterInfo.Id,
								Min = parameter.RangeMin,
								Max = parameter.RangeMax,
								Step = parameter.Stepsize,
								Decimals = parameter.Decimals,
								Unit = parameter.Units,
							};
						}

						break;

					case Parameter.ParameterType.Text:
						{
							parameterInfo.DisplayInfo = new TextParameterDisplayInfo()
							{
								Label = parameterInfo.Id,
							};
						}

						break;

					default:
						throw new NotSupportedException($"Unsupported parameter type {parameter.Type}");
				}
			}

			AssignProfileDefinitionGroups(scriptInfo.ProfileDefinitionReferences, profileParameterInfos);
		}

		private void AssignProfileDefinitionGroups(List<ProfileDefinition> profileDefinitions, Dictionary<Guid, ParameterInfo> parameters)
		{
			ProfileHelper profileHelper = new ProfileHelper(_engine.SendSLNetMessages);

			foreach (ProfileDefinition definition in profileDefinitions)
			{
				// Tip: If there are a lot of definitions, getting all instances in one call will be more efficient.
				List<ProfileInstance> instances = profileHelper.ProfileInstances.Read(ProfileInstanceExposers.AppliesToID.Equal(definition.ID));
				_engine.GenerateInformation($"AssignProfileDefinitionGroups|GetProfileInstance{definition.ID}|{JsonConvert.SerializeObject(instances)}");

				List<GroupPresetOption> presets = new List<GroupPresetOption>(instances.Count);
				foreach (ProfileInstance instance in instances)
				{
					PresetGroupDisplayInfo.PresetInfo presetInfo = new PresetGroupDisplayInfo.PresetInfo();

					// Tip: add a check if the option names are unique
					presets.Add(new GroupPresetOption(instance.Name, presetInfo));

					foreach (ProfileParameterEntry value in instance.Values)
					{
						if (!parameters.TryGetValue(value.ParameterID, out ParameterInfo parameter))
						{
							continue;
						}

						switch (value.Value.Type)
						{
							case ParameterValue.ValueType.Double:
								presetInfo.ParameterValues.Add((parameter, value.Value.DoubleValue));
								break;

							case ParameterValue.ValueType.String:
								presetInfo.ParameterValues.Add((parameter, value.Value.StringValue));
								break;

							default:
								throw new NotSupportedException($"No support for type {value.Value.Type} (Parameter ID: {value.ParameterID}; Profile Instance ID: {instance.ID})");
						}
					}
				}

				ParameterGroup group = new ParameterGroup
				{
					Description = definition.Name,
					Reference = new ProfileDefinitionID(definition.ID),
					Type = "ProfileDefinition",
					DisplayInfo = new PresetGroupDisplayInfo
					{
						Label = definition.Name,
						Presets = presets,
					},
				};

				foreach (Guid parameterId in definition.ParameterIDs)
				{
					if (!parameters.TryGetValue(parameterId, out ParameterInfo parameter))
					{
						continue;
					}

					parameter.Group = group;
				}
			}
		}

		private IEnumerable<ParameterInfo> GetIncompleteInfos(IEnumerable<ParameterInfo> infos) => infos.Where(x => x.Value is null);

		private OrchestrationScriptOutput PerformOrchestrationFromEntryPoint(IReadOnlyDictionary<string, string> metaData, bool askMissingValues)
		{
			OrchestrationScriptOutput orchestrationScriptOutput = new OrchestrationScriptOutput();

			ScriptInfo scriptInfo = GetScriptInfo();

			OrchestrationScriptInput orchestrationScriptInput = new OrchestrationScriptInput();
			if (metaData.TryGetValue(OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey, out string serializedScriptInputRequestScriptInfo))
			{
				orchestrationScriptInput = JsonConvert.DeserializeObject<OrchestrationScriptInput>(serializedScriptInputRequestScriptInfo);
			}

			_metadata = orchestrationScriptInput.Metadata;
			_parameterInfos = CreateParameterInfos(scriptInfo, orchestrationScriptInput);

			if (askMissingValues)
			{
				List<ParameterInfo> incompleteInfos = GetIncompleteInfos(_parameterInfos).ToList();
				if (incompleteInfos.Any())
				{
					GetValuesFromUser(incompleteInfos);
				}
			}

			Orchestrate(_engine);

			TryGetMetadataValue("{Orchestration Level}", out string orchestrationLevel);
			if (orchestrationLevel != "Global")
			{
				return orchestrationScriptOutput;
			}

			if (EventConfiguration.IsStartEvent)
			{
				MediaOpsLiveApi api = _engine.GetMediaOpsLiveApi();
				OrchestrationJobInfo eventJobInfo = EventConfiguration.GetJobInfo(api);

				if (eventJobInfo != null)
				{
					eventJobInfo.MonitoringService = SetupService(_engine);
					api.Orchestration.JobInfos.Update(eventJobInfo);
				}
			}

			if (EventConfiguration.IsStopEvent)
			{
				TearDownService(_engine);

				MediaOpsLiveApi api = _engine.GetMediaOpsLiveApi();
				OrchestrationJobInfo eventJobInfo = EventConfiguration.GetJobInfo(api);

				if (eventJobInfo == null || eventJobInfo.MonitoringService == default)
				{
					return orchestrationScriptOutput;
				}

				IDms dms = _engine.GetDms();
				if (dms.ServiceExists(eventJobInfo.MonitoringService))
				{
					var service = dms.GetService(eventJobInfo.MonitoringService);
					service.Delete();
				}

				eventJobInfo.MonitoringService = default;
				api.Orchestration.JobInfos.Update(eventJobInfo);
			}

			return orchestrationScriptOutput;
		}
	}
}