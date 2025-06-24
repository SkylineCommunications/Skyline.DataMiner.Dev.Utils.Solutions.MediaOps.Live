namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation;

	internal class ConnectionOperationContext
	{
		internal ConnectionOperationContext(ConnectionRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			Source = request.Source;
			Destination = request.Destination;
		}

		internal ConnectionOperationContext(DisconnectRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			Destination = request.Destination;
		}

		public Endpoint Source { get; }

		public Endpoint Destination { get; }

		public IDmsElement DestinationElement { get; set; }

		public MediationElement MediationElement { get; set; }

		public string ConnectionHandlerScript { get; set; }
	}
}
