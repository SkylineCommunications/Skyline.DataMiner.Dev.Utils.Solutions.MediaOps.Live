namespace Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting.TestData
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class TsoipTransportType : TransportType
	{
		public TsoipTransportType() : base(Guid.Parse("37f7faf4-6786-429d-9d66-6e46662c1986"))
		{
			Name = "TSoIP";
			Fields =
			[
				new TransportTypeField { Name = FieldNames.SourceIp },
				new TransportTypeField { Name = FieldNames.MulticastIp },
				new TransportTypeField { Name = FieldNames.MulticastPort },
			];
		}

		public class FieldNames
		{
			public const string SourceIp = "Source IP";
			public const string MulticastIp = "Multicast IP";
			public const string MulticastPort = "Multicast Port";
		}
	}
}
