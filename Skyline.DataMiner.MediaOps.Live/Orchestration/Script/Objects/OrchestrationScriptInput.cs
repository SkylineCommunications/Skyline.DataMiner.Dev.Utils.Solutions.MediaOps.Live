namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	public class OrchestrationScriptInput
	{
		public OrchestrationScriptInput()
			: this(new Dictionary<string, object>())
		{
		}

		public OrchestrationScriptInput(Dictionary<string, object> profileParameterValues) : this(profileParameterValues, String.Empty)
		{
		}

		[JsonConstructor]
		public OrchestrationScriptInput(Dictionary<string, object> profileParameterValues, string profileInstance)
		{
			ProfileParameterValues = profileParameterValues;
			ProfileInstance = profileInstance;
			Metadata = new Dictionary<string, string>();
		}

		public Dictionary<string, object> ProfileParameterValues { get; }

		public string ProfileInstance { get; }

		public Dictionary<string, string> Metadata { get; }
	}
}