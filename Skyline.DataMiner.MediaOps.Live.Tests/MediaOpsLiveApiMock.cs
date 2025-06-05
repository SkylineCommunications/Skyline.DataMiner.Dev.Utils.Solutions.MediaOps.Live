namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.DOM.Definitions;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	public class MediaOpsLiveApiMock : MediaOpsLiveApi
	{
		public MediaOpsLiveApiMock(bool installDomModules = true, bool createEndpoints = true, bool createVsgs = true, bool createConnections = false)
			: this(new DomConnectionMock(), installDomModules, createEndpoints, createVsgs, createConnections)
		{
		}

		public MediaOpsLiveApiMock(Net.IConnection connection, bool installDomModules = true, bool createEndpoints = true, bool createVsgs = true, bool createConnections = false)
			: base(connection)
		{
			if (installDomModules)
			{
				var slcConnectivityManagementDomModule = new SlcConnectivityManagementDomModule();
				DomModuleInstaller.Install(Connection.HandleMessages, slcConnectivityManagementDomModule, x => { });
			}

			var category = new Category { Name = "Category 1" };
			Categories.Create(category);

			var transportTypeIP = new TransportType { Name = "IP" };
			TransportTypes.Create(transportTypeIP);

			var videoLevel = new Level { Number = 1, Name = "Video", TransportType = transportTypeIP };
			var audioLevel = new Level { Number = 2, Name = "Audio", TransportType = transportTypeIP };
			var dataLevel = new Level { Number = 3, Name = "Data", TransportType = transportTypeIP };
			Levels.CreateOrUpdate([videoLevel, audioLevel, dataLevel]);

			if (!createEndpoints)
			{
				return;
			}

			for (int i = 1; i <= 10; i++)
			{
				var videoSource1 = new Endpoint
				{
					Role = Role.Source,
					Name = $"Video Source {i}",
					TransportType = transportTypeIP,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var audioSource1 = new Endpoint
				{
					Role = Role.Source,
					Name = $"Audio Source {i}",
					TransportType = transportTypeIP,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var videoDestination1 = new Endpoint
				{
					Role = Role.Destination,
					Name = $"Video Destination {i}",
					TransportType = transportTypeIP,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				var audioDestination1 = new Endpoint
				{
					Role = Role.Destination,
					Name = $"Audio Destination {i}",
					TransportType = transportTypeIP,
					Element = $"123/{i}",
					Identifier = $"Key-{i}",
				};
				Endpoints.CreateOrUpdate([videoSource1, audioSource1, videoDestination1, audioDestination1]);

				if (createVsgs)
				{
					var source1 = new VirtualSignalGroup
					{
						Role = Role.Source,
						Name = $"Source {i}",
						Description = $"Source {i}",
						Categories =
						[
							category,
						],
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
						Categories =
						[
							category,
						],
						Levels =
						[
							new LevelEndpoint(videoLevel, videoDestination1),
							new LevelEndpoint(audioLevel, audioDestination1),
						],
					};
					VirtualSignalGroups.CreateOrUpdate([source1, destination1]);
				}

				if (createConnections)
				{
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
		}

		public void CreateConnection(Endpoint source, Endpoint destination)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var connection = new Connection
			{
				Destination = destination,
				ConnectedSource = source,
				IsConnected = source != null,
			};
			Connections.CreateOrUpdate(connection);
		}

		public void CreatePendingConnection(Endpoint? source, Endpoint pendingSource, Endpoint destination)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			var connection = new Connection
			{
				Destination = destination,
				ConnectedSource = source,
				IsConnected = source != null,
				PendingConnectedSource = pendingSource,
			};
			Connections.CreateOrUpdate(connection);
		}

		public void CreatePendingConnection(Endpoint pendingSource, Endpoint destination)
		{
			if (destination is null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			CreatePendingConnection(null, pendingSource, destination);
		}
	}
}
