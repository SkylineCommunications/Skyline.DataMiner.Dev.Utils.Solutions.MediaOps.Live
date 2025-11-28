namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Element;

	internal class TakeOperationContextBase
	{
		public TakeOperationContextBase(Endpoint destination)
		{
			Destination = destination ?? throw new ArgumentNullException(nameof(destination));
		}

		public Endpoint Destination { get; }

		public IDmsElement DestinationElement { get; set; }

		public MediationElement MediationElement { get; set; }

		public string ConnectionHandlerScript { get; set; }
	}

	internal class ConnectOperationContext : TakeOperationContextBase
	{
		public ConnectOperationContext(VsgConnectionRequest connectionRequest, Endpoint source, Endpoint destination) : base(destination)
		{
			ConnectionRequest = connectionRequest ?? throw new ArgumentNullException(nameof(connectionRequest));
			Source = source ?? throw new ArgumentNullException(nameof(source));
		}

		public VsgConnectionRequest ConnectionRequest { get; }

		public Endpoint Source { get; }
	}

	internal class DisconnectOperationContext : TakeOperationContextBase
	{
		public DisconnectOperationContext(VsgDisconnectRequest disconnectRequest, Endpoint destination) : base(destination)
		{
			DisconnectRequest = disconnectRequest ?? throw new ArgumentNullException(nameof(disconnectRequest));
		}

		public VsgDisconnectRequest DisconnectRequest { get; }
	}
}
