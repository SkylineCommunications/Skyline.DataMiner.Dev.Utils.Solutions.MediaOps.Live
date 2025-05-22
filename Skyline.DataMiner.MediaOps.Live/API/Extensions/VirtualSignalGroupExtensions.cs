namespace Skyline.DataMiner.MediaOps.Live.API.Extensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories;

	public static class VirtualSignalGroupExtensions
	{
		public static IEnumerable<(VirtualSignalGroup VirtualSignalGroup, IEnumerable<Endpoint> Endpoints)> JoinEndpoints(
			this IEnumerable<VirtualSignalGroup> virtualSignalGroups,
			Repository<Endpoint> endpointsRepository)
		{
			if (virtualSignalGroups == null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			if (endpointsRepository == null)
			{
				throw new ArgumentNullException(nameof(endpointsRepository));
			}

			return virtualSignalGroups.JoinInBatches(
				endpointsRepository,
				vsg => vsg.GetEndpoints().Select(x => x.Endpoint),
				(vsg, endpoints) => (vsg, endpoints));
		}

		public static IEnumerable<IEnumerable<(VirtualSignalGroup VirtualSignalGroup, IEnumerable<Endpoint> Endpoints)>> JoinEndpoints(
			this IEnumerable<IEnumerable<VirtualSignalGroup>> virtualSignalGroups,
			Repository<Endpoint> endpointsRepository)
		{
			if (virtualSignalGroups == null)
			{
				throw new ArgumentNullException(nameof(virtualSignalGroups));
			}

			if (endpointsRepository == null)
			{
				throw new ArgumentNullException(nameof(endpointsRepository));
			}

			return virtualSignalGroups.Join(
				endpointsRepository,
				vsg => vsg.GetEndpoints().Select(x => x.Endpoint),
				(vsg, endpoints) => (vsg, endpoints));
		}
	}
}
