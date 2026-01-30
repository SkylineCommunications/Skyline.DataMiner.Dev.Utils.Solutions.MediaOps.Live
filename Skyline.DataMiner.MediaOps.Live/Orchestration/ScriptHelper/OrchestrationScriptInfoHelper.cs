namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.Advanced;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects;

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
			var script = _connection.GetDms().GetScript(scriptName)
				?? throw new InvalidOperationException("The specified script was not found.");

			if (!IsValidOrchestrationScript(script))
			{
				throw new InvalidOperationException("The specified script is not a valid orchestration script.");
			}

			var result = new OrchestrationScriptInputInfo(scriptName);

			foreach (var inputParam in script.Parameters)
			{
				result.Parameters.Add(new OrchestrationScriptInputParameter(inputParam.Description));
			}

			foreach (var inputDummy in script.Dummies)
			{
				result.Elements.Add(new OrchestrationScriptInputElement(inputDummy));
			}

			var hasCsharpCode = script.CSharpBlocks.Any();
			if (!hasCsharpCode)
			{
				// We have everything we need from the script itself, so we can return.
				return result;
			}

			if (TryGetScriptOrchestrationInfo(scriptName, out var scriptOrchestrationInfo))
			{
				if (scriptOrchestrationInfo.ProfileDefinitions.Any())
				{
					var profileDefinitionId = scriptOrchestrationInfo.ProfileDefinitions.First();
					var profileDefinition = _profileHelper.ProfileDefinitions.Read(ProfileDefinitionExposers.ID.Equal(profileDefinitionId)).FirstOrDefault();
					result.ProfileDefinition = profileDefinition;
				}

				foreach (var profileParameter in scriptOrchestrationInfo.ProfileParameters)
				{
					var orchestrationParam = new OrchestrationScriptInputParameter(profileParameter.Key, profileParameter.Value);
					orchestrationParam.LoadLinkedProfileParameter(_profileHelper);
					result.Parameters.Add(orchestrationParam);
				}
			}

			return result;
		}

		/// <summary>
		/// Get the names of all orchestration scripts.
		/// All scripts that are located in the "MediaOps/OrchestrationScripts" folder are considered orchestration scripts.
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

		internal static bool IsValidOrchestrationScript(IDmsAutomationScript script)
		{
			if (script == null)
			{
				throw new ArgumentNullException(nameof(script));
			}

			if (!script.Folder.StartsWith("MediaOps/OrchestrationScripts", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return true;
		}

		private bool TryGetScriptOrchestrationInfo(string scriptName, out OrchestrationScriptInfo orchestrationScriptInfo)
		{
			try
			{
				var response = OrchestrationAutomationHelper.ExecuteGetOrchestrationScriptInfo(_connection, scriptName);

				if (response != null &&
					!response.HadError &&
					response.EntryPointResult?.Result is RequestScriptInfoOutput scriptInfoOutput)
				{
					orchestrationScriptInfo = ParseScriptInfo(scriptInfoOutput.Data);
					return true;
				}
			}
			catch (Exception)
			{
				// Swallow exception and return false.
				// This can happen when the OnRequestScriptInfo entry point doesn't exist.
			}

			orchestrationScriptInfo = null;
			return false;
		}

		private static OrchestrationScriptInfo ParseScriptInfo(IReadOnlyDictionary<string, string> resultDictionary)
		{
			if (resultDictionary.TryGetValue(OrchestrationScriptConstants.ScriptOutputError, out var scriptError))
			{
				throw new InvalidOperationException($"Error during orchestration script info request: " + scriptError);
			}

			if (!resultDictionary.TryGetValue(OrchestrationScriptConstants.OrchestrationScriptInfoRequestScriptInfoKey, out var serializedScriptInfo))
			{
				throw new InvalidOperationException($"Script didn't build the scriptInfo");
			}

			return JsonConvert.DeserializeObject<OrchestrationScriptInfo>(serializedScriptInfo);
		}
	}
}
