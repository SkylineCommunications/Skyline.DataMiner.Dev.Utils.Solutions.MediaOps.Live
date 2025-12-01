namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Threading.Tasks;

	public abstract class DisconnectResult
	{
		protected DisconnectResult(DisconnectRequest request)
		{
			DisconnectRequest = request ?? throw new ArgumentNullException(nameof(request));
		}

		public DisconnectRequest DisconnectRequest { get; }

		/// <summary>
		/// Gets a value indicating whether the disconnect operation was successful.
		/// </summary>
		public bool IsSuccessful { get; internal set; }

		/// <summary>
		/// Gets the task that represents the completion of the disconnect operation.
		/// </summary>
		public Task CompletionTask { get; internal set; }
	}

	public class EndpointDisconnectResult : DisconnectResult
	{
		public EndpointDisconnectResult(EndpointDisconnectRequest request)
			: base(request)
		{
			EndpointDisconnectRequest = request ?? throw new ArgumentNullException(nameof(request));
		}

		public EndpointDisconnectRequest EndpointDisconnectRequest { get; }
	}

	public class VsgDisconnectResult : DisconnectResult
	{
		public VsgDisconnectResult(VsgDisconnectRequest request)
			: base(request)
		{
			VsgDisconnectRequest = request ?? throw new ArgumentNullException(nameof(request));
		}

		public VsgDisconnectRequest VsgDisconnectRequest { get; }
	}
}
