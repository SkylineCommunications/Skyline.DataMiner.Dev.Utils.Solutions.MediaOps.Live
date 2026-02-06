namespace Skyline.DataMiner.Solutions.MediaOps.Live.Take
{
	using System;

	public abstract class ConnectionResult : Result
	{
		protected ConnectionResult(ConnectionRequest request)
		{
			ConnectionRequest = request ?? throw new ArgumentNullException(nameof(request));
		}

		public ConnectionRequest ConnectionRequest { get; }
	}

	public class EndpointConnectionResult : ConnectionResult
	{
		public EndpointConnectionResult(EndpointConnectionRequest request)
			: base(request)
		{
			EndpointConnectionRequest = request ?? throw new ArgumentNullException(nameof(request));
		}

		public EndpointConnectionRequest EndpointConnectionRequest { get; }
	}

	public class VsgConnectionResult : ConnectionResult
	{
		public VsgConnectionResult(VsgConnectionRequest request)
			: base(request)
		{
			VsgConnectionRequest = request ?? throw new ArgumentNullException(nameof(request));
		}

		public VsgConnectionRequest VsgConnectionRequest { get; }
	}
}
