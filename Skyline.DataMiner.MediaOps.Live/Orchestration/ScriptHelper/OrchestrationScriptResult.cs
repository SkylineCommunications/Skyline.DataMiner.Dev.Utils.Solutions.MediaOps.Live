namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper
{
	using Newtonsoft.Json;

	internal class OrchestrationScriptResult
	{
		[JsonProperty]
		public string[] ErrorMessages { get; set; }

		[JsonProperty]
		public bool HadError { get; set; }

		// The script did not run because another execution already orchestrates the same node of the event.
		[JsonProperty]
		public bool IsSkipped { get; set; }
	}
}
