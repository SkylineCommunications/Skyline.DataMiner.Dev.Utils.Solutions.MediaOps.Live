namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;

	/// <summary>
	///     Simplified class to hold the scheduled task ID.
	/// </summary>
	public sealed class ScheduledTaskId : IEquatable<ScheduledTaskId>
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="ScheduledTaskId" /> class.
		/// </summary>
		/// <param name="dmaId">DataMiner agent ID.</param>
		/// <param name="taskId">Agent specific task ID.</param>
		public ScheduledTaskId(int dmaId, int taskId)
		{
			DmaId = dmaId;
			TaskId = taskId;
		}

		/// <summary>
		/// Gets the DataMiner agent ID.
		/// </summary>
		public int DmaId { get; }

		/// <summary>
		/// Gets the unique task ID on the DataMiner agent.
		/// </summary>
		public int TaskId { get; }

		/// <summary>
		///     Compares two <see cref="ScheduledTaskId" /> objects.
		/// </summary>
		/// <param name="obj">The object to compare with.</param>
		/// <returns>true if equal, otherwise false.</returns>
		public bool Equals(ScheduledTaskId obj)
		{
			if (obj == null)
			{
				return false;
			}

			return DmaId == obj.DmaId && TaskId == obj.TaskId;
		}
	}
}