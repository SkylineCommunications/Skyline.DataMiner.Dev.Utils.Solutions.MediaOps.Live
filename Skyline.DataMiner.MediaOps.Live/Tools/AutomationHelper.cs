namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tools
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.ExceptionServices;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions;

	internal static class AutomationHelper
	{
		public static ExecuteScriptResponseMessage ExecuteAutomationScript(IConnection connection, string scriptName, Dictionary<string, string> parameters, bool checkSets = true, bool extendedErrorInfo = true, bool interactive = false, bool synchronous = true, bool informationEvent = false)
		{
			var message = BuildExecuteScriptMessage(scriptName, parameters, checkSets, extendedErrorInfo, interactive, synchronous, informationEvent);

			return ExecuteAutomationScript(connection, message);
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
				ExceptionDispatchInfo.Capture(result.Failure).Throw();
			}

			var response = (ExecuteScriptResponseMessage)result.Messages.Single();

			if (response.HadError)
			{
				throw new ScriptExecutionFailedException(message.ScriptName, response);
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
