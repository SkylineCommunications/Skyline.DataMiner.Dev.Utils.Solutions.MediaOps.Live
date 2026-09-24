namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	/// <summary>
	/// Base class for orchestration scripts with a fixed set of profile based parameters.
	/// Use <see cref="DynamicOrchestrationScript"/> when the inputs depend on the values that were already provided.
	/// </summary>
	public abstract class OrchestrationScript : OrchestrationScriptBase
	{
		public abstract void Orchestrate(IEngine engine);

		public abstract IEnumerable<IOrchestrationParameters> GetParameters();

		public object GetParameterValue(string paramName)
		{
			ParameterInfo param = ParameterInfos.FirstOrDefault(paramInfo => paramInfo.Name == paramName);

			if (param == null)
			{
				throw new InvalidOperationException($"Parameter with name '{paramName}' is missing");
			}

			return param.Value;
		}

		internal override IEnumerable<IOrchestrationParameters> GetProfileParameters()
		{
			return GetParameters();
		}

		internal override OrchestrationInputDefinition EvaluateInputs(IEngine engine, OrchestrationInputValues providedValues)
		{
			return null;
		}

		internal override void ExecuteOrchestration(IEngine engine)
		{
			Orchestrate(engine);
		}
	}
}
