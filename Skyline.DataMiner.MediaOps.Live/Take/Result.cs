namespace Skyline.DataMiner.Solutions.MediaOps.Live.Take
{
	using System.Threading.Tasks;

	public abstract class Result
	{
		/// <summary>
		/// Gets a value indicating whether the operation was successful.
		/// </summary>
		public bool IsSuccessful { get; internal set; }

		/// <summary>
		/// Gets the task that represents the completion of the operation.
		/// </summary>
		public Task<bool> CompletionTask { get; internal set; }
	}
}
