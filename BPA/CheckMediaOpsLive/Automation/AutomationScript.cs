namespace CheckMediaOpsLive.Automation
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages;

	public class AutomationScript
	{
		private readonly GetScriptInfoResponseMessage _automationInfo;
		private readonly List<AutomationExe> _scriptExes = [];

		public AutomationScript(GetScriptInfoResponseMessage automationInfo)
		{
			_automationInfo = automationInfo ?? throw new ArgumentNullException(nameof(automationInfo));

			foreach (var automationExe in automationInfo.Exes)
			{
				if (automationExe.Type != AutomationExeType.CSharpCode)
				{
					continue;
				}

				_scriptExes.Add(new AutomationExe(automationExe));
			}
		}

		public string Name => _automationInfo.Name;

		public IReadOnlyCollection<AutomationExe> ScriptExes => _scriptExes;
	}
}
