namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	internal class OrchestrationScriptInput
	{
		internal OrchestrationScriptInput()
			: this(new Dictionary<string, object>())
		{
		}

		internal OrchestrationScriptInput(Dictionary<string, object> profileParameterValues) : this(profileParameterValues, String.Empty)
		{
		}

		[JsonConstructor]
		internal OrchestrationScriptInput(Dictionary<string, object> profileParameterValues, string profileInstance)
		{
			ProfileParameterValues = profileParameterValues;
			ProfileInstance = profileInstance;
			Metadata = new Dictionary<string, string>();
		}

		[JsonProperty]
		public Dictionary<string, object> ProfileParameterValues { get; set; }

		[JsonProperty]
		public string ProfileInstance { get; set; }

		[JsonProperty]
		public Dictionary<string, string> Metadata { get; set; }
	}
}