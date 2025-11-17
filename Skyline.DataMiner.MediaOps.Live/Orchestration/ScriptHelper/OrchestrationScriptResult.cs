namespace Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper
{
	internal class OrchestrationScriptResult
	{
		public string[] ErrorMessages { get; set; }

		public bool HadError { get; set; }

		public string ServiceId { get; set; }
	}
}
