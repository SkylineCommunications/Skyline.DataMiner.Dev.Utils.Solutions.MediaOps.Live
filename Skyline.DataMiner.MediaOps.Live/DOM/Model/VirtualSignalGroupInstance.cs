namespace Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement
{
	using System;
	using System.Collections.Generic;

	internal partial class VirtualSignalGroupInstance
	{
		public bool IsSource => VirtualSignalGroupInfo.Role == SlcConnectivityManagementIds.Enums.Role.Source;

		public bool IsDestination => VirtualSignalGroupInfo.Role == SlcConnectivityManagementIds.Enums.Role.Destination;

		public IEnumerable<Guid> GetEndpointIds()
		{
			if (VirtualSignalGroupLevel == null)
			{
				yield break;
			}

			foreach (var level in VirtualSignalGroupLevel)
			{
				if (level.Endpoint == null)
				{
					continue;
				}

				yield return (Guid)level.Endpoint;
			}
		}
	}
}
