namespace CheckMediaOpsLive.Automation
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages;

	public class AutomationExe
	{
		private readonly AutomationExeInfo _automationExe;
		private readonly List<string> _dllRefs = [];
		private readonly List<string> _namespaceRefs = [];
		private readonly List<string> _scriptRefs = [];

		public AutomationExe(AutomationExeInfo automationExe)
		{
			_automationExe = automationExe ?? throw new ArgumentNullException(nameof(automationExe));

			if (!String.IsNullOrEmpty(automationExe.CSharpDllRefs))
			{
				var dllRefs = automationExe.CSharpDllRefs.Split([';'], StringSplitOptions.RemoveEmptyEntries);
				_dllRefs.AddRange(dllRefs);
			}

			if (!String.IsNullOrEmpty(automationExe.CSharpNamespaceRefs))
			{
				var namespaceRefs = automationExe.CSharpNamespaceRefs.Split([';'], StringSplitOptions.RemoveEmptyEntries);
				_namespaceRefs.AddRange(namespaceRefs);
			}

			if (!String.IsNullOrEmpty(automationExe.CSharpScriptRefs))
			{
				var scriptRefs = automationExe.CSharpScriptRefs.Split([';'], StringSplitOptions.RemoveEmptyEntries);
				_scriptRefs.AddRange(scriptRefs);
			}
		}

		public string Code => _automationExe.Value;

		public bool DebugMode => _automationExe.CSharpDebugMode;

		public bool PreCompile => _automationExe.PreCompile;

		public IReadOnlyCollection<string> DllRefs => _dllRefs;

		public IReadOnlyCollection<string> NamespaceRefs => _namespaceRefs;

		public IReadOnlyCollection<string> ScriptRefs => _scriptRefs;
	}
}
