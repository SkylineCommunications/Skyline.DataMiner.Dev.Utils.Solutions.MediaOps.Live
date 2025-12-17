namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using Newtonsoft.Json;

	internal class OrchestrationScriptOutput
	{
		[JsonProperty]
		public int OrchestrationServiceAgentId { get; set; }

		[JsonProperty]
		public int OrchestrationServiceId { get; set; }
	}
}