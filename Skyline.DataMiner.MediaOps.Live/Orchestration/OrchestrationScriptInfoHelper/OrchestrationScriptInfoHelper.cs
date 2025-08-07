namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Newtonsoft.Json;

	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Profiles;

	public class OrchestrationScriptInfoHelper
	{
		private ProfileHelper _profileHelper;
		private readonly IConnection _connection;

		public OrchestrationScriptInfoHelper(IConnection connection)
		{
			_profileHelper = new ProfileHelper(connection.HandleMessages);
		}

		public OrchestrationScriptInputInfo GetOrchestrationScriptInputInfo(string scriptName)
		{
			var result = new OrchestrationScriptInputInfo();
			result.ScriptName = scriptName;

			var scriptOrchestrationInfo = GetScriptOrchestrationInfo(scriptName);

			if (scriptOrchestrationInfo.ProfileDefinitions.Any())
			{
				result.ProfileDefinition = scriptOrchestrationInfo.ProfileDefinitions.First();
			}

			foreach (KeyValuePair<string, Guid> profileParameter in scriptOrchestrationInfo.ProfileParameters)
			{
				var orchestrationParam = new OrchestrationScriptInputParameter(profileParameter.Key, profileParameter.Value);
				orchestrationParam.LoadLinkedProfileParameter(_profileHelper);
				result.Parameters.Add(orchestrationParam);
			}

			GetScriptInfoResponseMessage scriptInputInfoResponse = GetScriptInputInfo(scriptName);
			foreach (AutomationParameterInfo inputParam in scriptInputInfoResponse.Parameters)
			{
				result.Parameters.Add(new OrchestrationScriptInputParameter(inputParam.Description, Guid.Empty));
			}

			result.Elements.AddRange(scriptInputInfoResponse.Dummies);

			return result;
		}

		public GetScriptInfoResponseMessage GetScriptInputInfo(string scriptName)
		{
			return (GetScriptInfoResponseMessage)_connection.HandleSingleResponseMessage(new GetScriptInfoMessage(scriptName));
		}

		public ScriptInfo GetScriptOrchestrationInfo(string scriptName)
		{
			var response = AutomationHelper.ExecuteGetOrchestrationScriptInfoScript(_connection, scriptName);

			if (response?.EntryPointResult?.Result == null || response.HadError)
			{
				throw new InvalidOperationException("Script not found or an error occurred while retrieving script information.");
			}

			var scriptInfoOutput = (RequestScriptInfoOutput)response.EntryPointResult.Result;

			if (scriptInfoOutput?.Data is not IReadOnlyDictionary<string, string> returnedInfo)
			{
				throw new InvalidOperationException("Invalid result received");
			}

			return ParseScriptInfo(returnedInfo);
		}

		private static ScriptInfo ParseScriptInfo(IReadOnlyDictionary<string, string> resultDictionary)
		{
			if (!resultDictionary.TryGetValue("OrchestrationScriptInfo", out var serializedScriptInfo))
			{
				throw new InvalidOperationException($"Script didn't build the scriptInfo");
			}

			return JsonConvert.DeserializeObject<ScriptInfo>(serializedScriptInfo);
		}
	}
}
