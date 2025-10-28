namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	/// <summary>
	/// Represents the output data from an orchestration script.
	/// </summary>
	public class OrchestrationScriptOutput
	{
		/// <summary>
		/// Gets or sets the DataMiner Agent ID of the orchestration service.
		/// </summary>
		public int OrchestrationServiceAgentId { get; set; }

		/// <summary>
		/// Gets or sets the service ID of the orchestration service.
		/// </summary>
		public int OrchestrationServiceId { get; set; }
	}
}