namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	/// <summary>
	/// The smallest unit the operator picks for a date and time or a duration.
	/// </summary>
	public enum OrchestrationTimePrecision
	{
		/// <summary>
		/// Whole days.
		/// </summary>
		Day,

		/// <summary>
		/// Whole hours.
		/// </summary>
		Hour,

		/// <summary>
		/// Whole minutes.
		/// </summary>
		Minute,

		/// <summary>
		/// Whole seconds.
		/// </summary>
		Second,
	}
}
