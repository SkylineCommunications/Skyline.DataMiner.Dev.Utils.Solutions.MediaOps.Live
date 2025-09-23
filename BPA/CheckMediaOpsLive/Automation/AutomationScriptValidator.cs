namespace CheckMediaOpsLive.Automation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	public class AutomationScriptValidator(IConnection connection)
	{
		private readonly IConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

		public bool ValidateScript(string scriptName, out IReadOnlyCollection<string> errors)
		{
			var automationScript = LoadScript(scriptName);

			var hasErrors = false;
			var errorList = new HashSet<string>();

			foreach (var automationExe in automationScript.ScriptExes)
			{
				if (!ValidateScriptExe(automationScript, automationExe, out var exeErrors))
				{
					hasErrors = true;
					errorList.UnionWith(exeErrors);
				}
			}

			errors = errorList;
			return !hasErrors;
		}

		private AutomationScript LoadScript(string scriptName)
		{
			var message = new GetScriptInfoMessage(scriptName);
			var response = (GetScriptInfoResponseMessage)_connection.HandleSingleResponseMessage(message);

			return new AutomationScript(response);
		}

		private bool ValidateScriptExe(AutomationScript automationScript, AutomationExe automationExe, out IReadOnlyCollection<string> errors)
		{
			var errorList = new List<string>();

			var message = new CheckAutomationCSharpSyntaxMessage
			{
				ScriptName = automationScript.Name,
				Code = automationExe.Code,
				DataMinerID = -1,
				HostingDataMinerID = -1,
				DebugMode = automationExe.DebugMode,
				PreCompile = automationExe.PreCompile,
				DllRefs = automationExe.DllRefs.ToArray(),
				NameSpaceRefs = automationExe.NamespaceRefs.ToArray(),
				ScriptRefs = automationExe.ScriptRefs.ToArray(),
			};

			var response = (CheckAutomationCSharpSyntaxResponse)_connection.HandleSingleResponseMessage(message);

			foreach (var error in response.Errors)
			{
				var parts = error.Split(';');

				if (parts[2] == "F")
				{
					// error detected
					errorList.Add(error);
				}
			}

			errors = errorList;
			return errorList.Count == 0;
		}
	}
}
