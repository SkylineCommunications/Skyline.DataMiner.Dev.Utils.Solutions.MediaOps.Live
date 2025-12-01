namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	internal abstract class ConnectOperationContext : TakeOperationContextBase
	{
		public ConnectOperationContext(ConnectionRequest connectRequest, Endpoint source, Endpoint destination)
			: base(destination)
		{
			ConnectionRequest = connectRequest ?? throw new ArgumentNullException(nameof(connectRequest));
			Source = source ?? throw new ArgumentNullException(nameof(source));
		}

		public ConnectionRequest ConnectionRequest { get; }

		public Endpoint Source { get; }
	}

	internal class EndpointConnectOperationContext : ConnectOperationContext
	{
		public EndpointConnectOperationContext(EndpointConnectionRequest connectRequest)
			: base(connectRequest, connectRequest.Source, connectRequest.Destination)
		{
			EndpointConnectionRequest = connectRequest ?? throw new ArgumentNullException(nameof(connectRequest));
		}

		public EndpointConnectionRequest EndpointConnectionRequest { get; }
	}

	internal class VsgConnectOperationContext : ConnectOperationContext
	{
		public VsgConnectOperationContext(VsgConnectionRequest connectRequest, Endpoint source, Endpoint destination)
			: base(connectRequest, source, destination)
		{
			VsgConnectionRequest = connectRequest ?? throw new ArgumentNullException(nameof(connectRequest));
		}

		public VsgConnectionRequest VsgConnectionRequest { get; }
	}
}
