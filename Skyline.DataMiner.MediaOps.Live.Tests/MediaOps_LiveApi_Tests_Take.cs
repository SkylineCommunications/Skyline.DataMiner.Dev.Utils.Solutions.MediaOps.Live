namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using FluentAssertions;

	using Moq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting.TestData;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Take
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_Take_ConnectEndpointWithoutElement_ShouldNotThrowNullReference()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation(createEndpoints: false, createVsgs: false);
			var api = simulation.Api;

			var transportType = api.TransportTypes.Query().First(x => x.Name == "TSoIP");

			// Create endpoints without an element assigned (Element is null)
			var source = new Endpoint()
			{
				Role = EndpointRole.Source,
				Name = "Source Without Element",
				TransportType = transportType,
				Element = null,
				Identifier = "no-element-source",
			};

			var destination = new Endpoint()
			{
				Role = EndpointRole.Destination,
				Name = "Destination Without Element",
				TransportType = transportType,
				Element = null,
				Identifier = "no-element-destination",
			};

			api.Endpoints.CreateOrUpdate([source, destination]);

			using var performanceCollector = new PerformanceCollector(Mock.Of<IPerformanceLogger>());
			using var performanceTracker = new PerformanceTracker(performanceCollector);

			var takeHelper = api.GetConnectionHandler();

			var connectionRequests = new[]
			{
				new EndpointConnectionRequest(source, destination),
			};

			// Act & Assert
			// This should not throw a NullReferenceException.
			// Currently it does because FindConnectionHandlerScripts tries to call
			// GetConnectionHandlerScriptName on a null MediationElement when the
			// endpoint has no element assigned.
			var act = () => takeHelper.Take(connectionRequests, performanceTracker);

			act.Should().NotThrow<NullReferenceException>();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Take_ConnectEndpointWithElement_ShouldSucceed()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation(createEndpoints: true, createVsgs: false);
			var api = simulation.Api;

			var endpoints = api.Endpoints.Query();
			var source = endpoints.First(x => x.Name == "Video Source 1");
			var destination = endpoints.First(x => x.Name == "Video Destination 1");

			using var performanceCollector = new PerformanceCollector(Mock.Of<IPerformanceLogger>());
			using var performanceTracker = new PerformanceTracker(performanceCollector);

			var takeHelper = api.GetConnectionHandler();

			var connectionRequests = new[]
			{
				new EndpointConnectionRequest(source, destination),
			};

			// Act & Assert
			var act = () => takeHelper.Take(connectionRequests, performanceTracker);

			act.Should().NotThrow();
		}
	}
}
