namespace Skyline.DataMiner.MediaOps.Live.API.Repositories
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public static class PredefinedTransportTypes
	{
		public static TransportType TSoIP { get; } = new TransportType(Guid.Parse("37f7faf4-6786-429d-9d66-6e46662c1986"))
		{
			Name = "TSoIP",
			Fields =
			{
				new TransportTypeField { Name = "Source IP" },
				new TransportTypeField { Name = "Multicast IP" },
				new TransportTypeField { Name = "Port" },
			},
		};

		public static TransportType[] All { get; } =
		[
			TSoIP,
		];

		public static IReadOnlyDictionary<Guid, TransportType> ById { get; } = All.ToDictionary(x => x.ID);

		public static IReadOnlyDictionary<string, TransportType> ByName { get; } = All.ToDictionary(x => x.Name);
	}
}
