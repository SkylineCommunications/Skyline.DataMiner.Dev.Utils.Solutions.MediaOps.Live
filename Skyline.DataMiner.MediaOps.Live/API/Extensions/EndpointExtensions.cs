namespace Skyline.DataMiner.MediaOps.Live.API.Extensions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Net.Helper;

	public static class EndpointExtensions
	{
		public static IEnumerable<(Endpoint Endpoint, TransportType TransportType)> JoinTransportTypes(
			this IEnumerable<Endpoint> endpoints,
			Repository<TransportType> transportTypesRepository)
		{
			if (endpoints == null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			if (transportTypesRepository == null)
			{
				throw new ArgumentNullException(nameof(transportTypesRepository));
			}

			return endpoints
				.Batch(100)
				.JoinInBatches(
					transportTypesRepository,
					endpoint => endpoint.TransportType,
					(e, t) => (e, t))
				.Flatten();
		}

		public static IEnumerable<IEnumerable<(Endpoint Endpoint, TransportType TransportType)>> JoinTransportTypes(
			this IEnumerable<IEnumerable<Endpoint>> endpoints,
			Repository<TransportType> transportTypesRepository)
		{
			if (endpoints == null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			if (transportTypesRepository == null)
			{
				throw new ArgumentNullException(nameof(transportTypesRepository));
			}

			return endpoints.JoinInBatches(
				transportTypesRepository,
				endpoint => endpoint.TransportType,
				(e, t) => (e, t));
		}
	}
}
