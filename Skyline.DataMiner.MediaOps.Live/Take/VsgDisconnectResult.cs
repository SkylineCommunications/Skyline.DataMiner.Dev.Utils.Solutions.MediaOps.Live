namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Threading.Tasks;

	public class VsgDisconnectResult
	{
		public VsgDisconnectResult(VsgDisconnectRequest request)
		{
			Request = request ?? throw new ArgumentNullException(nameof(request));
		}

		public VsgDisconnectRequest Request { get; }

		/// <summary>
		/// Gets the task that represents the completion of the disconnect operation.
		/// </summary>
		public Task CompletionTask { get; internal set; }
	}
}
