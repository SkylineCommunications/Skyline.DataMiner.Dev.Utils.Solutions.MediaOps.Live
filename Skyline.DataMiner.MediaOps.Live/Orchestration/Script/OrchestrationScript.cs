namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.Dialogs;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.DisplayTypes;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	using GroupPresetOption = Skyline.DataMiner.Utils.InteractiveAutomationScript.Option<Mvc.DisplayTypes.PresetGroupDisplayInfo.PresetInfo>;
	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;
	using ValueOption = Skyline.DataMiner.Utils.InteractiveAutomationScript.Option<object>;

	public abstract class OrchestrationScript
	{
		internal static readonly string OrchestrationScriptActionRequestScriptInfoKey = nameof(OrchestrationScriptAction);
		internal static readonly string OrchestrationScriptInfoRequestScriptInfoKey = "OrchestrationScriptInfo";
		internal static readonly string ScriptInputRequestScriptInfoKey = "OrchestrationScriptInput";
		internal static readonly string ScriptOutputRequestScriptInfoKey = "OrchestrationScriptOutput";

		private List<ParameterInfo> _parameterInfos;

		private IEngine _engine;

		public abstract void Orchestrate(IEngine engine);

		public abstract IEnumerable<IOrchestrationParameters> GetParameters();

		[AutomationEntryPoint(AutomationEntryPointType.Types.OnRequestScriptInfo)]
		public RequestScriptInfoOutput OnRequestScriptInfoRequest(IEngine engine, RequestScriptInfoInput inputData)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));

			_engine.GenerateInformation(JsonConvert.SerializeObject(inputData));
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

		private void RunSafe(IEngine engine)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));

			ScriptInfo scriptInfo = GetScriptInfo();

			_parameterInfos = CreateParameterInfos(scriptInfo, new ScriptInput());

			if (GetIncompleteInfos(_parameterInfos).Any())
			{
				GetValuesFromUser(_parameterInfos);
			}

			Orchestrate(engine);
		}

		private Dictionary<string, string> HandleRequestInfoEntryPoint(IReadOnlyDictionary<string, string> metaData)
		{
			string unparsedOrchestrationScriptAction = null;
			if (metaData is null ||
			    !metaData.TryGetValue(OrchestrationScriptActionRequestScriptInfoKey, out unparsedOrchestrationScriptAction) ||
			    !Enum.TryParse(unparsedOrchestrationScriptAction, out OrchestrationScriptAction orchestrationScriptAction))
			{
				throw new InvalidOperationException($"No orchestration script action was provided (got {unparsedOrchestrationScriptAction}");
			}

			switch (orchestrationScriptAction)
			{
				case OrchestrationScriptAction.OrchestrationScriptInfo:
				{
					ScriptInfo scriptInfo = GetScriptInfo();
					return new Dictionary<string, string> { { OrchestrationScriptInfoRequestScriptInfoKey, scriptInfo == null ? null : JsonConvert.SerializeObject(scriptInfo) } };
				}

				case OrchestrationScriptAction.PerformOrchestration:
				case OrchestrationScriptAction.PerformOrchestrationAskMissingValues:
				{
					ScriptOutput scriptOutput = PerformOrchestrationFromEntryPoint(metaData, orchestrationScriptAction == OrchestrationScriptAction.PerformOrchestrationAskMissingValues);
					return new Dictionary<string, string> { { ScriptOutputRequestScriptInfoKey, scriptOutput == null ? null : JsonConvert.SerializeObject(scriptOutput) } };
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
				if (orchestrationParameters is OrchestrationProfileDefinition)
				{
					info.ProfileDefinitionReferences.Add(((OrchestrationProfileDefinition)orchestrationParameters).GetDefinitionReference(_engine));
					info.ProfileDefinitions.Add(((OrchestrationProfileDefinition)orchestrationParameters).GetDefinitionReference(_engine).ID);
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

		private List<ParameterInfo> CreateParameterInfos(ScriptInfo scriptInfo, ScriptInput input)
		{
			List<ParameterInfo> parameterInfos = new List<ParameterInfo>(scriptInfo.ProfileParameterReferences.Count);

			foreach (KeyValuePair<string, Parameter> profileParameter in scriptInfo.ProfileParameterReferences)
			{
				ProfileParameterID reference = new ProfileParameterID(profileParameter.Value.ID);
				ParameterInfo info = new ParameterInfo
				{
					Id = profileParameter.Key,
					Reference = reference,
					Value = input.ProfileParameterValues.TryGetValue(profileParameter.Key, out object value) ? value : null,
				};

				parameterInfos.Add(info);
			}

			_engine.GenerateInformation(JsonConvert.SerializeObject(input));

			if (!String.IsNullOrEmpty(input.ProfileInstance))
			{
				_engine.GenerateInformation("Get Instance Values");
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
				
				_engine.GenerateInformation("Found Instance with " + instances.First().Values.Length + " values");
				foreach (ProfileParameterEntry profileParameterEntry in instances.First().Values)
				{
					string paramName = scriptInfo.ProfileParameters
						.FirstOrDefault(x => x.Value == profileParameterEntry.ParameterID).Key;

					_engine.GenerateInformation($"Parameter {paramName} with value {profileParameterEntry.Value}");

					ParameterInfo info = new ParameterInfo
					{
						Id = paramName,
						Reference = new ProfileParameterID(profileParameterEntry.ParameterID),
						Value = profileParameterEntry.Value.Type == ParameterValue.ValueType.Double
							? profileParameterEntry.Value.DoubleValue
							: profileParameterEntry.Value.StringValue,
					};

					parameterInfos.Add(info);
				}
			}

			LinkParameters(scriptInfo, parameterInfos);

			return parameterInfos;
		}

		private void LinkParameters(ScriptInfo scriptInfo, List<ParameterInfo> infos)
		{
			var profileParameterInfos = infos
				.Where(x => x.Reference is ProfileParameterID)
				.ToDictionary(x => (x.Reference as ProfileParameterID).Id);

			var profileParameters = scriptInfo.ProfileParameterReferences.Values;

			foreach (var parameter in profileParameters)
			{
				if (!profileParameterInfos.TryGetValue(parameter.ID, out var parameterInfo))
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
							var queue = new Queue<string>(parameter.DiscreetDisplayValues);
							var options = new List<ValueOption>();
							foreach (var discreet in parameter.Discretes)
							{
								if (discreet.GetType() != parameterInfo.ValueType)
								{
									// Tip: warn or fail when this happens
									continue;
								}

								var display = queue.Dequeue();

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

					default:
						throw new NotSupportedException($"Unsupported parameter type {parameter.Type}");
				}
			}

			AssignProfileDefinitionGroups(scriptInfo.ProfileDefinitionReferences, profileParameterInfos);
		}

		private void AssignProfileDefinitionGroups(List<ProfileDefinition> profileDefinitions, Dictionary<Guid, ParameterInfo> parameters)
		{
			ProfileHelper profileHelper = new ProfileHelper(_engine.SendSLNetMessages);

			foreach (var definition in profileDefinitions)
			{
				// Tip: If there are allot of definitions, getting all instances in one call will be more efficient.
				var instances = profileHelper.ProfileInstances.Read(ProfileInstanceExposers.AppliesToID.Equal(definition.ID));

				var presets = new List<GroupPresetOption>(instances.Count);
				foreach (var instance in instances)
				{
					var presetInfo = new PresetGroupDisplayInfo.PresetInfo();
					// Tip: add a check if the option names are unique
					presets.Add(new GroupPresetOption(instance.Name, presetInfo));

					foreach (var value in instance.Values)
					{
						if (!parameters.TryGetValue(value.ParameterID, out var parameter))
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

				var group = new ParameterGroup
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

				foreach (var parameterId in definition.ParameterIDs)
				{
					if (!parameters.TryGetValue(parameterId, out var parameter))
					{
						continue;
					}

					parameter.Group = group;
				}
			}
		}

		private IEnumerable<ParameterInfo> GetIncompleteInfos(IEnumerable<ParameterInfo> infos) => infos.Where(x => x.Value is null);

		private ScriptOutput PerformOrchestrationFromEntryPoint(IReadOnlyDictionary<string, string> metaData, bool askMissingValues)
		{
			ScriptOutput scriptOutput = new ScriptOutput();

			try
			{
				ScriptInfo scriptInfo = GetScriptInfo();

				ScriptInput scriptInput = new ScriptInput();
				if (metaData.TryGetValue(ScriptInputRequestScriptInfoKey, out string serializedScriptInputRequestScriptInfo))
				{
					scriptInput = JsonConvert.DeserializeObject<ScriptInput>(serializedScriptInputRequestScriptInfo);
				}

				_parameterInfos = CreateParameterInfos(scriptInfo, scriptInput);

				if (askMissingValues)
				{
					List<ParameterInfo> incompleteInfos = GetIncompleteInfos(_parameterInfos).ToList();
					if (incompleteInfos.Any())
					{
						GetValuesFromUser(incompleteInfos);
					}
				}

				Orchestrate(_engine);
			}
			catch (Exception e)
			{
				scriptOutput.ExceptionString = e.ToString();
			}

			return scriptOutput;
		}
	}
}