namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	public class MediaOpsLiveApiMock : MediaOpsLiveApi
	{
		public MediaOpsLiveApiMock()
			: base(CreateMessageHandler(out var messageHandler).HandleMessages)
		{
			MessageHandler = messageHandler;

			var transportType = new TransportType { Name = "IP" };
			TransportTypes.Create(transportType);

			var videoLevel = new Level { Number = 1, Name = "Video", TransportType = transportType };
			var audioLevel = new Level { Number = 2, Name = "Audio", TransportType = transportType };
			var dataLevel = new Level { Number = 3, Name = "Data", TransportType = transportType };
			Levels.CreateOrUpdate([videoLevel, audioLevel, dataLevel]);

			for (int i = 1; i <= 10; i++)
			{
				var videoSource1 = new Endpoint
				{
					Role = Role.Source,
					Name = $"Video Source {i}",
					TransportType = transportType,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var audioSource1 = new Endpoint
				{
					Role = Role.Source,
					Name = $"Audio Source {i}",
					TransportType = transportType,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var videoDestination1 = new Endpoint
				{
					Role = Role.Destination,
					Name = $"Video Destination {i}",
					TransportType = transportType,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var audioDestination1 = new Endpoint
				{
					Role = Role.Destination,
					Name = $"Audio Destination {i}",
					TransportType = transportType,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				Endpoints.CreateOrUpdate([videoSource1, audioSource1, videoDestination1, audioDestination1]);

				var source1 = new VirtualSignalGroup
				{
					Role = Role.Source,
					Name = $"Source {i}",
					Description = $"Source {i}",
					Levels =
					[
						new LevelEndpoint(videoLevel, videoSource1),
						new LevelEndpoint(audioLevel, audioSource1),
					],
				};
				var destination1 = new VirtualSignalGroup
				{
					Role = Role.Destination,
					Name = $"Destination {i}",
					Description = $"Destination {i}",
					Levels =
					[
						new LevelEndpoint(videoLevel, videoDestination1),
						new LevelEndpoint(audioLevel, audioDestination1),
					],
				};
				VirtualSignalGroups.CreateOrUpdate([source1, destination1]);

				var connection1 = new Connection
				{
					Destination = videoDestination1,
					ConnectedSource = videoSource1,
					IsConnected = true,
				};
				var connection2 = new Connection
				{
					Destination = audioDestination1,
					ConnectedSource = audioSource1,
					IsConnected = true,
				};
				Connections.CreateOrUpdate([connection1, connection2]);
			}
		}

		public DomSLNetMessageHandler MessageHandler { get; }

		private static DomSLNetMessageHandler CreateMessageHandler(out DomSLNetMessageHandler handler)
		{
			handler = new DomSLNetMessageHandler();
			return handler;
		}
	}
}
