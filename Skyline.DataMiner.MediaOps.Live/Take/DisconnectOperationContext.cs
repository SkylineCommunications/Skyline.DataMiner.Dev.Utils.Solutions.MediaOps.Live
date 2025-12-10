namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	internal abstract class DisconnectOperationContext : TakeOperationContext
	{
		protected DisconnectOperationContext(DisconnectRequest disconnectRequest, Endpoint destination)
			: base(destination)
		{
			DisconnectRequest = disconnectRequest ?? throw new ArgumentNullException(nameof(disconnectRequest));
		}

		public DisconnectRequest DisconnectRequest { get; }
	}

	internal class EndpointDisconnectOperationContext : DisconnectOperationContext
	{
		public EndpointDisconnectOperationContext(EndpointDisconnectRequest disconnectRequest)
			: base(disconnectRequest, disconnectRequest.Destination)
		{
			EndpointDisconnectRequest = disconnectRequest ?? throw new ArgumentNullException(nameof(disconnectRequest));
		}

		public EndpointDisconnectRequest EndpointDisconnectRequest { get; }
	}

	internal class VsgEndpointDisconnectOperationContext : DisconnectOperationContext
	{
		public VsgEndpointDisconnectOperationContext(VsgDisconnectRequest disconnectRequest, Endpoint destination)
			: base(disconnectRequest, destination)
		{
			VsgDisconnectRequest = disconnectRequest ?? throw new ArgumentNullException(nameof(disconnectRequest));
		}

		public VsgDisconnectRequest VsgDisconnectRequest { get; }
	}
}
