namespace Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting.Simulation
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects;

	public class SimulatedAutomationScript
	{
		private readonly ICollection<string> _inputParameters;
		private readonly ICollection<string> _inputDummies;
		private readonly Func<OrchestrationInputValues, OrchestrationScriptInfo> _orchestrationScriptInfoResolver;

		public SimulatedAutomationScript(string name) : this(name, [], [], new OrchestrationScriptInfo())
		{
		}

		public SimulatedAutomationScript(string name, ICollection<string> inputParams, ICollection<string> inputDummies, OrchestrationScriptInfo orchestrationScriptInfo)
			: this(name, inputParams, inputDummies, _ => orchestrationScriptInfo)
		{
		}

		public SimulatedAutomationScript(string name, ICollection<string> inputParams, ICollection<string> inputDummies, Func<OrchestrationInputValues, OrchestrationScriptInfo> orchestrationScriptInfoResolver)
		{
			_inputParameters = inputParams;
			_inputDummies = inputDummies;
			_orchestrationScriptInfoResolver = orchestrationScriptInfoResolver ?? throw new ArgumentNullException(nameof(orchestrationScriptInfoResolver));
			Name = name;
		}

		public ICollection<string> Parameters => _inputParameters;

		public ICollection<string> Dummies => _inputDummies;

		public OrchestrationScriptInfo OrchestrationScriptInfo => GetOrchestrationScriptInfo(OrchestrationInputValues.Empty);

		public string Folder { get; set; } = String.Empty;

		public string Name { get; set; }

		/// <summary>
		/// Gets the script info for the specified input values, mimicking how a real orchestration script reevaluates its inputs.
		/// </summary>
		/// <param name="providedValues">The input values that were already provided.</param>
		/// <returns>The script info.</returns>
		public OrchestrationScriptInfo GetOrchestrationScriptInfo(OrchestrationInputValues providedValues)
		{
			return _orchestrationScriptInfoResolver(providedValues ?? OrchestrationInputValues.Empty);
		}
	}
}
