namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Profiles;

	public interface IOrchestrationParameters
	{
		public abstract IDictionary<string, Guid> GetParameterInformation(IEngine engine);

		public abstract IDictionary<string, Parameter> GetParameterReferences(IEngine engine);
	}
}
