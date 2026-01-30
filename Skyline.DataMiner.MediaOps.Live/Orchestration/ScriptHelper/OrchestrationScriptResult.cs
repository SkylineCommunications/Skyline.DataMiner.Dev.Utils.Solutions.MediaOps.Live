namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper
{
	using Newtonsoft.Json;

	internal class OrchestrationScriptResult
	{
		[JsonProperty]
		public string[] ErrorMessages { get; set; }

		[JsonProperty]
		public bool HadError { get; set; }
	}
}
