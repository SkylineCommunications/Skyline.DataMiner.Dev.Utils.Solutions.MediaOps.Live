namespace Skyline.DataMiner.MediaOps.Live.UnitTesting.Simulation
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;

	public class SimulatedAutomationScript
	{
		private readonly ICollection<string> _inputParameters;
		private readonly ICollection<string> _inputDummies;
		private readonly ScriptInfo _orchestrationScriptInfo;

		public SimulatedAutomationScript(string name) : this(name, [], [], new ScriptInfo())
		{
		}

		public SimulatedAutomationScript(string name, ICollection<string> inputParams, ICollection<string> inputDummies, ScriptInfo orchestrationScriptInfo)
		{
			_inputParameters = inputParams;
			_inputDummies = inputDummies;
			_orchestrationScriptInfo = orchestrationScriptInfo;
			Name = name;
		}

		public ICollection<string> Parameters => _inputParameters;

		public ICollection<string> Dummies => _inputDummies;

		public ScriptInfo OrchestrationScriptInfo => _orchestrationScriptInfo;

		public string Folder { get; set; } = String.Empty;

		public string Name { get; set; }
	}
}
