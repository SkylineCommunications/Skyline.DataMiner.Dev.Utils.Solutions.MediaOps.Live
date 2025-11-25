namespace Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Profiles;

	/// <summary>
	/// Helper class to retrieve orchestration script input information.
	/// </summary>
	public class OrchestrationScriptInfoHelper
	{
		private readonly ProfileHelper _profileHelper;
		private readonly IConnection _connection;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationScriptInfoHelper"/> class.
		/// </summary>
		/// <param name="connection">Instance of SLNet connection.</param>
		internal OrchestrationScriptInfoHelper(IConnection connection)
		{
			_connection = connection;
			_profileHelper = new ProfileHelper(connection.HandleMessages);
		}

		/// <summary>
		/// Request the orchestration script input information for the specified script.
		/// This includes the basic script parameters and dummies, as well as the orchestration profile definitions and parameters.
		/// </summary>
		/// <param name="scriptName">Name of the orchestration script.</param>
		/// <returns>Returns the orchestration script input information for the specified script.</returns>
		public OrchestrationScriptInputInfo GetOrchestrationScriptInputInfo(string scriptName)
		{
			OrchestrationScriptInputInfo result = new(scriptName);

			ScriptInfo scriptOrchestrationInfo = GetScriptOrchestrationInfo(scriptName);

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

			foreach (AutomationProtocolInfo inputDummy in scriptInputInfoResponse.Dummies)
			{
				result.Elements.Add(new OrchestrationScriptInputElement { ProtocolInfo = inputDummy });
			}

			return result;
		}

		/// <summary>
		/// Get the names of all orchestration scripts.
		/// </summary>
		/// <returns>A list of all orchestration script names.</returns>
		public List<string> GetOrchestrationScripts()
		{
			IDms dms = _connection.GetDms();
			return dms.GetScripts().Where(IsOrchestrationScript).Select(script => script.Name).ToList();
		}

		internal static bool IsOrchestrationScript(IDmsAutomationScript script)
		{
			if (script == null)
			{
				throw new ArgumentNullException(nameof(script));
			}

			if (!script.Folder.StartsWith("MediaOps/OrchestrationScripts"))
			{
				return false;
			}

			return script.CSharpBlocks.Count() == 1;
		}

		private GetScriptInfoResponseMessage GetScriptInputInfo(string scriptName)
		{
			return (GetScriptInfoResponseMessage)_connection.HandleSingleResponseMessage(new GetScriptInfoMessage(scriptName));
		}

		private ScriptInfo GetScriptOrchestrationInfo(string scriptName)
		{
			var response = OrchestrationAutomationHelper.ExecuteGetOrchestrationScriptInfo(_connection, scriptName);

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
