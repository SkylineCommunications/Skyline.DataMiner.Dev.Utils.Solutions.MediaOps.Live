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
		public static readonly string OrchestrationScriptActionRequestScriptInfoKey = nameof(OrchestrationScriptAction);
		public static readonly string OrchestrationScriptInfoRequestScriptInfoKey = "OrchestrationScriptInfo";
		public static readonly string ScriptInputRequestScriptInfoKey = "OrchestrationScriptInput";
		public static readonly string ScriptOutputRequestScriptInfoKey = "OrchestrationScriptOutput";

		private IEngine _engine;

		public abstract void Orchestrate(IEngine engine);

		public abstract IEnumerable<IOrchestrationParameters> GetParameters();

		[AutomationEntryPoint(AutomationEntryPointType.Types.OnRequestScriptInfo)]
		public RequestScriptInfoOutput OnRequestScriptInfoRequest(IEngine engine, RequestScriptInfoInput inputData)
		{
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

		private void RunSafe(IEngine engine)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));

			ScriptInfo scriptInfo = GetScriptInfo();

			List<ParameterInfo> parameterInfos = CreateParameterInfos(scriptInfo, new ValueInfo());

			if (GetIncompleteInfos(parameterInfos).Any())
			{
				GetValuesFromUser(parameterInfos);
			}

			Orchestrate(engine);
		}

		public Dictionary<string, string> HandleRequestInfoEntryPoint(IReadOnlyDictionary<string, string> metaData)
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
					info.ProfileDefinitions.Add(((OrchestrationProfileDefinition)orchestrationParameters).GetDefinitionReference(_engine));
				}

				foreach (KeyValuePair<string, Parameter> keyValuePair in orchestrationParameters.GetParameterReferences(_engine))
				{
					info.ProfileParameters.Add(keyValuePair.Key, keyValuePair.Value);
				}
			}

			return info;
		}

		public void GetValuesFromUser(List<ParameterInfo> infos)
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

		public List<ParameterInfo> CreateParameterInfos(ScriptInfo scriptInfo, ValueInfo valueInfo)
		{
			List<ParameterInfo> parameterInfos = new List<ParameterInfo>(scriptInfo.ProfileParameters.Count);

			foreach (KeyValuePair<string, Parameter> profileParameter in scriptInfo.ProfileParameters)
			{
				ProfileParameterID reference = new ProfileParameterID(profileParameter.Value.ID);
				ParameterInfo info = new ParameterInfo
				{
					Id = profileParameter.Key,
					Reference = reference,
					Value = valueInfo.ProfileParameterValues.TryGetValue(profileParameter.Value.ID, out object value) ? value : null,
				};

				parameterInfos.Add(info);
			}

			LinkParameters(scriptInfo, parameterInfos);

			return parameterInfos;
		}

		public void LinkParameters(ScriptInfo scriptInfo, List<ParameterInfo> infos)
		{
			var profileParameterInfos = infos
				.Where(x => x.Reference is ProfileParameterID)
				.ToDictionary(x => (x.Reference as ProfileParameterID).Id);

			var profileParameters = scriptInfo.ProfileParameters.Values;

			foreach (var parameter in profileParameters)
			{
				if (!profileParameterInfos.TryGetValue(parameter.ID, out var parameterInfo))
				{
					throw new InvalidOperationException($"Parameter {parameter.ID} wasn't requested");
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

			// Tip: add checks for missing parameters

			AssignProfileDefinitionGroups(scriptInfo.ProfileDefinitions, profileParameterInfos);
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

		public IEnumerable<ParameterInfo> GetIncompleteInfos(IEnumerable<ParameterInfo> infos) => infos.Where(x => x.Value is null);

		private ScriptOutput PerformOrchestrationFromEntryPoint(IReadOnlyDictionary<string, string> metaData, bool askMissingValues)
		{
			ScriptOutput scriptOutput = new ScriptOutput();

			try
			{
				ScriptInfo scriptInfo = GetScriptInfo();

				ScriptInput scriptInput = null;
				if (metaData.TryGetValue(ScriptInputRequestScriptInfoKey, out string serializedScriptInputRequestScriptInfo))
				{
					scriptInput = JsonConvert.DeserializeObject<ScriptInput>(serializedScriptInputRequestScriptInfo);
				}

				List<ParameterInfo> parameterInfos = CreateParameterInfos(scriptInfo, scriptInput?.ValueInfo ?? new ValueInfo());

				if (askMissingValues)
				{
					List<ParameterInfo> incompleteInfos = GetIncompleteInfos(parameterInfos).ToList();
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