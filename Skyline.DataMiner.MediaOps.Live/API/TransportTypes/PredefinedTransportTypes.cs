namespace Skyline.DataMiner.MediaOps.Live.API.TransportTypes
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public static class PredefinedTransportTypes
	{
		public static TransportType TSoIP { get; } = new TsoipTransportType();

		public static TransportType[] All { get; } =
		[
			TSoIP,
		];

		public static IReadOnlyDictionary<Guid, TransportType> ById { get; } = All.ToDictionary(x => x.ID);

		public static IReadOnlyDictionary<string, TransportType> ByName { get; } = All.ToDictionary(x => x.Name);
	}
}
