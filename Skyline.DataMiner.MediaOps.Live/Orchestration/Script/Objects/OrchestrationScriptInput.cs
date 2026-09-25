namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

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
			InputValues = new Dictionary<string, OrchestrationInputValue>();
		}

		[JsonProperty]
		public Dictionary<string, object> ProfileParameterValues { get; set; }

		[JsonProperty]
		public string ProfileInstance { get; set; }

		[JsonProperty]
		public Dictionary<string, string> Metadata { get; set; }

		/// <summary>
		/// Gets or sets the dynamic orchestration input values that were already provided, keyed by the path of the field they belong to.
		/// </summary>
		[JsonProperty]
		public Dictionary<string, OrchestrationInputValue> InputValues { get; set; }
	}
}