namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration
{
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Tools;

	internal static class OrchestrationAutomationHelper
	{
		public static ExecuteScriptResponseMessage ExecuteGetOrchestrationScriptInfo(IConnection connection, string scriptName)
		{
			var metaData = new Dictionary<string, string>
			{
				[nameof(OrchestrationScriptAction)] = nameof(OrchestrationScriptAction.OrchestrationScriptInfo),
			};

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
			var messageBuilder = new ExecuteScriptMessageBuilder(scriptName);
			messageBuilder.SetCheckSets(false);
			messageBuilder.SetInformationEvent(false);
			messageBuilder.SetSynchronous(true);
			messageBuilder.SetExtendedErrorInfo(true);
			messageBuilder.SetParameters(scriptParams.ToDictionary(param => param.Description, param => param.Value));
			messageBuilder.SetDummies(scriptDummies.ToDictionary(dummy => dummy.Description, dummy => dummy.Value));

			return AutomationHelper.ExecuteAutomationScript(connection, messageBuilder.Build());
		}
	}
}
