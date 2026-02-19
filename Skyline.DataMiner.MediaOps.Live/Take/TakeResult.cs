namespace Skyline.DataMiner.Solutions.MediaOps.Live.Take
{
	using System;
	using System.Threading.Tasks;

	public abstract class TakeResult<T> where T : TakeRequest
	{
		protected TakeResult(T request, bool isSuccessful, Task<bool> completionTask)
		{
			Request = request ?? throw new ArgumentNullException(nameof(request));
			IsSuccessful = isSuccessful;
			CompletionTask = completionTask ?? throw new ArgumentNullException(nameof(completionTask));
		}

		/// <summary>
		/// Gets the original request that led to this result.
		/// </summary>
		public T Request { get; }

		/// <summary>
		/// Gets a value indicating whether the operation was successful.
		/// </summary>
		public bool IsSuccessful { get; }

		/// <summary>
		/// Gets the task that represents the completion of the operation.
		/// </summary>
		public Task<bool> CompletionTask { get; }
	}
}
