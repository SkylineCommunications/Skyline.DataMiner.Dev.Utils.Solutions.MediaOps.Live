namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System.Collections.Generic;

	public class SimulatedAutomationScript
	{
		private readonly List<string> _inputParameters;
		private readonly List<string> _inputDummies;

		public SimulatedAutomationScript(string name) : this(name, [], [])
		{
		}

		public SimulatedAutomationScript(string name, List<string> inputParams, List<string> inputDummies)
		{
			_inputParameters = inputParams;
			_inputDummies = inputDummies;
			Name = name;
		}

		public List<string> Parameters => _inputParameters;

		public List<string> Dummies => _inputDummies;

		public string Name { get; set; }
	}
}
