namespace Skyline.DataMiner.MediaOps.Live.API.Repositories
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;

	public static class PredefinedTransportTypes
	{
		public static TransportType IP { get; } = new TransportType(Guid.Parse("7d8e541b-4e74-4973-a700-9ca352aa8c0b")) { Name = "IP" };

		public static TransportType SDI { get; } = new TransportType(Guid.Parse("858b9804-269c-43be-bac2-79b20ef4bc61")) { Name = "SDI" };

		public static TransportType TSoIP { get; } = new TransportType(Guid.Parse("37f7faf4-6786-429d-9d66-6e46662c1986")) { Name = "TSoIP" };

		public static TransportType SRT { get; } = new TransportType(Guid.Parse("18c3f4ed-6693-4652-b792-795773833f9c")) { Name = "SRT" };

		public static TransportType[] All { get; } =
			new[]
			{
				IP,
				SDI,
				TSoIP,
				SRT,
			};

		public static IReadOnlyDictionary<Guid, TransportType> ById { get; } = All.ToDictionary(x => x.ID);

		public static IReadOnlyDictionary<string, TransportType> ByName { get; } = All.ToDictionary(x => x.Name);
	}
}
