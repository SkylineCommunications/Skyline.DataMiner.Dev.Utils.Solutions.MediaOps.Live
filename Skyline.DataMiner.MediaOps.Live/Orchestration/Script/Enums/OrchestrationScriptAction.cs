namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Enums
{
	/// <summary>
	/// Defines the orchestration script action types.
	/// </summary>
	public enum OrchestrationScriptAction
	{
		/// <summary>
		/// Retrieves orchestration script information.
		/// </summary>
		OrchestrationScriptInfo = 0,

		/// <summary>
		/// Performs the orchestration.
		/// </summary>
		PerformOrchestration = 1,

		/// <summary>
		/// Performs the orchestration and requests missing values.
		/// </summary>
		PerformOrchestrationAskMissingValues = 2,
	}
}
