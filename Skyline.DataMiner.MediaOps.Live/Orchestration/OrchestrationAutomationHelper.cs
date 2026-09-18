namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Tools;

	internal static class OrchestrationAutomationHelper
	{
		public static ExecuteScriptResponseMessage ExecuteGetOrchestrationScriptInfo(IConnection connection, string scriptName)
		{
			return ExecuteGetOrchestrationScriptInfo(connection, scriptName, null);
		}

		public static ExecuteScriptResponseMessage ExecuteGetOrchestrationScriptInfo(IConnection connection, string scriptName, OrchestrationInputValues providedValues)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			if (String.IsNullOrEmpty(scriptName))
			{
				throw new ArgumentException($"'{nameof(scriptName)}' cannot be null or empty.", nameof(scriptName));
			}

			var metaData = new Dictionary<string, string>
			{
				[nameof(OrchestrationScriptAction)] = nameof(OrchestrationScriptAction.OrchestrationScriptInfo),
			};

			if (providedValues != null && providedValues.Count > 0)
			{
				var input = new OrchestrationScriptInput { InputValues = providedValues.ToDictionary() };
				metaData[OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey] = JsonConvert.SerializeObject(input);
			}

			var messageBuilder = new ExecuteScriptMessageBuilder(scriptName);
			messageBuilder.SetCheckSets(false);
			messageBuilder.SetInformationEvent(false);
			messageBuilder.SetSynchronous(true);
			messageBuilder.SetExtendedErrorInfo(true);
			messageBuilder.SetEntryPoint(new AutomationEntryPoint
			{
				EntryPointType = AutomationEntryPoint.Types.OnRequestScriptInfo,
				Parameters = [new RequestScriptInfoInput { Data = metaData }],
			});

			return AutomationHelper.ExecuteAutomationScript(connection, messageBuilder.Build());
		}

		public static ExecuteScriptResponseMessage ExecuteOrchestrationScript(
			IConnection connection,
			string scriptName,
			List<DmsAutomationScriptParamValue> scriptParams,
			List<DmsAutomationScriptDummyValue> scriptDummies,
			OrchestrationScriptInput input)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			if (String.IsNullOrEmpty(scriptName))
			{
				throw new ArgumentException($"'{nameof(scriptName)}' cannot be null or empty.", nameof(scriptName));
			}

			if (scriptParams is null)
			{
				throw new ArgumentNullException(nameof(scriptParams));
			}

			if (scriptDummies is null)
			{
				throw new ArgumentNullException(nameof(scriptDummies));
			}

			if (input is null)
			{
				throw new ArgumentNullException(nameof(input));
			}

			var metaData = new Dictionary<string, string>
			{
				[nameof(OrchestrationScriptAction)] = nameof(OrchestrationScriptAction.PerformOrchestration),
				[OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey] = JsonConvert.SerializeObject(input),
			};

			var messageBuilder = new ExecuteScriptMessageBuilder(scriptName);
			messageBuilder.SetCheckSets(false);
			messageBuilder.SetInformationEvent(false);
			messageBuilder.SetSynchronous(true);
			messageBuilder.SetExtendedErrorInfo(true);
			messageBuilder.SetParameters(scriptParams.ToDictionary(param => param.Description, param => param.Value));
			messageBuilder.SetDummies(scriptDummies.ToDictionary(dummy => dummy.Description, dummy => dummy.Value));
			messageBuilder.SetEntryPoint(new AutomationEntryPoint
			{
				EntryPointType = AutomationEntryPoint.Types.OnRequestScriptInfo,
				Parameters = [new RequestScriptInfoInput { Data = metaData }],
			});

			return AutomationHelper.ExecuteAutomationScript(connection, messageBuilder.Build());
		}

		public static ExecuteScriptResponseMessage ExecuteScript(
			IConnection connection,
			string scriptName,
			List<DmsAutomationScriptParamValue> scriptParams,
			List<DmsAutomationScriptDummyValue> scriptDummies)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			if (String.IsNullOrEmpty(scriptName))
			{
				throw new ArgumentException($"'{nameof(scriptName)}' cannot be null or empty.", nameof(scriptName));
			}

			if (scriptParams is null)
			{
				throw new ArgumentNullException(nameof(scriptParams));
			}

			if (scriptDummies is null)
			{
				throw new ArgumentNullException(nameof(scriptDummies));
			}

			var messageBuilder = new ExecuteScriptMessageBuilder(scriptName);
			messageBuilder.SetCheckSets(false);
			messageBuilder.SetInformationEvent(false);
			messageBuilder.SetSynchronous(true);
			messageBuilder.SetExtendedErrorInfo(true);
			messageBuilder.SetParameters(scriptParams.ToDictionary(param => param.Description, param => param.Value));
			messageBuilder.SetDummies(scriptDummies.ToDictionary(dummy => dummy.Description, dummy => dummy.Value));

			return AutomationHelper.ExecuteAutomationScript(connection, messageBuilder.Build());
		}

		/// <summary>
		/// Launches the <see cref="Constants.OrchestrationScriptName"/> script as a fire-and-forget deferred task to execute the events with the given IDs, without waiting for the orchestration to complete.
		/// </summary>
		/// <param name="connection">The connection used to launch the script.</param>
		/// <param name="eventIds">The IDs of the events to orchestrate.</param>
		public static void ExecuteEventsInBackground(
			IConnection connection,
			List<Guid> eventIds)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			if (eventIds is null)
			{
				throw new ArgumentNullException(nameof(eventIds));
			}

			if (eventIds.Count == 0)
			{
				return;
			}

			var messageBuilder = new ExecuteScriptMessageBuilder(Constants.OrchestrationScriptName);
			messageBuilder.SetCheckSets(false);
			messageBuilder.SetInformationEvent(false);
			messageBuilder.SetSynchronous(false);
			messageBuilder.SetParameters(new Dictionary<string, string>
			{
				[Constants.OrchestrationScriptEventIdsParameter] = JsonConvert.SerializeObject(eventIds),
			});

			// Fire-and-forget: the deferred script runs the orchestration and reports per-event failures via the job state.
			AutomationHelper.ExecuteAutomationScriptInBackground(connection, messageBuilder.Build());
		}
	}
}
