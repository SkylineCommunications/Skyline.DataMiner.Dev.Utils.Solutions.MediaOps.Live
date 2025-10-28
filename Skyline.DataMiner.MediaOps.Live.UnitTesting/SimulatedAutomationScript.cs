namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;

	public class SimulatedAutomationScript
	{
		private readonly List<string> _inputParameters;
		private readonly List<string> _inputDummies;
		private readonly ScriptInfo _orchestrationScriptInfo;

		public SimulatedAutomationScript(string name) : this(name, [], [], new ScriptInfo())
		{
		}

		public SimulatedAutomationScript(string name, List<string> inputParams, List<string> inputDummies, ScriptInfo orchestrationScriptInfo)
		{
			_inputParameters = inputParams;
			_inputDummies = inputDummies;
			_orchestrationScriptInfo = orchestrationScriptInfo;
			Name = name;
		}

		public List<string> Parameters => _inputParameters;

		public List<string> Dummies => _inputDummies;

		public ScriptInfo OrchestrationScriptInfo => _orchestrationScriptInfo;

		public string Folder { get; set; } = String.Empty;

		public string Name { get; set; }
	}
}
