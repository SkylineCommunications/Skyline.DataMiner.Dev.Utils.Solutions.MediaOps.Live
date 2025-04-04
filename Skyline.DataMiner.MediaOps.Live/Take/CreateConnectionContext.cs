namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	internal class CreateConnectionContext
	{
		public CreateConnectionContext(ConnectionRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			Source = request.Source;
			Destination = request.Destination;
		}

		public CreateConnectionContext(Endpoint source, Endpoint destination)
		{
			if (source == null)
			{
				// ignore
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			Source = source;
			Destination = destination;
		}

		public Endpoint Source { get; }

		public Endpoint Destination { get; }

		public ConnectionInstance DomConnection { get; set; }

		public bool HasSucceeded { get; set; }
	}
}
