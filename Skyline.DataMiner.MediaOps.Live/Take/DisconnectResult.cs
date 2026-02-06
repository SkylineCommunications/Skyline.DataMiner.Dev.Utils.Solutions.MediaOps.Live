namespace Skyline.DataMiner.Solutions.MediaOps.Live.Take
{
	using System;

	public abstract class DisconnectResult : Result
	{
		protected DisconnectResult(DisconnectRequest request)
		{
			DisconnectRequest = request ?? throw new ArgumentNullException(nameof(request));
		}

		public DisconnectRequest DisconnectRequest { get; }
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
