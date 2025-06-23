namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	internal class TakeContext
	{
		internal TakeContext(ConnectionRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			Source = request.Source;
			Destination = request.Destination;
		}

		internal TakeContext(DisconnectRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			Destination = request.Destination;
		}

		public Endpoint Source { get; }

		public Endpoint Destination { get; }

		public string ConnectionHandlerScript { get; set; }

		public ConnectionInstance DomConnection { get; set; }

		public bool HasSucceeded { get; set; }
	}
}
