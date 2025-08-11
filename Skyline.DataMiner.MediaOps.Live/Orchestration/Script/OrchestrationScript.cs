namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.Dialogs;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

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

			var scriptInfo = GetScriptInfo();

			var parameterInfos = CreateParameterInfos(scriptInfo, new ValueInfo());

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
					var scriptOutput = PerformOrchestrationFromEntryPoint(metaData, orchestrationScriptAction == OrchestrationScriptAction.PerformOrchestrationAskMissingValues);
					return new Dictionary<string, string> { { ScriptOutputRequestScriptInfoKey, scriptOutput == null ? null : JsonConvert.SerializeObject(scriptOutput) } };
				}

				default:
					throw new NotSupportedException($"No support for orchestration script action {orchestrationScriptAction}");
			}
		}

		private ScriptInfo GetScriptInfo()
		{
			var info = new ScriptInfo();
			foreach (IOrchestrationParameters orchestrationParameters in GetParameters())
			{
				if (orchestrationParameters is OrchestrationProfileDefinition)
				{
					info.ProfileDefinitions.Add(orchestrationParameters.GetDefinition(_engine));
				}

				foreach (KeyValuePair<string, Guid> keyValuePair in orchestrationParameters.GetParameterInformation(_engine))
				{
					info.ProfileParameters.Add(keyValuePair.Key, keyValuePair.Value);
				}
			}

			return info;
		}

		public void GetValuesFromUser(List<ParameterInfo> infos)
		{
			var controller = new InteractiveController(_engine);
			var dialog = new GetOrchestrationValuesDialog(_engine, infos);

			dialog.Button.Pressed += (sender, args) =>
			{
				controller.Stop();
				dialog.UpdateValues();
			};

			controller.ShowDialog(dialog);
		}

		public List<ParameterInfo> CreateParameterInfos(ScriptInfo scriptInfo, ValueInfo valueInfo)
		{
			var parameterInfos = new List<ParameterInfo>(scriptInfo.ProfileParameters.Count);

			foreach (var profileParameter in scriptInfo.ProfileParameters)
			{
				var reference = new ProfileParameterID(profileParameter.Value);
				var info = new ParameterInfo
				{
					Id = profileParameter.Key,
					Reference = reference,
					Value = valueInfo.ProfileParameterValues.TryGetValue(profileParameter.Value, out var value) ? value : null,
				};

				parameterInfos.Add(info);
			}

			return parameterInfos;
		}

		public IEnumerable<ParameterInfo> GetIncompleteInfos(IEnumerable<ParameterInfo> infos) => infos.Where(x => x.Value is null);

		private ScriptOutput PerformOrchestrationFromEntryPoint(IReadOnlyDictionary<string, string> metaData, bool askMissingValues)
		{
			var scriptOutput = new ScriptOutput();

			try
			{
				var scriptInfo = GetScriptInfo();

				ScriptInput scriptInput = null;
				if (metaData.TryGetValue(ScriptInputRequestScriptInfoKey, out var serializedScriptInputRequestScriptInfo))
				{
					scriptInput = JsonConvert.DeserializeObject<ScriptInput>(serializedScriptInputRequestScriptInfo);
				}

				var parameterInfos = CreateParameterInfos(scriptInfo, scriptInput?.ValueInfo ?? new ValueInfo());

				if (askMissingValues)
				{
					var incompleteInfos = GetIncompleteInfos(parameterInfos).ToList();
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