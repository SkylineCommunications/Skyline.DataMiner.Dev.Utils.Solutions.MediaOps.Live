namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

	/// <summary>
	/// Thrown by <see cref="OrchestrationScriptBase.OrchestrateConnection"/> when connecting or disconnecting a connection of the event failed.
	/// </summary>
	public sealed class OrchestrationConnectionFailedException : Exception
	{
		internal OrchestrationConnectionFailedException(Connection connection, string failureReason)
			: base($"Connection from '{connection?.SourceNodeId}' to '{connection?.DestinationNodeId}' failed: {failureReason}")
		{
			SourceNodeId = connection?.SourceNodeId;
			DestinationNodeId = connection?.DestinationNodeId;
			FailureReason = failureReason;
		}

		/// <summary>
		/// Gets the ID of the source node of the connection, or <see langword="null"/> when it has none.
		/// </summary>
		public string SourceNodeId { get; }

		/// <summary>
		/// Gets the ID of the destination node of the connection, or <see langword="null"/> when it has none.
		/// </summary>
		public string DestinationNodeId { get; }

		/// <summary>
		/// Gets the reason why the connection failed.
		/// </summary>
		public string FailureReason { get; }
	}
}
