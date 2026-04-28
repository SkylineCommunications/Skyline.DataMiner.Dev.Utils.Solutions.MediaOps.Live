namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using System;

	using FluentAssertions;

	using Moq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_Take
	{
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

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Take_ConnectDestinationWithoutElement_ShouldThrowInvalidOperation()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation(createEndpoints: false, createVsgs: false);
			var api = simulation.Api;

			var transportType = api.TransportTypes.Query().First(x => x.Name == "TSoIP");

			// Source has an element assigned (mediated element on DMA 123)
			var source = new Endpoint()
			{
				Role = EndpointRole.Source,
				Name = "Source With Element",
				TransportType = transportType,
				Element = new DmsElementId(123, 1),
				Identifier = "source-with-element",
			};

			// Destination has no element assigned
			var destination = new Endpoint()
			{
				Role = EndpointRole.Destination,
				Name = "Destination Without Element",
				TransportType = transportType,
				Element = null,
				Identifier = "destination-no-element",
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
			// Destination endpoints must always have an element assigned for connect operations.
			var act = () => takeHelper.Take(connectionRequests, performanceTracker);

			act.Should().Throw<InvalidOperationException>()
				.WithMessage("*Missing element for endpoints*");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Take_ConnectBothWithoutElement_ShouldThrowInvalidOperation()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation(createEndpoints: false, createVsgs: false);
			var api = simulation.Api;

			var transportType = api.TransportTypes.Query().First(x => x.Name == "TSoIP");

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
			// When neither endpoint has an element, a clear InvalidOperationException
			// should be thrown instead of a NullReferenceException.
			var act = () => takeHelper.Take(connectionRequests, performanceTracker);

			act.Should().Throw<InvalidOperationException>()
				.WithMessage("*Missing element for endpoints*");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Take_DisconnectEndpointWithElement_ShouldSucceed()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation(createEndpoints: true, createVsgs: false);
			var api = simulation.Api;

			var endpoints = api.Endpoints.Query();
			var source = endpoints.First(x => x.Name == "Video Source 1");
			var destination = endpoints.First(x => x.Name == "Video Destination 1");

			// Set up an existing connection so the disconnect has something to work with
			simulation.CreateTestConnection(source, destination);

			using var performanceCollector = new PerformanceCollector(Mock.Of<IPerformanceLogger>());
			using var performanceTracker = new PerformanceTracker(performanceCollector);

			var takeHelper = api.GetConnectionHandler();

			var disconnectRequests = new[]
			{
				new EndpointDisconnectRequest(destination),
			};

			// Act & Assert
			var act = () => takeHelper.Disconnect(disconnectRequests, performanceTracker);

			act.Should().NotThrow();
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Take_DisconnectEndpointWithoutElement_ShouldThrowInvalidOperation()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation(createEndpoints: false, createVsgs: false);
			var api = simulation.Api;

			var transportType = api.TransportTypes.Query().First(x => x.Name == "TSoIP");

			// Destination has no element assigned
			var destination = new Endpoint()
			{
				Role = EndpointRole.Destination,
				Name = "Destination Without Element",
				TransportType = transportType,
				Element = null,
				Identifier = "destination-no-element",
			};

			api.Endpoints.CreateOrUpdate([destination]);

			using var performanceCollector = new PerformanceCollector(Mock.Of<IPerformanceLogger>());
			using var performanceTracker = new PerformanceTracker(performanceCollector);

			var takeHelper = api.GetConnectionHandler();

			var disconnectRequests = new[]
			{
				new EndpointDisconnectRequest(destination),
			};

			// Act & Assert
			// Destination endpoints must always have an element assigned,
			// even for disconnect operations.
			var act = () => takeHelper.Disconnect(disconnectRequests, performanceTracker);

			act.Should().Throw<InvalidOperationException>()
				.WithMessage("*Missing element for endpoints*");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_Take_ConnectMultipleRequests_MixedElementAssignment_ShouldThrowForMissingElement()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation(createEndpoints: true, createVsgs: false);
			var api = simulation.Api;

			var transportType = api.TransportTypes.Query().First(x => x.Name == "TSoIP");

			var endpoints = api.Endpoints.Query();
			var sourceWithElement = endpoints.First(x => x.Name == "Video Source 1");
			var destinationWithElement = endpoints.First(x => x.Name == "Video Destination 1");

			// Create a destination without an element
			var destinationWithoutElement = new Endpoint()
			{
				Role = EndpointRole.Destination,
				Name = "Destination Without Element",
				TransportType = transportType,
				Element = null,
				Identifier = "destination-no-element",
			};

			api.Endpoints.CreateOrUpdate([destinationWithoutElement]);

			using var performanceCollector = new PerformanceCollector(Mock.Of<IPerformanceLogger>());
			using var performanceTracker = new PerformanceTracker(performanceCollector);

			var takeHelper = api.GetConnectionHandler();

			var connectionRequests = new[]
			{
				// Request 1: both endpoints have elements (normal case)
				new EndpointConnectionRequest(sourceWithElement, destinationWithElement),

				// Request 2: source has element, destination does not
				new EndpointConnectionRequest(sourceWithElement, destinationWithoutElement),
			};

			// Act & Assert
			// Should throw because the second request has a destination without an element.
			var act = () => takeHelper.Take(connectionRequests, performanceTracker);

			act.Should().Throw<InvalidOperationException>()
				.WithMessage("*Missing element for endpoints*");
		}
	}
}
