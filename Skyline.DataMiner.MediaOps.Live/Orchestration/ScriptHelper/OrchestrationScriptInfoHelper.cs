namespace Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.Advanced;
	using Skyline.DataMiner.Net.Profiles;

	/// <summary>
	/// Helper class to retrieve orchestration script input information.
	/// </summary>
	public class OrchestrationScriptInfoHelper
	{
		private readonly IConnection _connection;
		private readonly ProfileHelper _profileHelper;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationScriptInfoHelper"/> class.
		/// </summary>
		/// <param name="api">Instance of MediaOpsLiveApi.</param>
		internal OrchestrationScriptInfoHelper(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			_connection = api.Connection;
			_profileHelper = new ProfileHelper(_connection.HandleMessages);
		}

		/// <summary>
		/// Request the orchestration script input information for the specified script.
		/// This includes the basic script parameters and dummies, as well as the orchestration profile definitions and parameters.
		/// </summary>
		/// <param name="scriptName">Name of the orchestration script.</param>
		/// <returns>Returns the orchestration script input information for the specified script.</returns>
		public OrchestrationScriptInputInfo GetOrchestrationScriptInputInfo(string scriptName)
		{
			OrchestrationScriptInputInfo result = new OrchestrationScriptInputInfo(scriptName);

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
				result.Elements.Add(new OrchestrationScriptInputElement(inputDummy));
			}

			return result;
		}

		/// <summary>
		/// Get the names of all orchestration scripts.
		/// </summary>
		/// <returns>A list of all orchestration script names.</returns>
		public IEnumerable<string> GetOrchestrationScripts()
		{
			var message = new GetAutomationInfoMessage
			{
				What = (int)AutomationInfoType.ScriptFolders,
			};

			var response = _connection.HandleSingleResponseMessage(message) as GetAutomationInfoResponseMessage;

			var psa = response?.psaRet?.Psa;
			if (psa == null)
			{
				throw new InvalidOperationException("Failed to retrieve orchestration script information.");
			}

			foreach (string[] sa in psa.Select(x => x.Sa))
			{
				if (sa.Length < 2)
				{
					continue;
				}

				var folderPath = sa[0].Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

				if (folderPath.Length >= 2 &&
					String.Equals(folderPath[0], "MediaOps", StringComparison.OrdinalIgnoreCase) &&
					String.Equals(folderPath[1], "OrchestrationScripts", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var script in sa.Skip(1))
					{
						yield return script;
					}
				}
			}
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
			if (resultDictionary.TryGetValue(OrchestrationScriptConstants.ScriptOutputError, out var scriptError))
			{
				throw new InvalidOperationException($"Error during orchestration script info request: " + scriptError);
			}

			if (!resultDictionary.TryGetValue(OrchestrationScriptConstants.OrchestrationScriptInfoRequestScriptInfoKey, out var serializedScriptInfo))
			{
				throw new InvalidOperationException($"Script didn't build the scriptInfo");
			}

			return JsonConvert.DeserializeObject<ScriptInfo>(serializedScriptInfo);
		}
	}
}
