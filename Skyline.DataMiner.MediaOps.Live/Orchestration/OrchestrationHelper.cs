namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;

	public static class OrchestrationHelper
	{
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
			return AutomationHelper.ExecuteAutomationScript(connection, messageBuilder.Build());
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
			metaData[OrchestrationScriptConstants.ScriptInputRequestScriptInfoKey] = JsonConvert.SerializeObject(input);

			messageBuilder.SetEntryPoint(new AutomationEntryPoint
			{
				EntryPointType = AutomationEntryPoint.Types.OnRequestScriptInfo,
				Parameters = new List<object> { new RequestScriptInfoInput { Data = metaData } },
			});

			return AutomationHelper.ExecuteAutomationScript(connection, messageBuilder.Build(), out errorMessages);
		}

		public static ExecuteScriptResponseMessage TryExecuteScript(
			IConnection connection,
			string scriptName,
			List<DmsAutomationScriptParamValue> scriptParams,
			List<DmsAutomationScriptDummyValue> scriptDummies,
			out string[] errorMessages)
		{
			ExecuteScriptMessageBuilder messageBuilder = new(scriptName);
			messageBuilder.SetCheckSets(false);
			messageBuilder.SetInformationEvent(false);
			messageBuilder.SetSynchronous(true);
			messageBuilder.SetExtendedErrorInfo(true);
			messageBuilder.SetParameters(scriptParams.ToDictionary(param => param.Description, param => param.Value));
			messageBuilder.SetDummies(scriptDummies.ToDictionary(dummy => dummy.Description, dummy => dummy.Value));

			return AutomationHelper.ExecuteAutomationScript(connection, messageBuilder.Build(), out errorMessages);
		}
	}
}
