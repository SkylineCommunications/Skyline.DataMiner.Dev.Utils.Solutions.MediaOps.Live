namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Automation;

	public interface IOrchestrationParameters
	{
		public abstract IDictionary<string, Guid> GetParameterInformation(IEngine engine);

		public abstract Guid GetDefinition(IEngine engine);
	}
}
