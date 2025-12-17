namespace Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper
{
	using Newtonsoft.Json;

	internal class OrchestrationScriptResult
	{
		[JsonProperty]
		public string[] ErrorMessages { get; set; }

		[JsonProperty]
		public bool HadError { get; set; }

		[JsonProperty]
		public string ServiceId { get; set; }
	}
}
