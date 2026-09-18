namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.Dialogs;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Plan;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;
	using DropdownParameterDisplayInfo = Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes.DropdownParameterDisplayInfo;
	using GroupPresetOption = Skyline.DataMiner.Utils.InteractiveAutomationScript.Option<Mvc.DisplayTypes.PresetGroupDisplayInfo.PresetInfo>;
	using NumericParameterDisplayInfo = Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes.NumericParameterDisplayInfo;
	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;
	using PresetGroupDisplayInfo = Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes.PresetGroupDisplayInfo;
	using ValueOption = Skyline.DataMiner.Utils.InteractiveAutomationScript.Option<object>;

	public abstract class OrchestrationScript
	{
		private enum OrchestrationScriptContext
		{
			Standalone,
			Event,
		}

		private List<ParameterInfo> _parameterInfos;
		private Dictionary<string, string> _metadata = new Dictionary<string, string>();
		private OrchestrationScriptContext _context = OrchestrationScriptContext.Standalone;
		private OrchestrationLevel _orchestrationLevel = OrchestrationLevel.Unknown;

		private Lazy<OrchestrationEventConfiguration> _eventConfiguration;
		private RequestScriptInfoOutput _returnResult;

		private IEngine _engine;

		public OrchestrationEventConfiguration EventConfiguration => _eventConfiguration?.Value;

		/// <summary>
		/// Gets the input items of this script, evaluated for the values that are currently provided.
		/// This is <see langword="null"/> for scripts that do not override <see cref="GetParameters(OrchestrationInputValues)"/>.
		/// </summary>
		public OrchestrationInputDefinition Inputs { get; private set; }

		/// <summary>
		/// Gets the resolved values of the input items of this script.
		/// </summary>
		public OrchestrationInputValues InputValues { get; private set; } = OrchestrationInputValues.Empty;

		/// <summary>
		/// Performs the orchestration.
		/// Override this method when the script does not use dynamic inputs, otherwise override
		/// <see cref="Orchestrate(IEngine, OrchestrationInputValues)"/>.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public virtual void Orchestrate(IEngine engine)
		{
			throw new NotImplementedException($"'{GetType().Name}' must override Orchestrate(IEngine) or Orchestrate(IEngine, OrchestrationInputValues).");
		}

		/// <summary>
		/// Performs the orchestration with the values that were provided for the inputs of this script.
		/// The default implementation calls <see cref="Orchestrate(IEngine)"/>.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		/// <param name="inputs">The resolved values of the inputs of this script, addressed by path.</param>
		public virtual void Orchestrate(IEngine engine, OrchestrationInputValues inputs)
		{
			Orchestrate(engine);
		}

		public abstract IEnumerable<IOrchestrationParameters> GetParameters();

		/// <summary>
		/// Gets the input items this script requires, based on the values that were already provided.
		/// This method is called again every time a value changes that affects which items are relevant, so that
		/// items can appear or disappear, options and ranges can change, and groups can be repeated.
		/// </summary>
		/// <param name="providedValues">The values that were already provided, keyed by the path of the field they belong to.</param>
		/// <returns>The input items to expose, or <see langword="null"/> when this script does not use dynamic inputs.</returns>
		public virtual OrchestrationInputDefinition GetParameters(OrchestrationInputValues providedValues)
		{
			return null;
		}

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
			var api = _engine.GetMediaOpsLiveApi();
			var eventJobInfo = EventConfiguration.GetJobInfo(api);

			if (eventJobInfo == null)
			{
				return default;
			}

			return eventJobInfo.MonitoringService;
		}

		[AutomationEntryPoint(AutomationEntryPointType.Types.OnRequestScriptInfo)]
		public RequestScriptInfoOutput OnRequestScriptInfoRequest(IEngine engine, RequestScriptInfoInput inputData)
		{
			try
			{
				_engine = engine ?? throw new ArgumentNullException(nameof(engine));
				_eventConfiguration = new Lazy<OrchestrationEventConfiguration>(() => LoadEventFromMetaData(engine));
				_context = OrchestrationScriptContext.Event;

				return new RequestScriptInfoOutput
				{
					Data = HandleRequestInfoEntryPoint(inputData.Data),
				};
			}
			catch (Exception e)
			{
				return new RequestScriptInfoOutput
				{
					Data = new Dictionary<string, string> { { OrchestrationScriptConstants.ScriptOutputError, e.ToString() } },
				};
			}
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
			ParameterInfo param = _parameterInfos.FirstOrDefault(paramInfo => paramInfo.Name == paramName);

			if (param == null)
			{
				throw new InvalidOperationException($"Parameter with name '{paramName}' is missing");
			}

			return param.Value;
		}

		/// <summary>
		/// Attempts to get the value of the input field with the specified path.
		/// </summary>
		/// <param name="path">The path of the field, for example <c>destinations/1/endpoint</c>.</param>
		/// <param name="value">When this method returns <see langword="true"/>, contains the value of the field.</param>
		/// <returns><see langword="true"/> when the field holds a value; otherwise, <see langword="false"/>.</returns>
		public bool TryGetInputValue(string path, out object value)
		{
			return InputValues.TryGetValue(path, out value);
		}

		/// <summary>
		/// Gets the value of the input field with the specified path.
		/// </summary>
		/// <param name="path">The path of the field, for example <c>destinations/1/endpoint</c>.</param>
		/// <returns>The value of the field.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the field does not hold a value.</exception>
		public object GetInputValue(string path)
		{
			if (!TryGetInputValue(path, out var value))
			{
				throw new InvalidOperationException($"Orchestration input '{path}' is missing");
			}

			return value;
		}

		public bool TryGetMetadataValue(string metadataParam, out string metadataValue)
		{
			return _metadata.TryGetValue(metadataParam, out metadataValue);
		}

		public bool TryGetNodeConfiguration(string nodeLabel, out NodeConfiguration nodeConfiguration)
		{
			if (_context != OrchestrationScriptContext.Event)
			{
				nodeConfiguration = null;
				return false;
			}

			if (EventConfiguration?.Configuration == null)
			{
				throw new InvalidOperationException("No event configuration was found");
			}

			nodeConfiguration = EventConfiguration.Configuration.NodeConfigurations.FirstOrDefault(nc => nc.NodeLabel == nodeLabel);

			return nodeConfiguration != null;
		}

		public void OrchestrateNode(NodeConfiguration nodeConfig)
		{
			if (_context != OrchestrationScriptContext.Event || _orchestrationLevel != OrchestrationLevel.Global)
			{
				return;
			}

			if (nodeConfig == null)
			{
				throw new ArgumentNullException(nameof(nodeConfig));
			}

			if (String.IsNullOrEmpty(nodeConfig.OrchestrationScriptName))
			{
				return;
			}

			IEnumerable<OrchestrationScriptArgument> addedInputParams = new OrchestrationScriptInternalInput(EventConfiguration.ID, OrchestrationLevel.Node).ToMetadataArguments();

			OrchestrationEventExecutionHelper.ExecuteOrchestrationScript(
				_engine.GetUserConnection(),
				nodeConfig.OrchestrationScriptName,
				nodeConfig.OrchestrationScriptArguments.Union(addedInputParams).ToList(),
				nodeConfig.Profile);
		}

		/// <summary>
		/// Orchestrates all connections for the event. Based on event type, this will be a connect or disconnect operation.
		/// </summary>
		/// <param name="timeoutSeconds">Optional argument to override timeout (default 60 seconds).</param>
		public void OrchestrateAllConnections(int timeoutSeconds = 60)
		{
			if (_context != OrchestrationScriptContext.Event || _orchestrationLevel != OrchestrationLevel.Global)
			{
				return;
			}

			var api = new EngineMediaOpsLiveApi(_engine);
			var planHelper = api.GetMediaOpsPlanHelper();

			var orchestrationEventExecutionHelper = new OrchestrationEventExecutionHelper(api, planHelper, new OrchestrationSettings { Timeout = TimeSpan.FromSeconds(timeoutSeconds) });

			IPerformanceLogger performanceLogger = PerformanceLoggerFactory.Create("ORC-OrchestrateAllConnections");

			using (PerformanceCollector collector = new PerformanceCollector(performanceLogger))
			using (PerformanceTracker performanceTracker = new PerformanceTracker(collector))
			{
				if (!EventConfiguration.HasConnections)
				{
					return;
				}

				orchestrationEventExecutionHelper.ExecuteConnections(EventConfiguration, performanceTracker);
			}
		}

		private void RunSafe(IEngine engine)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));

			OrchestrationScriptInfo scriptInfo = GetScriptInfo(OrchestrationInputValues.Empty);

			_parameterInfos = CreateParameterInfos(scriptInfo, new OrchestrationScriptInput());

			if (GetIncompleteInfos(_parameterInfos).Any())
			{
				GetValuesFromUser(_parameterInfos);
			}

			Orchestrate(engine, InputValues);
		}

		private OrchestrationEventConfiguration LoadEventFromMetaData(IEngine engine)
		{
			var api = engine.GetMediaOpsLiveApi();

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
						// The caller can pass the values it already collected so the returned input definition reflects them.
						OrchestrationScriptInput scriptInput = ReadScriptInput(metaData);
						OrchestrationScriptInfo scriptInfo = GetScriptInfo(new OrchestrationInputValues(scriptInput.InputValues));
						return new Dictionary<string, string> { { OrchestrationScriptConstants.OrchestrationScriptInfoRequestScriptInfoKey, JsonConvert.SerializeObject(scriptInfo) } };
					}

				case OrchestrationScriptAction.PerformOrchestration:
				case OrchestrationScriptAction.PerformOrchestrationAskMissingValues:
					{
						PerformOrchestrationFromEntryPoint(metaData, orchestrationScriptAction == OrchestrationScriptAction.PerformOrchestrationAskMissingValues);

						// Currently there is no valuable output to provide for this flow.
						return new Dictionary<string, string>();
					}

				default:
					throw new NotSupportedException($"No support for orchestration script action {orchestrationScriptAction}");
			}
		}

		private OrchestrationScriptInfo GetScriptInfo(OrchestrationInputValues providedValues)
		{
			OrchestrationScriptInfo info = new OrchestrationScriptInfo();
			foreach (IOrchestrationParameters orchestrationParameters in GetParameters())
			{
				if (orchestrationParameters is OrchestrationProfileDefinition definition)
				{
					info.ProfileDefinitionReferences.Add(definition.GetDefinitionReference(_engine));
					info.ProfileDefinitions.Add(definition.GetDefinitionReference(_engine).ID);
				}

				foreach (KeyValuePair<string, Parameter> keyValuePair in orchestrationParameters.GetParameterReferences(_engine))
				{
					info.ProfileParameterReferences.Add(keyValuePair.Value.ID, keyValuePair.Value);
					info.ProfileParametersIdByName.Add(keyValuePair.Key, keyValuePair.Value.ID);
				}
			}

			if (info.ProfileDefinitions.Count > 1)
			{
				throw new NotSupportedException("Currently only a single profile definition can be supported by an orchestration script");
			}

			info.InputDefinition = EvaluateInputs(providedValues);

			RegisterProfileBackedInputs(info);

			return info;
		}

		// Profile backed inputs are also published by path so consumers that key inputs by profile parameter keep working.
		private void RegisterProfileBackedInputs(OrchestrationScriptInfo info)
		{
			if (info.InputDefinition == null)
			{
				return;
			}

			foreach (var field in info.InputDefinition.GetAllFields().Where(x => x.IsProfileBacked))
			{
				info.ProfileParametersIdByName[field.Path] = field.ProfileParameterId.Value;
			}
		}

		private OrchestrationInputDefinition EvaluateInputs(OrchestrationInputValues providedValues)
		{
			OrchestrationInputValues values = providedValues ?? OrchestrationInputValues.Empty;
			OrchestrationInputDefinition definition = OrchestrationInputDefinition.Evaluate(ResolveInputs, values);

			Inputs = definition;
			InputValues = definition == null ? values : definition.GetValues();

			return definition;
		}

		private OrchestrationInputDefinition ResolveInputs(OrchestrationInputValues values)
		{
			OrchestrationInputDefinition definition = GetParameters(values);

			if (definition == null)
			{
				return null;
			}

			new OrchestrationInputProfileResolver(new ProfileHelper(_engine.SendSLNetMessages)).Resolve(definition);

			return definition;
		}

		private static OrchestrationScriptInput ReadScriptInput(IReadOnlyDictionary<string, string> metaData)
		{
			if (metaData != null
				&& metaData.TryGetValue(OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey, out string serializedScriptInput)
				&& !String.IsNullOrWhiteSpace(serializedScriptInput))
			{
				return JsonConvert.DeserializeObject<OrchestrationScriptInput>(serializedScriptInput) ?? new OrchestrationScriptInput();
			}

			return new OrchestrationScriptInput();
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

		private List<ParameterInfo> CreateParameterInfos(OrchestrationScriptInfo scriptInfo, OrchestrationScriptInput input)
		{
			List<ParameterInfo> parameterInfos = new List<ParameterInfo>();

			// Create objects for all orchestration parameters.
			foreach (KeyValuePair<Guid, Parameter> profileParameter in scriptInfo.ProfileParameterReferences)
			{
				ProfileParameterID reference = new ProfileParameterID(profileParameter.Key);
				var overrideName = scriptInfo.ProfileParametersIdByName.FirstOrDefault(kv => kv.Value == profileParameter.Key).Key;
				ParameterInfo info = new ParameterInfo
				{
					Name = overrideName ?? profileParameter.Value.Name,
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

		private void GetSeparateParams(OrchestrationScriptInput input, List<ParameterInfo> parameterInfos)
		{
			foreach (KeyValuePair<string, object> parameterValue in input.ProfileParameterValues)
			{
				ParameterInfo matchInfo = null;
				if (Guid.TryParse(parameterValue.Key, out Guid profileParameterId))
				{
					matchInfo = parameterInfos.FirstOrDefault(x => x.Reference is ProfileParameterID id && id.Id == profileParameterId);
				}
				else
				{
					matchInfo = parameterInfos.FirstOrDefault(x => x.Name == parameterValue.Key);
				}

				if (matchInfo != null)
				{
					matchInfo.Value = parameterValue.Value;
				}
			}
		}

		private void LinkParameters(OrchestrationScriptInfo scriptInfo, List<ParameterInfo> infos)
		{
			Dictionary<Guid, ParameterInfo> profileParameterInfos = infos
				.Where(x => x.Reference is ProfileParameterID)
				.ToDictionary(x => (x.Reference as ProfileParameterID).Id);

			Dictionary<Guid, Parameter>.ValueCollection profileParameters = scriptInfo.ProfileParameterReferences.Values;

			foreach (Parameter parameter in profileParameters)
			{
				if (!profileParameterInfos.TryGetValue(parameter.ID, out ParameterInfo parameterInfo))
				{
					throw new InvalidOperationException($"Parameter {parameter} wasn't requested");
				}

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
								string display = queue.Dequeue();
								options.Add(new ValueOption(display, discreet));
							}

							parameterInfo.DisplayInfo = new DropdownParameterDisplayInfo
							{
								Label = parameterInfo.Name,
								Options = options,
							};
						}

						break;

					case Parameter.ParameterType.Number:
						{
							parameterInfo.DisplayInfo = new NumericParameterDisplayInfo()
							{
								Label = parameterInfo.Name,
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
								Label = parameterInfo.Name,
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

		private void PerformOrchestrationFromEntryPoint(IReadOnlyDictionary<string, string> metaData, bool askMissingValues)
		{
			OrchestrationScriptInput orchestrationScriptInput = ReadScriptInput(metaData);

			// The metadata is assigned first so the script can already use it while it determines its inputs.
			_metadata = orchestrationScriptInput.Metadata ?? new Dictionary<string, string>();

			OrchestrationScriptInfo scriptInfo = GetScriptInfo(new OrchestrationInputValues(orchestrationScriptInput.InputValues));

			_parameterInfos = CreateParameterInfos(scriptInfo, orchestrationScriptInput);

			if (askMissingValues)
			{
				List<ParameterInfo> incompleteInfos = GetIncompleteInfos(_parameterInfos).ToList();
				if (incompleteInfos.Any())
				{
					GetValuesFromUser(incompleteInfos);
				}
			}

			TryGetMetadataValue("{Orchestration Level}", out string orchestrationLevel);
			_orchestrationLevel = Enum.TryParse(orchestrationLevel, out OrchestrationLevel parsedLevel) ? parsedLevel : OrchestrationLevel.Unknown;

			Orchestrate(_engine, InputValues);

			if (_orchestrationLevel != OrchestrationLevel.Global)
			{
				return;
			}

			if (EventConfiguration.IsStartEvent)
			{
				var api = _engine.GetMediaOpsLiveApi();
				var eventJobInfo = EventConfiguration.GetJobInfo(api);

				if (eventJobInfo != null)
				{
					eventJobInfo.MonitoringService = SetupService(_engine);
					api.Orchestration.JobInfos.Update(eventJobInfo);
				}
			}

			if (EventConfiguration.IsStopEvent)
			{
				TearDownService(_engine);

				var api = _engine.GetMediaOpsLiveApi();
				var eventJobInfo = EventConfiguration.GetJobInfo(api);

				if (eventJobInfo == null || eventJobInfo.MonitoringService == default)
				{
					return;
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
		}
	}
}