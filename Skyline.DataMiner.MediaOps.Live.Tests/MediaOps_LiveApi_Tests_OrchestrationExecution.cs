namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Moq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.ConnectionHandlers;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Plan;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers;

	using Connection = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration.Connection;
	using Level = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration.Level;
	using LevelMapping = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration.LevelMapping;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_OrchestrationExecution
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationExecution_DisconnectFailsDestinationVsgIsUnlocked()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var destinationVsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Destination 1");

			// Lock the destination VSG (simulating it was locked by a prior connect event)
			api.VirtualSignalGroups.LockVirtualSignalGroup(destinationVsg, "Orchestration Engine", "Locked for job", "TestJob");

			// Verify locked
			var stateBefore = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			Assert.AreEqual(LockState.Locked, stateBefore.LockState);

			// Create a mock api that returns a TakeHelper that throws on disconnect
			var mockApi = new Mock<MediaOpsLiveApi>(api.Connection) { CallBase = true };
			mockApi.Setup(x => x.GetConnectionHandler()).Returns(new FailingDisconnectTakeHelper(api));

			// Create a disconnect event configuration that references the real destination VSG
			var orchestrationEvent = CreateDisconnectEvent(destinationVsg);

			var planHelper = new Mock<IMediaOpsPlanHelper>().Object;
			var settings = new OrchestrationSettings { Timeout = TimeSpan.FromSeconds(5) };
			var helper = new OrchestrationEventExecutionHelper(mockApi.Object, planHelper, settings);

			// Act
			using var performanceCollector = new PerformanceCollector(Mock.Of<IPerformanceLogger>());
			using var performanceTracker = new PerformanceTracker(performanceCollector);
			helper.ExecuteConnections(orchestrationEvent, performanceTracker);

			// Assert - the VSG should be unlocked despite the disconnect failure
			var stateAfter = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			Assert.AreEqual(LockState.Unlocked, stateAfter.LockState);

			// The event should be in a Failed state
			Assert.AreEqual(EventState.Failed, orchestrationEvent.EventState);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationExecution_DisconnectFailsMultipleDestinationVsgsAreUnlocked()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var destinationVsg1 = api.VirtualSignalGroups.Query().First(x => x.Name == "Destination 1");
			var destinationVsg2 = api.VirtualSignalGroups.Query().First(x => x.Name == "Destination 2");

			// Lock the destination VSGs
			api.VirtualSignalGroups.LockVirtualSignalGroups(
				new[] { destinationVsg1, destinationVsg2 },
				"Orchestration Engine",
				"Locked for job",
				"TestJob");

			// Verify locked
			Assert.AreEqual(LockState.Locked, api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg1).LockState);
			Assert.AreEqual(LockState.Locked, api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg2).LockState);

			// Create a mock api that returns a TakeHelper that throws on disconnect
			var mockApi = new Mock<MediaOpsLiveApi>(api.Connection) { CallBase = true };
			mockApi.Setup(x => x.GetConnectionHandler()).Returns(new FailingDisconnectTakeHelper(api));

			// Create a disconnect event with multiple destinations
			var orchestrationEvent = CreateDisconnectEvent(destinationVsg1, destinationVsg2);

			var planHelper = new Mock<IMediaOpsPlanHelper>().Object;
			var settings = new OrchestrationSettings { Timeout = TimeSpan.FromSeconds(5) };
			var helper = new OrchestrationEventExecutionHelper(mockApi.Object, planHelper, settings);

			// Act
			using var performanceCollector = new PerformanceCollector(Mock.Of<IPerformanceLogger>());
			using var performanceTracker = new PerformanceTracker(performanceCollector);
			helper.ExecuteConnections(orchestrationEvent, performanceTracker);

			// Assert - all VSGs should be unlocked despite the disconnect failure
			Assert.AreEqual(LockState.Unlocked, api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg1).LockState);
			Assert.AreEqual(LockState.Unlocked, api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg2).LockState);

			// The event should be in a Failed state
			Assert.AreEqual(EventState.Failed, orchestrationEvent.EventState);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationExecution_ConnectFailsDestinationVsgRemainsLocked()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var sourceVsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var destinationVsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Destination 1");

			// Create a connect event and save it as part of a job (required for GetJobInfo)
			var connectEvent = CreateConnectEvent(sourceVsg, destinationVsg);
			var stopEvent = CreateStopEvent();
			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(connectEvent);
			job.OrchestrationEvents.Add(stopEvent);
			api.Orchestration.SaveOrchestrationJobConfiguration(job);

			// Create a mock api that returns a TakeHelper that throws on connect
			var mockApi = new Mock<MediaOpsLiveApi>(api.Connection) { CallBase = true };
			mockApi.Setup(x => x.GetConnectionHandler()).Returns(new FailingConnectTakeHelper(api));

			var planHelper = new Mock<IMediaOpsPlanHelper>().Object;
			var settings = new OrchestrationSettings { Timeout = TimeSpan.FromSeconds(5) };
			var helper = new OrchestrationEventExecutionHelper(mockApi.Object, planHelper, settings);

			// Act
			using var performanceCollector = new PerformanceCollector(Mock.Of<IPerformanceLogger>());
			using var performanceTracker = new PerformanceTracker(performanceCollector);
			helper.ExecuteConnections(connectEvent, performanceTracker);

			// Assert - the destination VSG should remain locked despite the connect failure
			var stateAfter = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg);
			Assert.AreEqual(LockState.Locked, stateAfter.LockState);

			// The event should be in a Failed state
			Assert.AreEqual(EventState.Failed, connectEvent.EventState);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_OrchestrationExecution_ConnectFailsMultipleDestinationVsgsRemainLocked()
		{
			// Arrange
			var simulation = new MediaOpsLiveSimulation();
			var api = simulation.Api;

			var sourceVsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var destinationVsg1 = api.VirtualSignalGroups.Query().First(x => x.Name == "Destination 1");
			var destinationVsg2 = api.VirtualSignalGroups.Query().First(x => x.Name == "Destination 2");

			// Create a connect event with multiple destinations and save it as part of a job
			var connectEvent = CreateConnectEvent(sourceVsg, destinationVsg1, destinationVsg2);
			var stopEvent = CreateStopEvent();
			var job = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(Guid.NewGuid().ToString());
			job.OrchestrationEvents.Add(connectEvent);
			job.OrchestrationEvents.Add(stopEvent);
			api.Orchestration.SaveOrchestrationJobConfiguration(job);

			// Create a mock api that returns a TakeHelper that throws on connect
			var mockApi = new Mock<MediaOpsLiveApi>(api.Connection) { CallBase = true };
			mockApi.Setup(x => x.GetConnectionHandler()).Returns(new FailingConnectTakeHelper(api));

			var planHelper = new Mock<IMediaOpsPlanHelper>().Object;
			var settings = new OrchestrationSettings { Timeout = TimeSpan.FromSeconds(5) };
			var helper = new OrchestrationEventExecutionHelper(mockApi.Object, planHelper, settings);

			// Act
			using var performanceCollector = new PerformanceCollector(Mock.Of<IPerformanceLogger>());
			using var performanceTracker = new PerformanceTracker(performanceCollector);
			helper.ExecuteConnections(connectEvent, performanceTracker);

			// Assert - all destination VSGs should remain locked despite the connect failure
			Assert.AreEqual(LockState.Locked, api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg1).LockState);
			Assert.AreEqual(LockState.Locked, api.VirtualSignalGroupStates.GetByVirtualSignalGroup(destinationVsg2).LockState);

			// The event should be in a Failed state
			Assert.AreEqual(EventState.Failed, connectEvent.EventState);
		}

		private static OrchestrationEventConfiguration CreateDisconnectEvent(params VirtualSignalGroup[] destinationVsgs)
		{
			var connections = new List<Connection>();

			foreach (var destinationVsg in destinationVsgs)
			{
				connections.Add(new Connection
				{
					DestinationNodeId = "1",
					DestinationVsg = destinationVsg.ID,
					LevelMappings = new List<LevelMapping>
					{
						new LevelMapping(new Level("Video", 1), new Level("Video", 1)),
					},
				});
			}

			return new OrchestrationEventConfiguration
			{
				Name = "Test Disconnect Event",
				EventTime = DateTimeOffset.UtcNow,
				EventType = EventType.Stop,
				EventState = EventState.Draft,
				Configuration =
				{
					Connections = connections,
				},
			};
		}

		private static OrchestrationEventConfiguration CreateConnectEvent(VirtualSignalGroup sourceVsg, params VirtualSignalGroup[] destinationVsgs)
		{
			var connections = new List<Connection>();

			foreach (var destinationVsg in destinationVsgs)
			{
				connections.Add(new Connection
				{
					SourceNodeId = "1",
					SourceVsg = sourceVsg.ID,
					DestinationNodeId = "2",
					DestinationVsg = destinationVsg.ID,
					LevelMappings = new List<LevelMapping>
					{
						new LevelMapping(new Level("Video", 1), new Level("Video", 1)),
					},
				});
			}

			return new OrchestrationEventConfiguration
			{
				Name = "Test Connect Event",
				EventTime = DateTimeOffset.UtcNow,
				EventType = EventType.Start,
				EventState = EventState.Draft,
				Configuration =
				{
					Connections = connections,
				},
			};
		}

		private static OrchestrationEventConfiguration CreateStopEvent()
		{
			return new OrchestrationEventConfiguration
			{
				Name = "Test Stop Event",
				EventTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
				EventType = EventType.Stop,
				EventState = EventState.Draft,
			};
		}

		/// <summary>
		/// A TakeHelper subclass that throws when a disconnect connection handler script is executed,
		/// simulating a connection handler that does not support disconnect.
		/// </summary>
		private class FailingDisconnectTakeHelper : TakeHelper
		{
			internal FailingDisconnectTakeHelper(MediaOpsLiveApi api) : base(api)
			{
			}

			protected override void ExecuteConnectionHandlerScript(
				string script,
				ConnectionHandlerScriptAction action,
				IConnectionHandlerInputData inputData,
				PerformanceTracker performanceTracker)
			{
				if (action == ConnectionHandlerScriptAction.Disconnect)
				{
					throw new InvalidOperationException("Disconnect is not supported by this connection handler.");
				}

				base.ExecuteConnectionHandlerScript(script, action, inputData, performanceTracker);
			}
		}

		/// <summary>
		/// A TakeHelper subclass that throws when a connect connection handler script is executed,
		/// simulating a connection failure.
		/// </summary>
		private class FailingConnectTakeHelper : TakeHelper
		{
			internal FailingConnectTakeHelper(MediaOpsLiveApi api) : base(api)
			{
			}

			protected override void ExecuteConnectionHandlerScript(
				string script,
				ConnectionHandlerScriptAction action,
				IConnectionHandlerInputData inputData,
				PerformanceTracker performanceTracker)
			{
				if (action == ConnectionHandlerScriptAction.Connect)
				{
					throw new InvalidOperationException("Connect failed in connection handler.");
				}

				base.ExecuteConnectionHandlerScript(script, action, inputData, performanceTracker);
			}
		}
	}
}
