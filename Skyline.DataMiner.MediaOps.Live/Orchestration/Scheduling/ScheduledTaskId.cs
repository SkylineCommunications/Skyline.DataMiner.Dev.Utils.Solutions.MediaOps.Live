namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling
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
			if (obj is null)
			{
				return false;
			}

			return DmaId == obj.DmaId && TaskId == obj.TaskId;
		}

		/// <summary>
		/// Operator for comparison.
		/// </summary>
		/// <param name="left">Left id.</param>
		/// <param name="right">Right id.</param>
		/// <returns>True when both object create the same ID.</returns>
		public static bool operator ==(ScheduledTaskId left, ScheduledTaskId right)
		{
			if (left is null)
			{
				return right is null;
			}

			return left.Equals(right);
		}

		/// <summary>
		/// Operator for comparison.
		/// </summary>
		/// <param name="left">Left id.</param>
		/// <param name="right">Right id.</param>
		/// <returns>True when both object do not create the same ID.</returns>
		public static bool operator !=(ScheduledTaskId left, ScheduledTaskId right)
		{
			return !(left == right);
		}

		/// <summary>
		/// Generates a string showing the combine task ID.
		/// </summary>
		/// <returns>A string representation of the task ID.</returns>
		public override string ToString()
		{
			return $"{DmaId}/{TaskId}";
		}
	}
}