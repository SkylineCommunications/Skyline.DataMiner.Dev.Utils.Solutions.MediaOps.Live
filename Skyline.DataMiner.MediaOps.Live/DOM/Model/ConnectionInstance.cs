namespace Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement
{
	using System;
	using System.Collections.Generic;

	public partial class ConnectionInstance
	{
		public IEnumerable<Guid> GetEndpointIds()
		{
			if (ConnectionInfo == null)
			{
				yield break;
			}

			if (ConnectionInfo.ConnectedSource is Guid connectedSource && connectedSource != Guid.Empty)
			{
				yield return connectedSource;
			}

			if (ConnectionInfo.PendingConnectedSource is Guid pendingConnectedSource && pendingConnectedSource != Guid.Empty)
			{
				yield return pendingConnectedSource;
			}

			if (ConnectionInfo.Destination is Guid destination && destination != Guid.Empty)
			{
				yield return destination;
			}
		}
	}
}
