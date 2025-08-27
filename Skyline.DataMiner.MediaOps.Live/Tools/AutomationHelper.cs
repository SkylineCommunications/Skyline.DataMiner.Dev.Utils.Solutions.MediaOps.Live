namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;

	public static class AutomationHelper
	{
		public static ExecuteScriptResponseMessage ExecuteAutomationScript(IConnection connection, string scriptName, Dictionary<string, string> parameters, bool checkSets = true, bool extendedErrorInfo = true, bool interactive = false, bool synchronous = true, bool informationEvent = false)
		{
			var request = BuildExecuteScriptMessage(scriptName, parameters, checkSets, extendedErrorInfo, interactive, synchronous, informationEvent);

			var progress = connection.Async.Launch(request);

			var result = progress.WaitForAsyncResponse(timeout: 5 * 60);

			if (result == null)
			{
				throw new DataMinerException("No response received");
			}

			if (result.Failure != null)
			{
				throw result.Failure;
			}

			var response = (ExecuteScriptResponseMessage)result.Messages.Single();

			if (response.HadError)
			{
				throw new DataMinerException("Script execution failed: " + String.Join(", ", response.ErrorMessages));
			}

			return response;
		}

		public static ExecuteScriptResponseMessage ExecuteConnectionHandlerScript(IConnection connection, string scriptName, Dictionary<string, string> parameters)
		{
			ExecuteScriptMessageBuilder messageBuilder = new(scriptName);
			messageBuilder.SetCheckSets(true);
			messageBuilder.SetParameters(parameters);
			messageBuilder.SetExtendedErrorInfo(true);
			messageBuilder.SetInformationEvent(false);
			messageBuilder.SetSynchronous(true);
			return ExecuteAutomationScript(connection, messageBuilder.Build());
		}

		public static ExecuteScriptResponseMessage ExecuteGetOrchestrationScriptInfo(IConnection connection, string scriptName)
		{
			ExecuteScriptMessageBuilder messageBuilder = new(scriptName);
			messageBuilder.SetCheckSets(false);
			messageBuilder.SetInformationEvent(false);
			messageBuilder.SetSynchronous(true);

			var metaData = new Dictionary<string, string>();
			metaData[nameof(OrchestrationScriptAction)] = nameof(OrchestrationScriptAction.OrchestrationScriptInfo);
			messageBuilder.SetEntryPoint(new AutomationEntryPoint
			{
				EntryPointType = AutomationEntryPoint.Types.OnRequestScriptInfo,
				Parameters = new List<object> { new RequestScriptInfoInput { Data = metaData } },
			});
			return ExecuteAutomationScript(connection, messageBuilder.Build());
		}

		public static ExecuteScriptResponseMessage TryExecuteOrchestrationScript(
			IConnection connection,
			string scriptName,
			List<DmsAutomationScriptParamValue> scriptParams,
			List<DmsAutomationScriptDummyValue> scriptDummies,
			OrchestrationScriptInput input,
			out string[] errorMessages)
		{
			ExecuteScriptMessageBuilder messageBuilder = new(scriptName);
			messageBuilder.SetCheckSets(false);
			messageBuilder.SetInformationEvent(false);
			messageBuilder.SetSynchronous(true);
			messageBuilder.SetExtendedErrorInfo(true);
			messageBuilder.SetParameters(scriptParams.ToDictionary(param => param.Description, param => param.Value));
			messageBuilder.SetDummies(scriptDummies.ToDictionary(dummy => dummy.Description, dummy => dummy.Value));

			var metaData = new Dictionary<string, string>();
			metaData[nameof(OrchestrationScriptAction)] = nameof(OrchestrationScriptAction.PerformOrchestration);
			metaData[OrchestrationScript.ScriptInputRequestScriptInfoKey] = JsonConvert.SerializeObject(input);

			messageBuilder.SetEntryPoint(new AutomationEntryPoint
			{
				EntryPointType = AutomationEntryPoint.Types.OnRequestScriptInfo,
				Parameters = new List<object> { new RequestScriptInfoInput { Data = metaData } },
			});

			return ExecuteAutomationScript(connection, messageBuilder.Build(), out errorMessages);
		}

		public static ExecuteScriptResponseMessage ExecuteAutomationScript(IConnection connection, ExecuteScriptMessage message)
		{
			var progress = connection.Async.Launch(message);

			var result = progress.WaitForAsyncResponse(timeout: 5 * 60);

			if (result == null)
			{
				throw new DataMinerException("No response received");
			}

			if (result.Failure != null)
			{
				throw result.Failure;
			}

			var response = (ExecuteScriptResponseMessage)result.Messages.Single();

			if (response.HadError)
			{
				throw new DataMinerException("Script execution failed: " + String.Join(", ", response.ErrorMessages));
			}

			return response;
		}

		public static ExecuteScriptResponseMessage ExecuteAutomationScript(IConnection connection, ExecuteScriptMessage message, out string[] errorMessages)
		{
			errorMessages = [];
			var progress = connection.Async.Launch(message);

			var result = progress.WaitForAsyncResponse(timeout: 5 * 60);

			if (result == null)
			{
				throw new DataMinerException("No response received");
			}

			if (result.Failure != null)
			{
				throw result.Failure;
			}

			var response = (ExecuteScriptResponseMessage)result.Messages.Single();

			if (response.HadError)
			{
				errorMessages = response.ErrorMessages;
			}

			return response;
		}

		private static ExecuteScriptMessage BuildExecuteScriptMessage(string scriptName, Dictionary<string, string> parameters, bool checkSets, bool extendedErrorInfo, bool interactive, bool synchronous, bool informationEvent)
		{
			var options = new List<string>
			{
				$"CHECKSETS:{(checkSets ? "TRUE" : "FALSE")}",
				$"DEFER:{(!synchronous ? "TRUE" : "FALSE")}",
			};

			if (extendedErrorInfo)
			{
				options.Add("EXTENDED_ERROR_INFO");
			}

			if (interactive)
			{
				options.Add("INTERACTIVE");
			}

			if (!informationEvent)
			{
				options.Add("SKIP_STARTED_INFO_EVENT:TRUE");
			}

			foreach (var parameter in parameters)
			{
				options.Add($"PARAMETERBYNAME:{parameter.Key}:{parameter.Value}");
			}

			var request = new ExecuteScriptMessage
			{
				ScriptName = scriptName,
				Options = new SA(options.ToArray()),
				DataMinerID = -1,
				HostingDataMinerID = -1,
			};

			return request;
		}
	}
}
