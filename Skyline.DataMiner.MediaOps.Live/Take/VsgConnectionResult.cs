namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Threading.Tasks;

	public class VsgConnectionResult
	{
		public VsgConnectionResult(VsgConnectionRequest request)
		{
			Request = request ?? throw new ArgumentNullException(nameof(request));
		}

		public VsgConnectionRequest Request { get; }

		/// <summary>
		/// Gets the task that represents the completion of the connection operation.
		/// </summary>
		public Task CompletionTask { get; internal set; }
	}
}
